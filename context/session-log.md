# Session Log

> 每次 Session 結束新增一筆，最新放最上方，每筆不超過 10 行。

---

**2026-10-25（階段 5）**
**Goal**: PopupPanel（格子、啟動、收合）。
**Completed**: Panel/（PopupPanel、PanelController、PanelPlacement、PanelTheme、Backdrop、ShellItem、PanelItem）、config `panelBackground`；48 測試通過；Acrylic 與 layered 實機全流程通過。
**Modified**: App.xaml.cs、AppConfig、ConfigStore（泛用 enum converter）、DesktopSurface（MA_NOACTIVATE）、WidgetBoard（點擊帶邊界）、Strings（PanelEmpty）。
**Decisions**: 面板旁、48 DIP、標題、可自訂背景（使用者）；Acrylic 視窗做法、MA_NOACTIVATE、Process.Start（見 decisions.md）。
**Next Session**: 使用者驗證後開始階段 6，先確認設定視窗、拖放、開機自啟。
**Issues**: 測試中 widget 被拖動導致一輪結果作廢；面板開著時拖 widget 不會關面板。

---

**2026-10-24（階段 4）**
**Goal**: FolderWidget（顯示、拖動、存位置）。
**Completed**: Widgets/（FolderWidget、WidgetBoard、WidgetGrid）、MonitorArea、DesktopHost Add/Remove、DesktopSurface.MoveTo；移除 Spike；36 測試通過；實機擺放、拖曳吸附、點擊、重開、右鍵選單皆通過。
**Modified**: App.xaml.cs、DesktopHost、DesktopSurface、ScreenPoint、NativeMethods.txt、Strings（MenuExit）。
**Decisions**: 外觀 A / 48 DIP / 右上角 / 對齊格線（使用者）；76×96 格、最近空格（見 decisions.md）。
**Next Session**: 使用者驗證後開始階段 5，先確認面板外觀與位置。
**Issues**: 不避開原生圖示；執行中改解析度不重新吸附。

---

**2026-10-24（階段 3）**
**Goal**: IconService + 2×2 預覽合成。
**Completed**: Icons/（ShellImage、PreviewComposer、IconService）、Threading/DispatcherThread；28 個測試通過；實驗確認 256px 與直通 alpha；實機預覽即時更新。
**Modified**: App.xaml.cs、SpikeTile、NativeMethods.txt、測試改用共用 DispatcherThread。
**Decisions**: 一律圖示（使用者）；Bgra32；STA 執行緒 + mtime 快取（見 decisions.md）。
**Next Session**: 使用者驗證後開始階段 4，先確認 widget 外觀與預設位置。
**Issues**: 捷徑目標圖示更新需重啟；`%USERPROFILE%\DeskFolders` 留有 4 個示範資料夾。

---

**2026-10-24（階段 2）**
**Goal**: ConfigStore + FolderScanner + FileSystemWatcher。
**Completed**: Config/、Folders/、資源檔 en/zh-TW、xUnit v3 測試 21 個通過；實機驗證掃描、排序、rename 轉移。
**Modified**: App.xaml.cs、SpikeTile、Log、csproj（共用設定移到 Directory.Build.props）。
**Decisions**: 位置=實體像素、欄數全域、清除殘留設定、根目錄不合格改用預設；Scanner/Catalog 拆分；SynchronizationContext 序列化（見 decisions.md）。
**Next Session**: 使用者驗證後開始階段 3。
**Issues**: 關閉期間改名無法轉移；根目錄在桌面的提示視窗未實機測。

---

**2026-10-24**
**Goal**: 階段 1 — DesktopHost spike 可行性驗證。
**Completed**: 安裝 .NET 10 SDK；建立 repo、專案骨架、context 文件；實作 DesktopHost / DesktopSurface / IconLayer / SpikeTile；本機自動驗證掛載、渲染、Win+D、explorer 重啟皆通過。
**Modified**: 全部為新檔。
**Decisions**: .NET 10 LTS；掛 SHELLDLL_DefView 圖示之上；layered 子視窗；重掛=重建 HwndSource；CsWin32（見 decisions.md）。
**Next Session**: 使用者驗證後開始階段 2。
**Issues**: 舊版桌面階層未實機測試；widget 蓋住下方圖示。

---
