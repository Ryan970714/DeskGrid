# Session Log

> 每次 Session 結束新增一筆，最新放最上方，每筆不超過 10 行。

---

**2026-10-25（更名 DeskGrid）**
**Goal**: 專案與 repo 全面改名 DeskFolders → DeskGrid。
**Completed**: 內容、資料夾、solution/csproj/ico 全改名；GitHub repo 改名並更新 remote；build 0 警告、72 測試通過。
**Modified**: 全專案、context 文件。
**Decisions**: 見 decisions.md「專案更名」。
**Next Session**: 使用者驗證；舊資料夾 `%USERPROFILE%\DeskFolders`、`%APPDATA%\DeskFolders` 需手動搬到 DeskGrid。
**Issues**: v0.1.0 Release 的 exe 仍叫 DeskFolders，下次發版才是 DeskGrid。

---

**2026-10-25（v0.1.0）**
**Goal**: 發布第一個公開 Release。
**Completed**: 推 `v0.1.0` tag，Release workflow 通過；Release 附 62 MB exe 與 .sha256，下載核對一致，檔案版本 0.1.0。
**Modified**: context 文件。
**Decisions**: 無新決策。
**Next Session**: 使用者驗證後挑 MVP 後改進。
**Issues**: 無。

---

**2026-10-25（階段 7）**
**Goal**: CI、self-contained 打包、README、LICENSE。
**Completed**: publish profile（62 MB 單一 exe，實測可執行）、ci.yml、release.yml、README en/zh-TW、MIT LICENSE、csproj 版本資訊；公開前掃描個人資訊。
**Modified**: DeskFolders.csproj、.gitignore（artifacts/、pubxml 例外）。
**Decisions**: 公開 repo、tag Release、Ryan Kuo、雙語 README（使用者）；打包設定（見 decisions.md）。
**Next Session**: 使用者驗證；是否推 v0.1.0 tag 發布 Release。
**Issues**: 使用者授權 workflow 權限後已建公開 repo 並推送，首次 CI 通過（72 測試）。

---

**2026-10-25（階段 6）**
**Goal**: 拖放加入、TrayApp、開機自啟。
**Completed**: Tray/、Settings/、Shell/、ItemImporter、WidgetFolderName、App 圖示、單一執行個體；72 測試；實機驗證系統匣、設定、新增資料夾、開機自啟、OLE 拖放。
**Modified**: App.xaml.cs（重寫）、DesktopHost、FolderWidget（拖放、移除結束選單）、WidgetBoard、app.manifest、csproj、Strings。
**Decisions**: 拖放規則與 Run 機碼（使用者）；慣例預設、Shell/ 集中、XAML 不引用本地型別（見 decisions.md）。
**Next Session**: 使用者驗證後開始階段 7，先確認 GitHub repo 與發布方式。
**Issues**: 檔案總管桌面直接拖到 widget 未實機驗證；測試腳本誤送一次點擊到使用者視窗，之後停止模擬輸入。

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
