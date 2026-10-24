# Current Task

## Current Goal
階段 2：ConfigStore + FolderScanner + FileSystemWatcher。

## Current Task
等待使用者手動驗證階段 2。

## Current Status
實作完成並已 commit。驗證：
- `dotnet test`：21 個測試通過（連跑 4 次穩定），含 rename 轉移、debounce、清除、壞 config 備份、根目錄驗證
- 實機：在 `%USERPROFILE%\DeskFolders` 建資料夾/檔案 → 方塊即時列出、config 寫入自然排序；資料夾與項目改名後設定正確轉移
- 未實機測：根目錄設在桌面時的提示視窗（MessageBox 需人工關閉；邏輯有單元測試）

## Current File
`src/DeskFolders/Config/*`、`src/DeskFolders/Folders/*`

## Next Step
使用者驗證通過後開始階段 3（IconService + 2×2 預覽合成）。

## Blocking Issues
無。

## Last Update
2026-10-24
