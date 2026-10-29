# DeskGrid

[繁體中文](README.zh-TW.md)

Phone-style folders on the Windows desktop. Each folder sits among your desktop icons as a small tile
showing a 2×2 preview of what's inside; click it to open a grid of its items and launch one.

Every folder is a real folder on disk, so whatever you change there shows up on the desktop right away.

- Lives in the desktop icon layer: behind every window, still there after Win+D, back on its own when
  Explorer restarts
- Drop one desktop icon on another and the two become a folder, like on a phone
- Drag shortcuts, programs, files or folders onto a folder to add them; rearrange the items inside a folder, or
  drag them out onto the desktop or into another folder
- Folders line up with the desktop's own icon grid and remember where you put them; icons that land on a folder
  step aside
- Right-click a folder to delete it (to the Recycle Bin) or pin it to the taskbar
- Tray menu: new folder, open the root folder, pin all folders to the taskbar, settings, start with Windows, exit
- Panel background: Windows 11 acrylic following the light/dark theme, dark, light, or your own color
- Folder names stay readable over a light wallpaper
- Checks GitHub for new versions and can update itself
- English and Traditional Chinese, following the Windows display language

## Requirements

Windows 10 22H2 or Windows 11, 64-bit. No .NET install needed: the download is a single self-contained exe.

## Install

1. Download `DeskGrid-<version>-win-x64.exe` from [Releases](../../releases).
2. Put it wherever you like (e.g. `%LOCALAPPDATA%\Programs\DeskGrid\`) and run it.
3. Optional: turn on **Start with Windows** in the tray menu.

### "Windows protected your PC"

The exe isn't code-signed (signing certificates cost money), so Microsoft Defender SmartScreen warns
about it until enough people have run it. To start it anyway, click **More info** → **Run anyway**.
If the file was blocked, right-click it → **Properties** → check **Unblock** → **OK**.

GitHub shows the SHA-256 of the exe on the release page, next to the file. To check that your download matches:

```powershell
Get-FileHash .\DeskGrid-0.3.0-win-x64.exe -Algorithm SHA256
```

If you'd rather not trust a binary, [build it yourself](#build-from-source).

## Usage

On first run DeskGrid creates `%USERPROFILE%\DeskGrid\`. **Every subfolder of it becomes a
desktop folder**; the files inside are its items.

| To | Do |
|---|---|
| Create a folder | Drag a desktop icon onto another one (see below), tray menu → **New folder…**, or create a subfolder in the root folder |
| Open a folder | Click it. Click an item to launch it. Esc, clicking elsewhere or clicking the folder again closes it |
| Move a folder | Drag it; it snaps to the nearest free cell |
| Rename | Click the folder's name in its panel, or rename the subfolder in Explorer |
| Delete | Right-click the folder → **Delete folder…**. The folder goes to the Recycle Bin; what happens to the items inside is up to Settings |
| Add items | Drag them onto the folder (see below), or put files into the subfolder |
| Reorder items | Drag an item to another place in the open panel; the others slide out of the way |
| Take items out | Drag an item out of the panel onto the desktop, or onto another folder (hold it over a folder for a moment to open it and drop inside) |
| Pin to the taskbar | Right-click a folder → **Pin to taskbar…**, then drag the shortcut that appears onto the taskbar (Windows only lets you do the pinning). Tray menu → **Pin all folders to taskbar…** pins one button that lists every folder |

### Making a folder from two icons

Drag a desktop shortcut or file onto another icon on the desktop and let go. A new folder, called "New folder", appears
where the icon you dropped on was, and holds both; click its name in its panel to rename it. Select several icons
first to bring them all along.

- It works on icons that are files on your desktop. "This PC", the Recycle Bin and other system icons can't be
  dropped on, and dropping onto a real folder on the desktop still moves the file into it, as usual.
- Dropping a file on a program's shortcut no longer opens the file with that program: it makes a folder instead.
- Shortcuts are moved or copied, and other files get a shortcut, by the same rules as dropping on a folder (below).

### Dropping onto a folder

| Dropped item | Result |
|---|---|
| Program (`.exe`) | A new shortcut to it. The program itself is never moved or copied |
| Shortcut (`.lnk`, `.url`) lying on the desktop | Moved into the folder (Settings can switch this to copy) |
| Shortcut from anywhere else (e.g. the Start menu folder) | Copied |
| Any other file, or a folder | A new shortcut to it; the original stays where it is |
| Item taken from another folder's panel | Moved |

Items keep the order they were added in; new items go last, or where you drop them in an open panel.

### Settings

Tray menu → **Settings…**:

- **Root folder**: where the folders live. It can't be on the desktop (its folders would show up twice).
- **Panel columns** (1–8).
- **Desktop shortcuts dropped on a folder**: moved off the desktop, or copied.
- **When a folder is deleted, the items inside it**: moved to the desktop (the default), or deleted along with it.
  If an item can't be moved, the folder is kept.
- **Panel background**.
- **Check for updates**: DeskGrid also checks a few seconds after it starts. If there is a newer release you can
  update (downloads it, checks its SHA-256, and restarts into the new version), keep the current version (not asked
  about this release again), or cancel (asked again next start).

### Files

| What | Where |
|---|---|
| Settings and folder positions | `%APPDATA%\DeskGrid\config.json` |
| Log | `%LOCALAPPDATA%\DeskGrid\deskgrid.log` |
| Your folders | `%USERPROFILE%\DeskGrid\` (or the root folder you chose) |

### Uninstall

Turn off **Start with Windows**, exit from the tray menu and delete the exe. To remove settings too,
delete `%APPDATA%\DeskGrid` and `%LOCALAPPDATA%\DeskGrid`. Your folders and their contents are
never touched.

## Known limitations

- Dropping one icon on another only makes a folder for drags that start on the desktop itself.
- With "Auto arrange icons" on, Explorer keeps the icons where it wants them, so a folder moves out of the way of
  an icon instead of the other way round.
- A panel opened from a taskbar button appears at the mouse pointer, so it can come up in the wrong place if you move
  the mouse right after clicking.
- Changing resolution, scaling or the taskbar while running doesn't re-snap folders until a restart.
- Multiple monitors with different scaling aren't supported yet.
- Apps dragged from the Start menu (no file behind them) can't be added.
- When DeskGrid runs as administrator, Windows blocks drops from Explorer.
- The tray menu stays light in dark mode.
- Only tested on Windows 11; the Windows 10 desktop layout is handled but not yet verified.

## Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build DeskGrid.slnx
dotnet test DeskGrid.slnx
dotnet publish src/DeskGrid/DeskGrid.csproj -p:PublishProfile=win-x64   # -> artifacts/publish/DeskGrid.exe
```

Pushing a tag such as `v0.1.0` makes GitHub Actions publish a release.

### How it works

Each folder is a small per-pixel transparent WPF window parented to Explorer's `SHELLDLL_DefView`, the
window that holds the desktop icons, placed above the icon list. That is what keeps it behind other
windows yet clickable, and it works with both the classic desktop layout and the one in Windows 11
24H2. When Explorer restarts and takes those windows with it, DeskGrid re-attaches them.
`CLAUDE.md` and `context/` hold the design notes and decisions.

## License

[MIT](LICENSE) © 2026 Ryan Kuo
