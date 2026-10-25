# Current Task

## Current Goal
階段 7：GitHub Actions CI、self-contained 打包、README（含 SmartScreen 說明）、LICENSE (MIT)。

## Current Task
本機完成並已 commit；等使用者授權 gh `workflow` 權限後建立公開 repo 並推送，確認 CI 通過。

## Current Status
- 使用者決定：公開 repo `Ryan970714/DeskFolders`；CI + 推 tag 自動 Release；LICENSE 著作權人 Ryan Kuo；README 英文 + 繁中
- `dotnet publish -p:PublishProfile=win-x64` → 62 MB 單一 exe，實際執行成功（版本 0.1.0、版權資訊正確）
- workflows：ci.yml（push main / PR）、release.yml（v* tag，附 exe 與 .sha256）；action 版本 checkout@v7、setup-dotnet@v6、upload-artifact@v7
- 公開前掃描：無個人路徑 / Email / token；commit 作者為 GitHub noreply 信箱
- gh 已登入 Ryan970714，但 token 只有 gist / read:org / repo，推送 workflow 檔需要 `workflow` 權限

## Current File
`.github/workflows/*`、`README*.md`、`LICENSE`、`src/DeskFolders/Properties/PublishProfiles/win-x64.pubxml`

## Next Step
1. 使用者執行 `gh auth refresh -h github.com -s workflow`
2. `gh repo create Ryan970714/DeskFolders --public --source . --remote origin --push`
3. 確認 CI 通過
4. 是否推 `v0.1.0` tag 發布第一個 Release（使用者決定）

## Blocking Issues
gh token 缺 `workflow` 權限（需使用者在瀏覽器授權）。

## Last Update
2026-10-25
