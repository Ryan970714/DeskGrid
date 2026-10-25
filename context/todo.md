# Todo

## High Priority
- 使用者手動驗證階段 6（特別是：從檔案總管桌面拖捷徑 / exe / 檔案 / 資料夾到 widget）
- 在 Win10 22H2（或 Win11 23H2）驗證舊版桌面階層與面板非 Acrylic 路徑（可用 VirtualBox VM）

## Medium Priority
- 階段 7 開始前需確認：GitHub repo 名稱、公開或私人、發布方式（GitHub Releases 附 self-contained exe？）、CI 範圍（build + test）
- 面板開著時拖曳另一個 widget → 關閉面板

## Low Priority
- 系統匣選單 / 設定視窗跟隨深色模式
- 從開始功能表拖 UWP App（用 IShellLink SetIDList 建捷徑）
- 面板方向鍵導覽
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
2026-10-25
