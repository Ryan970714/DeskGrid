# Todo

## High Priority
- 使用者手動驗證階段 1
- 在 Win10 22H2（或 Win11 23H2）驗證舊版桌面階層路徑（可用 VirtualBox VM）

## Medium Priority
- 階段 2 開始前需確認的設計問題：
  - config 中 widget 位置要存實體像素還是 DIP？（影響換 DPI / 換螢幕後的行為）
  - 根目錄改變時，舊 config 中的 widget 項目要保留還是清除？

## Low Priority
- 關閉「顯示桌面圖示」時 widget 是否跟著隱藏
- 執行中 DPI 變更（`WM_DPICHANGED_AFTERPARENT`）處理

## Future Ideas
- 多螢幕支援（MVP 後）
- widget 下方桌面圖示的避讓

## Done Recently
見 `progress.md` 的 Completed 區塊與 `session-log.md`。

## Last Update
2026-10-24
