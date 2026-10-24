# Progress

## Completed
- 階段 1：DesktopHost spike（使用者驗證通過）
  - 專案骨架：.NET 10 WPF、CsWin32、PerMonitorV2 manifest、global.json、.editorconfig
  - `IconLayer.Find()`：送 `0x052C`，找新版 / 舊版階層中的 `SHELLDLL_DefView`
  - `DesktopSurface`：layered 子視窗 HwndSource，可重建並沿用 WPF content
  - `DesktopHost`：Lost 事件 + 每秒重試 + `TaskbarCreated` 監聽，自動重掛
  - 專案 context 文件（CLAUDE.md、AGENTS.md、context/）
- 階段 2：ConfigStore + FolderScanner + FileSystemWatcher（待驗證）
  - `AppConfig` / `ConfigStore`：JSON、原子寫入、壞檔備份、不分大小寫的 widget key
  - `FolderScanner`：可見檔案掃描、自然排序、watcher + 300ms debounce、rename 事件
  - `WidgetCatalog`：套用排序、新項目接最後、清除殘留設定、rename 轉移
  - `RootFolder.Prepare`：完整路徑、不在桌面、可建立；啟動時不合格 → 提示 + 改用預設
  - 資源檔 en / zh-TW；`Directory.Build.props`；xUnit v3 測試專案（21 tests）
  - `SpikeTile` 顯示掃描結果供手動驗證

## In Progress
- 階段 2 使用者驗證

## Pending
- 階段 3 ~ 7（見 `CLAUDE.md`「開發階段與進度」）

## Completion %
約 29%（2 / 7 階段）

## Milestones
- 2026-10-24：階段 1 完成並驗證通過
- 2026-10-24：階段 2 完成（待驗證）

## Last Update
2026-10-24
