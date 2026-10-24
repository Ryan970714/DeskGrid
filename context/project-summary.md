# Project Summary

## Project Name
DeskFolders（repo 目錄名 DeskGrid）

## Project Goal
在 Windows 10 22H2 / 11 桌面放置類似 iOS/Android 的資料夾 widget：2×2 縮圖預覽、點擊展開格子面板、每個 widget 對應一個實體資料夾並即時同步。MIT 開源。

## Project Description
見 `CLAUDE.md` 開頭與「行為規則」。

## Architecture
見 `CLAUDE.md`「架構」與「DesktopHost 實作要點」（唯一來源，此處不重複）。

## Tech Stack
C# + WPF、.NET 10 LTS、x64、CsWin32、self-contained 單一 exe。詳見 `CLAUDE.md`「技術棧」。

## Directory Structure
```
DeskFolders.slnx          solution
global.json               SDK 10.0.x (rollForward latestFeature)
src/DeskFolders/
  DeskFolders.csproj
  app.manifest            PerMonitorV2, asInvoker
  NativeMethods.txt       CsWin32 API list
  App.xaml(.cs)           entry; ShutdownMode=OnExplicitShutdown
  Desktop/                DesktopHost, DesktopSurface, IconLayer, ScreenPoint
  Diagnostics/            Log
  Spike/                  SpikeTile (stage 1 placeholder, 階段 4 移除)
context/                  session context docs
```

## Core Modules
目前僅 DesktopHost（`Desktop/`）。其餘模組見 `CLAUDE.md`「架構」。

## Coding Rules
見 `AGENTS.md`.

## Current Milestone
階段 1：DesktopHost spike。

## Current Status
階段 1 完成，本機（Win11 build 26300，新版桌面階層，200% DPI）自動驗證通過，待使用者手動驗證。

## Known Issues
- 舊版桌面階層（Win10 / Win11 ≤ 23H2）程式路徑尚未實機測試。
- widget 會蓋住其下方的桌面圖示；圖示自動排列可能排到 widget 底下。
- 關閉「顯示桌面圖示」時 widget 仍會顯示（與 SysListView32 為兄弟視窗），尚未決定是否要跟著隱藏。
- 跨行程掛子視窗會連結 input queue：Explorer 卡住時我們的 UI 執行緒可能一起卡住（此方案固有）。`0x052C` 用 `SendMessageTimeout` 最多阻塞 1 秒。
- 位置/大小以系統 DPI 計算；執行中改 DPI、多螢幕尚未處理（MVP 範圍外）。
- Spike 文字未進資源檔（暫時性 placeholder）。

## Future Plan
依 `CLAUDE.md`「開發階段與進度」2 → 7。

---
*本檔案應保持精簡（1–3 頁），僅記錄長期不變或緩慢變動的專案事實。工作日誌/進度細節見 `current-task.md`、`progress.md`、`session-log.md`。*
