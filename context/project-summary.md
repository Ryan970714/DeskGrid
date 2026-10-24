# Project Summary

## Project Name
DeskFolders（repo 目錄名 DeskGrid）

## Project Goal
在 Windows 10 22H2 / 11 桌面放置類似 iOS/Android 的資料夾 widget：2×2 縮圖預覽、點擊展開格子面板、每個 widget 對應一個實體資料夾並即時同步。MIT 開源。

## Project Description
見 `CLAUDE.md` 開頭與「行為規則」。

## Architecture
見 `CLAUDE.md`「架構」與各階段「實作要點」（唯一來源，此處不重複）。

## Tech Stack
C# + WPF、.NET 10 LTS、x64、CsWin32、xUnit v3、self-contained 單一 exe。詳見 `CLAUDE.md`「技術棧」。

## Directory Structure
```
DeskFolders.slnx          solution
Directory.Build.props     shared settings (nullable, x64, warnings as errors)
global.json               SDK 10.0.x (rollForward latestFeature)
src/DeskFolders/
  DeskFolders.csproj
  app.manifest            PerMonitorV2, asInvoker
  NativeMethods.txt       CsWin32 API list
  App.xaml(.cs)           entry; ShutdownMode=OnExplicitShutdown
  Config/                 AppConfig, ConfigStore
  Desktop/                DesktopHost, DesktopSurface, IconLayer, ScreenPoint
  Diagnostics/            Log
  Folders/                FolderScanner, WidgetCatalog, RootFolder, NaturalStringComparer, records
  Resources/              Strings.resx (en), Strings.zh-TW.resx
  Spike/                  SpikeTile (placeholder, 階段 4 移除)
tests/DeskFolders.Tests/  xUnit v3
context/                  session context docs
```

## Core Modules
DesktopHost（`Desktop/`）、ConfigStore（`Config/`）、FolderScanner + WidgetCatalog（`Folders/`）。其餘模組見 `CLAUDE.md`「架構」。

## Coding Rules
見 `AGENTS.md`.

## Current Milestone
階段 2：ConfigStore + FolderScanner + FileSystemWatcher。

## Current Status
階段 1 使用者驗證通過。階段 2 完成，21 個測試通過，本機實機驗證通過，待使用者手動驗證。

## Known Issues
- 舊版桌面階層（Win10 / Win11 ≤ 23H2）程式路徑尚未實機測試。
- widget 會蓋住其下方的桌面圖示；圖示自動排列可能排到 widget 底下。
- 關閉「顯示桌面圖示」時 widget 仍會顯示，尚未決定是否要跟著隱藏。
- 跨行程掛子視窗會連結 input queue：Explorer 卡住時我們的 UI 執行緒可能一起卡住。`0x052C` 最多阻塞 1 秒。
- 位置/大小以系統 DPI 計算；執行中改 DPI、多螢幕尚未處理（MVP 範圍外）。
- 程式關閉期間改名的資料夾/項目無法轉移設定（下次啟動視為刪除 + 新增）。
- 無法讀取的 widget 資料夾（權限不足）在該次掃描中視為不存在，其設定會被清除。
- 執行中刪除根目錄本身：watcher 停止，不會自動恢復（重啟 app 會重建）。
- Spike 文字未進資源檔（暫時性 placeholder）。

## Future Plan
依 `CLAUDE.md`「開發階段與進度」3 → 7。

---
*本檔案應保持精簡（1–3 頁），僅記錄長期不變或緩慢變動的專案事實。工作日誌/進度細節見 `current-task.md`、`progress.md`、`session-log.md`。*
