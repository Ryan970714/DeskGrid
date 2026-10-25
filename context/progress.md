# Progress

## Completed
- 階段 1：DesktopHost spike（使用者驗證通過）
  - 專案骨架：.NET 10 WPF、CsWin32、PerMonitorV2 manifest、global.json、.editorconfig
  - `IconLayer.Find()`：送 `0x052C`，找新版 / 舊版階層中的 `SHELLDLL_DefView`
  - `DesktopSurface`：layered 子視窗 HwndSource，可重建並沿用 WPF content
  - `DesktopHost`：Lost 事件 + 每秒重試 + `TaskbarCreated` 監聽，自動重掛
  - 專案 context 文件（CLAUDE.md、AGENTS.md、context/）
- 階段 7：CI、打包、README、LICENSE（本機完成，待推上 GitHub）
  - `win-x64.pubxml`：self-contained 單一 exe（62 MB）
  - `ci.yml`（build / test / publish artifact）、`release.yml`（v* tag → Release + sha256）
  - `README.md` + `README.zh-TW.md`（含 SmartScreen 與 SHA256 說明）、MIT `LICENSE`
  - csproj 版本與版權資訊
- 階段 6：拖放加入、TrayApp、開機自啟（使用者驗證通過）
  - `ItemImporter`：拖放規則（exe/檔案/資料夾 → 捷徑、桌面捷徑 → 依模式、其他捷徑 → 複製）、同名編號
  - `ShortcutFile`（IShellLinkW）；`Shell/` 集中 Shell 互通
  - `TrayIcon`（Shell_NotifyIcon + 原生選單）、`AutoStart`（Run 機碼 + 工作管理員旗標）、單一執行個體
  - `SettingsWindow`（根目錄、欄數、拖入模式、面板背景 + ChooseColor）、`NewFolderDialog`
  - App 圖示 + `tools/make-icon.ps1`；manifest 加 Common Controls v6；移除暫時性結束選單
  - 24 個新測試（共 72）
- 階段 5：PopupPanel（使用者驗證通過）
  - `PopupPanel`（XAML）：標題、3 欄格子、4 列後捲動、空資料夾提示、Esc / 失焦收合、即時刷新
  - `PanelController`：一次一個面板、切換、啟動後收合
  - `PanelPlacement`：widget 下/上/左/右、夾回工作區
  - `PanelTheme` + `Backdrop`：system（Win11 Acrylic）/ dark / light / custom；config `panelBackground`
  - `ShellItem`：ShellExecuteEx 啟動、Shell 顯示名稱
  - `DesktopSurface` 回 `MA_NOACTIVATE`；11 個新測試（共 48）
- 階段 4：FolderWidget（使用者驗證通過）
  - `FolderWidget`（XAML）：48 DIP 淺色半透明方塊 + 名稱、hover 高亮、點擊/拖曳手勢、暫時性結束選單
  - `WidgetGrid`：右上角起算的 76×96 DIP 格子、FirstFree / NearestFree
  - `WidgetBoard`：依 catalog 同步 widget、擺放、拖曳吸附、存位置
  - `DesktopHost.Add` 立即掛載 / `Remove`；`DesktopSurface.MoveTo`；`MonitorArea`（工作區 + DPI）
  - 移除 Spike；8 個新測試（共 36）
- 階段 3：IconService + 預覽合成（使用者驗證通過）
  - `ShellImage`：IShellItemImageFactory（ICONONLY）→ 直通 alpha `Bgra32` frozen bitmap
  - `PreviewComposer`：256² 透明 2×2 預覽，依列填滿
  - `IconService`：STA 背景執行緒、圖示/預覽記憶體快取（Task 快取、前 4 項 key）、`Retain`
  - `DispatcherThread` 從測試搬進 `Threading/` 共用；7 個新測試（共 28）
  - `SpikeTile` 顯示每個資料夾的預覽
- 階段 2：ConfigStore + FolderScanner + FileSystemWatcher（使用者驗證通過）
  - `AppConfig` / `ConfigStore`：JSON、原子寫入、壞檔備份、不分大小寫的 widget key
  - `FolderScanner`：可見檔案掃描、自然排序、watcher + 300ms debounce、rename 事件
  - `WidgetCatalog`：套用排序、新項目接最後、清除殘留設定、rename 轉移
  - `RootFolder.Prepare`：完整路徑、不在桌面、可建立；啟動時不合格 → 提示 + 改用預設
  - 資源檔 en / zh-TW；`Directory.Build.props`；xUnit v3 測試專案（21 tests）
  - `SpikeTile` 顯示掃描結果供手動驗證

## In Progress
- 階段 7：推上 GitHub、確認 CI、使用者驗證

## Pending
- MVP 後的改進（見 `todo.md`）

## Completion %
約 95%（7 / 7 階段實作完成，待推送與驗證）

## Milestones
- 2026-10-24：階段 1 完成並驗證通過
- 2026-10-24：階段 2 完成並驗證通過
- 2026-10-24：階段 3 完成並驗證通過
- 2026-10-24：階段 4 完成並驗證通過
- 2026-10-25：階段 5 完成並驗證通過
- 2026-10-25：階段 6 完成並驗證通過
- 2026-10-25：階段 7 本機完成

## Last Update
2026-10-24
