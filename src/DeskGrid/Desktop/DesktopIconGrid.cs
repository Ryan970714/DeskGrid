using System.Drawing;
using DeskGrid.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskGrid.Desktop;

/// <summary>One desktop icon.</summary>
/// <param name="Index">The icon's item index in Explorer's list view.</param>
/// <param name="Cell">The icon's cell, in physical screen pixels.</param>
/// <param name="ViewPosition">The icon's position as the list view reports it, in its client coordinates.</param>
internal sealed record DesktopIcon(int Index, ScreenRect Cell, Point ViewPosition);

/// <summary>
/// The desktop's own icon grid, read from Explorer's icon list view: the size of one icon cell and where
/// the icons currently sit. Widgets use it to line up with the icons and to stay off them.
/// </summary>
/// <param name="Anchor">The top-left corner of one icon cell (all cells lie on the lattice through it).</param>
/// <param name="Icons">Every icon on the desktop.</param>
/// <param name="AutoArranged">Explorer arranges the icons itself ("Auto arrange icons"), so they can't be moved.</param>
internal sealed record DesktopIconGrid(int CellWidth, int CellHeight, ScreenPoint? Anchor, IReadOnlyList<DesktopIcon> Icons, bool AutoArranged)
{
    // Messages of the list view (commctrl.h): LVM_FIRST = 0x1000.
    private const uint GetItemCount = 0x1004;
    private const uint GetItemRect = 0x100E;
    private const uint SetItemPosition = 0x100F;
    private const uint GetItemPosition = 0x1010;
    private const int IconRectangle = 1; // LVIR_ICON
    private const uint GetItemSpacing = 0x1033;
    private const int AutoArrangeStyle = 0x0100; // LVS_AUTOARRANGE
    private const int MaxIcons = 2000;
    private const uint Timeout = 500;

    /// <summary>Reads the grid, or returns <see langword="null"/> if Explorer doesn't answer.</summary>
    public static unsafe DesktopIconGrid? Read(IconLayer layer)
    {
        var listView = layer.FindListView();
        if (listView.IsNull)
        {
            return null;
        }

        // Large icon view; the result packs the cell's width and height into one value.
        if (!TrySend(listView, GetItemSpacing, 0, 0, out var spacing))
        {
            return null;
        }

        var (cellWidth, cellHeight) = ((int)(spacing & 0xFFFF), (int)((spacing >> 16) & 0xFFFF));
        if (cellWidth < 16 || cellHeight < 16 || !TrySend(listView, GetItemCount, 0, 0, out var count))
        {
            return null;
        }

        var origin = new Point(0, 0);
        PInvoke.ClientToScreen(listView, ref origin);
        var positions = ReadPositions(listView, (int)Math.Min(count, MaxIcons));
        if (positions is null)
        {
            return null;
        }

        // The list view reports where each icon image is; the cell is centered on it horizontally and starts at its top.
        var icons = positions
            .Select((p, index) =>
            {
                var left = origin.X + (p.Icon.left + p.Icon.right - cellWidth) / 2;
                var cell = new ScreenRect(left, origin.Y + p.Position.Y, left + cellWidth, origin.Y + p.Position.Y + cellHeight);
                return new DesktopIcon(index, cell, p.Position);
            })
            .ToList();

        var anchor = icons.Count > 0 ? new ScreenPoint(icons[0].Cell.Left, icons[0].Cell.Top) : (ScreenPoint?)null;
        var autoArranged = (PInvoke.GetWindowLong(listView, WINDOW_LONG_PTR_INDEX.GWL_STYLE) & AutoArrangeStyle) != 0;
        return new DesktopIconGrid(cellWidth, cellHeight, anchor, icons, autoArranged);
    }

    /// <summary>Moves <paramref name="icon"/> so its cell's top-left corner is at <paramref name="cell"/>.</summary>
    public static bool Move(IconLayer layer, DesktopIcon icon, ScreenPoint cell)
    {
        var listView = layer.FindListView();
        var x = icon.ViewPosition.X + cell.X - icon.Cell.Left;
        var y = icon.ViewPosition.Y + cell.Y - icon.Cell.Top;

        // The position travels in the message itself (MAKELPARAM), so nothing has to be written into Explorer.
        var position = (nint)(uint)((ushort)x | ((ushort)y << 16));
        return !listView.IsNull && TrySend(listView, SetItemPosition, (nuint)icon.Index, position, out _);
    }

    // LVM_GETITEMPOSITION and LVM_GETITEMRECT write into the caller's memory, which must be in Explorer's address space.
    private static unsafe List<(Point Position, RECT Icon)>? ReadPositions(HWND listView, int count)
    {
        if (count == 0)
        {
            return [];
        }

        if (PInvoke.GetWindowThreadProcessId(listView, out var processId) == 0)
        {
            return null;
        }

        using var process = PInvoke.OpenProcess_SafeHandle(
            PROCESS_ACCESS_RIGHTS.PROCESS_VM_OPERATION | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ | PROCESS_ACCESS_RIGHTS.PROCESS_VM_WRITE,
            false, processId);
        if (process.IsInvalid)
        {
            Log.Info("Could not open Explorer to read desktop icon positions.");
            return null;
        }

        var remote = PInvoke.VirtualAllocEx(process, null, (nuint)sizeof(RECT),
            VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE, PAGE_PROTECTION_FLAGS.PAGE_READWRITE);
        if (remote is null)
        {
            return null;
        }

        var handle = new HANDLE(process.DangerousGetHandle());
        try
        {
            var items = new List<(Point, RECT)>(count);
            for (var index = 0; index < count; index++)
            {
                Point position;
                // The rectangle's left field says which part of the item is asked for.
                var iconRequest = new RECT { left = IconRectangle };
                RECT icon;
                if (!TrySend(listView, GetItemPosition, (nuint)index, (nint)remote, out var found) || found == 0
                    || !PInvoke.ReadProcessMemory(handle, remote, &position, (nuint)sizeof(Point), null)
                    || !PInvoke.WriteProcessMemory(handle, remote, &iconRequest, (nuint)sizeof(RECT), null)
                    || !TrySend(listView, GetItemRect, (nuint)index, (nint)remote, out found) || found == 0
                    || !PInvoke.ReadProcessMemory(handle, remote, &icon, (nuint)sizeof(RECT), null))
                {
                    return null;
                }

                items.Add((position, icon));
            }

            return items;
        }
        finally
        {
            PInvoke.VirtualFreeEx(handle, remote, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
        }
    }

    // Never block on a hung Explorer.
    private static bool TrySend(HWND window, uint message, nuint wParam, nint lParam, out nuint result)
    {
        var sent = PInvoke.SendMessageTimeout(window, message, wParam, lParam, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG, Timeout, out result);
        return sent != 0;
    }
}
