# Todo

## High Priority
- 使用者手動驗證階段 4（含 explorer 重啟時多個 widget 重掛）
- 在 Win10 22H2（或 Win11 23H2）驗證舊版桌面階層路徑（可用 VirtualBox VM）

## Medium Priority
- 階段 5 開始前需確認：面板外觀、出現位置（widget 旁 / 螢幕中央）、項目圖示大小、是否顯示資料夾名稱標題
- 階段 5：面板項目圖示用 `IconService.GetIconAsync`，延遲載入；`PanelColumns` 夾在合理範圍（手改 config 可能填 0 或負數）
- 階段 5：widget 點擊目前只寫 log → 改為開面板
- 階段 6：移除 widget 右鍵的暫時性「結束」選單

## Low Priority
- 執行中解析度 / DPI / 工作列變更時重新吸附 widget（`WM_DISPLAYCHANGE`、`WM_SETTINGCHANGE`）
- 拖曳中 Esc 取消
- 關閉「顯示桌面圖示」時 widget 是否跟著隱藏
- 執行中根目錄被刪除時自動恢復 watcher
- 捷徑目標圖示更新時刷新快取（目前要重啟）

## Future Ideas
- 多螢幕支援（MVP 後；需處理副螢幕 DPI 與 Progman 渲染 DPI 不同）
- widget 避開原生桌面圖示

## Done Recently
見 `progress.md` 的 Completed 區塊與 `session-log.md`。

## Last Update
2026-10-24
