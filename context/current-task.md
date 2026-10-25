# Current Task

## Current Goal
階段 6：拖放加入、TrayApp、開機自啟（含設定視窗、新增資料夾、單一執行個體）。

## Current Task
等待使用者手動驗證階段 6。

## Current Status
實作完成並已 commit。驗證：
- `dotnet test`：72 個測試通過（新增拖放規則、捷徑建立、同名編號、資料夾命名、開機自啟含工作管理員旗標）
- 實機（模擬輸入，使用者同意）：系統匣選單（勾選狀態）、設定視窗開關與版面、新增資料夾對話框、開機自啟開→關（登錄已還原為無）、單一執行個體
- 實機 OLE 拖放（自製 WPF 拖曳來源 → Test widget）：假 exe → `probe-tool.lnk`（原檔不動）、txt → `drop-notes.txt.lnk`、桌面暫存捷徑 → 移進 Test（桌面上已消失）
- **未驗證**：從檔案總管桌面直接拖圖示到 widget（測試腳本 bug 中止，見下）
- 修掉的 bug：manifest 缺 Common Controls v6（LoadIconMetric 啟動即當）；非模態設定視窗 Esc/取消不會關；不透明度列太寬
- 拖放「失敗」其實是測試腳本 bug（`powershell -File` 把陣列串成一個字串），app 本身正確
- 測試事故：Explorer 拖曳腳本的函式名 `Move` 被 PowerShell 的 `Move-Item` 別名蓋掉，游標沒移動，卻在使用者當時游標位置（可能是 Chrome）送出一次左鍵點擊；`Move-Item` 呼叫因路徑不存在全部失敗，未搬動檔案。之後停止所有模擬輸入。
- `%USERPROFILE%\DeskFolders\Test` 內留有拖放測試產物（probe-tool.lnk、drop-notes.txt.lnk、DeskFolders Drop Probe.lnk），桌面暫存捷徑已刪除。使用者自建的 `AI` 資料夾未動。

## Current File
`src/DeskFolders/{Tray,Settings,Shell}/*`、`src/DeskFolders/Folders/ItemImporter.cs`

## Next Step
使用者驗證通過後開始階段 7（GitHub Actions CI、self-contained 打包、README 含 SmartScreen 說明、LICENSE MIT）。開始前確認 GitHub repo 名稱 / 公開或私人、發布方式。

## Blocking Issues
無。

## Last Update
2026-10-25
