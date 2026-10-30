using System.IO;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace DeskGrid.Shell;

/// <summary>Deleting the way Explorer does: into the Recycle Bin, so it can be undone.</summary>
internal static class RecycleBin
{
    // The Recycle Bin's own shell item, wherever it is shown.
    private const string ParsingName = "::{645FF040-5081-101B-9F08-00AA002F954E}";

    /// <summary>
    /// The name Explorer shows under the Recycle Bin's desktop icon: it follows the display language and a rename,
    /// so it is the way to tell that icon from the others.
    /// </summary>
    public static string DisplayName => ShellItem.GetDisplayName(ParsingName);

    /// <summary>
    /// Sends a file or folder to the Recycle Bin, without progress or confirmation windows. If it can't be recycled
    /// (a network drive, or too big), the shell asks before deleting it for good.
    /// </summary>
    /// <returns><see langword="false"/> if that question was answered with no.</returns>
    /// <exception cref="IOException">The shell could not delete it.</exception>
    public static unsafe bool Send(string path)
    {
        // The list of paths ends with an extra null.
        fixed (char* from = path + "\0")
        {
            var operation = new SHFILEOPSTRUCTW
            {
                wFunc = PInvoke.FO_DELETE,
                pFrom = from,
                fFlags = (ushort)(FILEOPERATION_FLAGS.FOF_ALLOWUNDO | FILEOPERATION_FLAGS.FOF_NOCONFIRMATION
                    | FILEOPERATION_FLAGS.FOF_WANTNUKEWARNING | FILEOPERATION_FLAGS.FOF_SILENT),
            };

            var result = PInvoke.SHFileOperation(ref operation);
            return result == 0
                ? !operation.fAnyOperationsAborted
                : throw new IOException($"Deleting \"{path}\" failed (error 0x{result:X}).");
        }
    }
}
