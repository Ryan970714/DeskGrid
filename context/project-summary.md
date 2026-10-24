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
  Desktop/                DesktopHost, DesktopSurface, IconLayer, MonitorArea, ScreenPoint/ScreenRect
  Diagnostics/            Log
  Folders/                FolderScanner, WidgetCatalog, RootFolder, NaturalStringComparer, records
  Icons/                  IconService, ShellImage, PreviewComposer
  Panel/                  PopupPanel (XAML), PanelController, PanelPlacement, PanelTheme, Backdrop, ShellItem, PanelItem
  Resources/              Strings.resx (en), Strings.zh-TW.resx
  Threading/              DispatcherThread (STA worker)
  Widgets/                FolderWidget (XAML), WidgetBoard, WidgetGrid
tests/DeskFolders.Tests/  xUnit v3
context/                  session context docs
```

## Core Modules
DesktopHost（`Desktop/`）、ConfigStore（`Config/`）、FolderScanner + WidgetCatalog（`Folders/`）、IconService（`Icons/`）、FolderWidget + WidgetBoard（`Widgets/`）、PopupPanel + PanelController（`Panel/`）。其餘模組見 `CLAUDE.md`「架構」。

## Coding Rules
見 `AGENTS.md`.

## Current Milestone
階段 5：PopupPanel（格子、啟動、收合）。

## Current Status
階段 1–4 使用者驗證通過。階段 5 完成，48 個測試通過，本機實機驗證（Acrylic 與 layered 兩種背景：開啟、Esc、點外部、切換、啟動）通過，待使用者手動驗證。

## Known Issues
- 舊版桌面階層（Win10 / Win11 ≤ 23H2）程式路徑尚未實機測試。
- widget 會蓋住其下方的桌面圖示；圖示自動排列可能排到 widget 底下。
- 關閉「顯示桌面圖示」時 widget 仍會顯示，尚未決定是否要跟著隱藏。
- 跨行程掛子視窗會連結 input queue：Explorer 卡住時我們的 UI 執行緒可能一起卡住。`0x052C` 最多阻塞 1 秒。
- 位置/大小以系統 DPI 計算；執行中改 DPI、多螢幕尚未處理（MVP 範圍外）。
- 程式關閉期間改名的資料夾/項目無法轉移設定（下次啟動視為刪除 + 新增）。
- 無法讀取的 widget 資料夾（權限不足）在該次掃描中視為不存在，其設定會被清除。
- 執行中刪除根目錄本身：watcher 停止，不會自動恢復（重啟 app 會重建）。
- 捷徑目標程式更新圖示但 .lnk 本身沒變時，快取的舊圖示要到重啟 app 才更新。
- 圖示快取只在記憶體，每次啟動重新向 Shell 取（每個項目一次）。
- 只有小尺寸圖示的程式在 256px 時由 Shell 放大，可能偏糊（本機樣本都有 256px 圖示）。
- widget 不會避開原生桌面圖示（右側若有原生圖示會疊在一起），需使用者自己拖開。
- 執行中改解析度、DPI、工作列位置時 widget 不會重新吸附，重啟 app 才夾回工作區。
- 多螢幕：格子大小依各螢幕 DPI 算，但 widget 實際以 Progman 的 DPI 渲染，不同 DPI 的副螢幕上可能錯位。
- 拖曳中無法用 Esc 取消；放到已佔用的格子會被放到次近的空格。
- 暫時性：widget 右鍵選單只有「結束」（階段 6 移除）。
- 面板開著時拖曳另一個 widget，面板不會自動關閉。
- 面板背景只在開啟時讀取設定與系統深淺色；目前改設定要手改 config 並重啟 app（階段 6 設定 UI）。
- Win10 上「system」背景走不透明系統色路徑，未在 Win10 實機看過。
- 面板內只支援 Tab / Enter 鍵盤操作，沒有方向鍵。

## Future Plan
依 `CLAUDE.md`「開發階段與進度」3 → 7。

---
*本檔案應保持精簡（1–3 頁），僅記錄長期不變或緩慢變動的專案事實。工作日誌/進度細節見 `current-task.md`、`progress.md`、`session-log.md`。*
