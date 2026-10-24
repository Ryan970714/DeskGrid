# Session Log

> 每次 Session 結束新增一筆，最新放最上方，每筆不超過 10 行。

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
