# Todo

## High Priority
- 使用者手動驗證階段 5（含：其他 App 在前景時點 widget 也能開面板並取得焦點）
- 在 Win10 22H2（或 Win11 23H2）驗證舊版桌面階層與面板非 Acrylic 路徑（可用 VirtualBox VM）

## Medium Priority
- 階段 6 開始前需確認：設定視窗內容與版面（根目錄、欄數、拖入模式、面板背景含自訂色）、拖放加入的細節、開機自啟方式（登錄 Run 機碼 / 工作排程 / 啟動資料夾）
- 階段 6：移除 widget 右鍵的暫時性「結束」選單；字串 `MenuExit` 沿用到系統匣
- 階段 6：設定變更即時套用（根目錄變更需重建 catalog / board）

## Low Priority
- 面板開著時拖曳另一個 widget → 關閉面板
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
