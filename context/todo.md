# Todo

## High Priority
- 使用者手動驗證階段 3
- 在 Win10 22H2（或 Win11 23H2）驗證舊版桌面階層路徑（可用 VirtualBox VM）

## Medium Priority
- 階段 4 開始前需確認：widget 外觀（底色、圓角、大小、名稱樣式）、新 widget 的預設擺放位置
- 階段 4：載入位置時若在工作區外要夾回
- 階段 5：面板項目圖示用 `IconService.GetIconAsync`，延遲載入
- 階段 5：`PanelColumns` 使用時夾在合理範圍（手改 config 可能填 0 或負數）

## Low Priority
- 關閉「顯示桌面圖示」時 widget 是否跟著隱藏
- 執行中 DPI 變更（`WM_DPICHANGED_AFTERPARENT`）處理
- 執行中根目錄被刪除時自動恢復 watcher
- 捷徑目標圖示更新時刷新快取（目前要重啟）

## Future Ideas
- 多螢幕支援（MVP 後）
- widget 下方桌面圖示的避讓

## Done Recently
見 `progress.md` 的 Completed 區塊與 `session-log.md`。

## Last Update
2026-10-24
