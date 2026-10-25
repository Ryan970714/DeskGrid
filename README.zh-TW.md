# DeskFolders

[English](README.md)

把手機式資料夾放到 Windows 桌面上。每個資料夾以小方塊的樣子排在桌面圖示之間，顯示裡面項目的
2×2 預覽；點一下就展開成格子面板，再點項目即可啟動。

每個資料夾都對應磁碟上一個真實的資料夾，在那裡做的任何變更都會立刻反映到桌面上。

- 位於桌面圖示層：在所有視窗後方、按 Win+D 後仍在、檔案總管重新啟動後會自動回來
- 把捷徑、程式、檔案或資料夾拖到資料夾上即可加入
- 資料夾會對齊格線，並記住你擺放的位置
- 系統匣選單：新增資料夾、開啟根目錄、設定、開機時啟動、結束
- 面板背景：Windows 11 毛玻璃（跟隨系統深淺色）、深色、淺色，或自訂顏色
- 介面支援英文與繁體中文，依 Windows 顯示語言自動切換

## 系統需求

Windows 10 22H2 或 Windows 11，64 位元。不需安裝 .NET：下載的是自帶執行環境的單一 exe。

## 安裝

1. 到 [Releases](../../releases) 下載 `DeskFolders-<版本>-win-x64.exe`。
2. 放到任意位置（例如 `%LOCALAPPDATA%\Programs\DeskFolders\`）後執行。
3. 可選：在系統匣選單開啟 **開機時啟動**。

### 「Windows 已保護您的電腦」

這個 exe 沒有程式碼簽章（簽章憑證需要付費），所以在足夠多人執行過之前，Microsoft Defender
SmartScreen 會跳出警告。要繼續執行，請按 **其他資訊** → **仍要執行**。
若檔案被封鎖，請在檔案上按右鍵 → **內容** → 勾選 **解除封鎖** → **確定**。

每個 Release 都附有 `.sha256` 檔。檢查下載的檔案是否一致：

```powershell
Get-FileHash .\DeskFolders-0.1.0-win-x64.exe -Algorithm SHA256
```

如果不想信任現成的執行檔，也可以[自己從原始碼建置](#從原始碼建置)。

## 使用方式

第一次執行時會建立 `%USERPROFILE%\DeskFolders\`。**其中每個子資料夾都會成為桌面上的一個資料夾**，
裡面的檔案就是它的項目。

| 想要 | 做法 |
|---|---|
| 建立資料夾 | 系統匣選單 → **新增資料夾…**，或直接在根目錄建立子資料夾 |
| 開啟資料夾 | 點它，再點項目即可啟動。按 Esc、點其他地方或再點一次資料夾就會收合 |
| 移動資料夾 | 拖曳它，放開後會對齊到最近的空格 |
| 重新命名或刪除 | 在檔案總管裡重新命名或刪除該子資料夾 |
| 加入項目 | 拖到資料夾上（見下表），或直接把檔案放進子資料夾 |

拖到資料夾上時：

| 拖入的項目 | 結果 |
|---|---|
| 程式（`.exe`） | 建立指向它的捷徑，絕不移動或複製程式本體 |
| 放在桌面上的捷徑（`.lnk`、`.url`） | 移進資料夾（可在設定改成複製） |
| 其他地方的捷徑（例如開始功能表資料夾） | 複製 |
| 其他檔案，或資料夾 | 建立指向它的捷徑，原檔留在原處 |

項目依加入的先後排序，新項目排在最後。

### 設定

系統匣選單 → **設定…**：根目錄、面板欄數（1–8）、桌面捷徑要移動還是複製，以及面板背景。
根目錄不能放在桌面上（否則其中的資料夾會重複出現）。

### 檔案位置

| 內容 | 位置 |
|---|---|
| 設定與資料夾位置 | `%APPDATA%\DeskFolders\config.json` |
| Log | `%LOCALAPPDATA%\DeskFolders\deskfolders.log` |
| 你的資料夾 | `%USERPROFILE%\DeskFolders\`（或你選擇的根目錄） |

### 解除安裝

關閉 **開機時啟動**、從系統匣選單結束，然後刪除 exe。若也要移除設定，再刪除
`%APPDATA%\DeskFolders` 與 `%LOCALAPPDATA%\DeskFolders`。你的資料夾與其中的檔案不會被動到。

## 已知限制

- 資料夾不會避開桌面原生圖示，重疊時請自行拖開。
- 執行中變更解析度、縮放比例或工作列時，資料夾要重新啟動後才會重新對齊。
- 尚未支援縮放比例不同的多螢幕。
- 無法加入從開始功能表拖來的 App（背後沒有檔案）。
- 以系統管理員身分執行時，Windows 會擋掉從檔案總管拖來的檔案。
- 深色模式下系統匣選單與設定視窗仍是淺色。
- 目前只在 Windows 11 上測試過；Windows 10 的桌面結構有處理，但尚未實機驗證。

## 從原始碼建置

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download)。

```powershell
dotnet build DeskFolders.slnx
dotnet test DeskFolders.slnx
dotnet publish src/DeskFolders/DeskFolders.csproj -p:PublishProfile=win-x64   # -> artifacts/publish/DeskFolders.exe
```

推送 `v0.1.0` 這類 tag 時，GitHub Actions 會自動發布 Release。

### 運作原理

每個資料夾都是一個逐像素透明的小型 WPF 視窗，掛在檔案總管放桌面圖示的 `SHELLDLL_DefView` 底下、
圖示清單的上方。這讓它位於其他視窗後方卻仍可點擊，而且同時適用傳統桌面結構與 Windows 11 24H2
的新結構。檔案總管重新啟動時會連同這些視窗一起銷毀，DeskFolders 會自動重新掛上。
設計筆記與決策紀錄在 `CLAUDE.md` 與 `context/`。

## 授權

[MIT](LICENSE) © 2026 Ryan Kuo
