# DeskFolders

[繁體中文](README.zh-TW.md)

Phone-style folders on the Windows desktop. Each folder sits among your desktop icons as a small tile
showing a 2×2 preview of what's inside; click it to open a grid of its items and launch one.

Every folder is a real folder on disk, so whatever you change there shows up on the desktop right away.

- Lives in the desktop icon layer: behind every window, still there after Win+D, back on its own when
  Explorer restarts
- Drag shortcuts, programs, files or folders onto a folder to add them
- Folders snap to a grid and remember where you put them
- Tray menu: new folder, open the root folder, settings, start with Windows, exit
- Panel background: Windows 11 acrylic following the light/dark theme, dark, light, or your own color
- English and Traditional Chinese, following the Windows display language

## Requirements

Windows 10 22H2 or Windows 11, 64-bit. No .NET install needed: the download is a single self-contained exe.

## Install

1. Download `DeskFolders-<version>-win-x64.exe` from [Releases](../../releases).
2. Put it wherever you like (e.g. `%LOCALAPPDATA%\Programs\DeskFolders\`) and run it.
3. Optional: turn on **Start with Windows** in the tray menu.

### "Windows protected your PC"

The exe isn't code-signed (signing certificates cost money), so Microsoft Defender SmartScreen warns
about it until enough people have run it. To start it anyway, click **More info** → **Run anyway**.
If the file was blocked, right-click it → **Properties** → check **Unblock** → **OK**.

Each release has a `.sha256` file next to the exe. To check that your download matches:

```powershell
Get-FileHash .\DeskFolders-0.1.0-win-x64.exe -Algorithm SHA256
```

If you'd rather not trust a binary, [build it yourself](#build-from-source).

## Usage

On first run DeskFolders creates `%USERPROFILE%\DeskFolders\`. **Every subfolder of it becomes a
desktop folder**; the files inside are its items.

| To | Do |
|---|---|
| Create a folder | Tray menu → **New folder…**, or create a subfolder in the root folder |
| Open a folder | Click it. Click an item to launch it. Esc, clicking elsewhere or clicking the folder again closes it |
| Move a folder | Drag it; it snaps to the nearest free cell |
| Rename or delete | Rename or delete the subfolder in Explorer |
| Add items | Drag them onto the folder (see below), or put files into the subfolder |

What dropping onto a folder does:

| Dropped item | Result |
|---|---|
| Program (`.exe`) | A new shortcut to it. The program itself is never moved or copied |
| Shortcut (`.lnk`, `.url`) lying on the desktop | Moved into the folder (Settings can switch this to copy) |
| Shortcut from anywhere else (e.g. the Start menu folder) | Copied |
| Any other file, or a folder | A new shortcut to it; the original stays where it is |

Items keep the order they were added in; new items go last.

### Settings

Tray menu → **Settings…**: root folder, panel columns (1–8), whether desktop shortcuts are moved or
copied, and the panel background. The root folder can't be on the desktop (its folders would show up
twice).

### Files

| What | Where |
|---|---|
| Settings and folder positions | `%APPDATA%\DeskFolders\config.json` |
| Log | `%LOCALAPPDATA%\DeskFolders\deskfolders.log` |
| Your folders | `%USERPROFILE%\DeskFolders\` (or the root folder you chose) |

### Uninstall

Turn off **Start with Windows**, exit from the tray menu and delete the exe. To remove settings too,
delete `%APPDATA%\DeskFolders` and `%LOCALAPPDATA%\DeskFolders`. Your folders and their contents are
never touched.

## Known limitations

- Folders don't avoid the desktop's own icons; drag them apart if they overlap.
- Changing resolution, scaling or the taskbar while running doesn't re-snap folders until a restart.
- Multiple monitors with different scaling aren't supported yet.
- Apps dragged from the Start menu (no file behind them) can't be added.
- When DeskFolders runs as administrator, Windows blocks drops from Explorer.
- The tray menu and Settings window stay light in dark mode.
- Only tested on Windows 11; the Windows 10 desktop layout is handled but not yet verified.

## Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build DeskFolders.slnx
dotnet test DeskFolders.slnx
dotnet publish src/DeskFolders/DeskFolders.csproj -p:PublishProfile=win-x64   # -> artifacts/publish/DeskFolders.exe
```

Pushing a tag such as `v0.1.0` makes GitHub Actions publish a release.

### How it works

Each folder is a small per-pixel transparent WPF window parented to Explorer's `SHELLDLL_DefView`, the
window that holds the desktop icons, placed above the icon list. That is what keeps it behind other
windows yet clickable, and it works with both the classic desktop layout and the one in Windows 11
24H2. When Explorer restarts and takes those windows with it, DeskFolders re-attaches them.
`CLAUDE.md` and `context/` hold the design notes and decisions.

## License

[MIT](LICENSE) © 2026 Ryan Kuo
