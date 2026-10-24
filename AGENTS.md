# Language

- 回答使用者：繁體中文
- Code（變數、函式、註解）：英文
- Commit message：英文（conventional commits）
- `CLAUDE.md`、`AGENTS.md`、`context/*.md`：繁體中文；之後的 README 以英文為主（開源）

# Coding Principles

遵守 SOLID、DRY、KISS、Clean Code、Clean Architecture、YAGNI。

專案慣例：

- 每個模組一個資料夾，file-scoped namespace `DeskFolders.<Folder>`
- 類別預設 `internal sealed`；private 欄位 `_camelCase`
- Win32 API 一律透過 CsWin32（加到 `src/DeskFolders/NativeMethods.txt`），不手寫 `DllImport`
- 建置零警告（`TreatWarningsAsErrors`）；CA1416 已在 `Directory.Build.props` 以註解說明原因後關閉
- 純邏輯（Config、Folders 等）要有 xUnit 測試；使用者介面文字一律放 `Resources/Strings*.resx`（en + zh-TW）
- 不使用第三方 UI 框架

# 修改程式前

1. 分析需求
2. 分析影響範圍（哪些檔案、哪些呼叫端會受影響）
3. 若需求模糊 → 先詢問，不要自行猜測

# 修改程式後

1. 檢查 Bug
2. 檢查副作用（會不會影響其他頁面/元件/呼叫端）
3. 提出可改善建議（若有）

# 禁止事項

- 大量重複程式
- Copy & Paste（應抽共用邏輯）
- 過度設計（YAGNI——不要為了假設中的未來需求先做抽象）
- 自行更改需求（需求不清楚要問，不要自己決定改成別的做法）

# Markdown 自動維護

工作完成後自動維護 `context/*.md`（見 `CLAUDE.md` 的 Session 結束流程），不要等待使用者提醒。新增功能、修改功能、修 Bug、Refactor、更新 Architecture/API/Folder 都要同步更新相關 Markdown。
