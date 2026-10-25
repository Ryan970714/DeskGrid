# Decisions

> 只能新增，不得覆蓋或刪除既有紀錄。若某個決策後來被推翻，新增一筆新紀錄說明並引用原紀錄的日期，不要修改原紀錄。

---

### 2026-10-24 — 目標框架改用 .NET 10 LTS
**Decision**: TargetFramework 用 `net10.0-windows`，SDK 以 winget 安裝 Microsoft.DotNet.SDK.10。
**Reason**: 原規格 .NET 8 於 2026-11-10 結束支援；.NET 10 LTS 支援到 2028-11。
**Alternatives**: 維持 .NET 8（17 天後無安全更新）。
**Consequences**: CLAUDE.md 技術棧更新；`global.json` 鎖 10.0.x。
**Author**: 使用者決定

---

### 2026-10-24 — Widget 掛在 SHELLDLL_DefView、圖示之上
**Decision**: surface 為 `SHELLDLL_DefView` 的子視窗，z-order 在 `SysListView32` 之上。
**Reason**: widget 要能點擊、拖動；放在圖示後方（wallpaper engine 的 WorkerW 做法）會被圖示 list view 攔截所有滑鼠輸入。掛 DefView 而非其父視窗：DefView 在新舊階層都存在，且在舊版被 Explorer 搬到 WorkerW 時 widget 會跟著走。
**Alternatives**: 掛在 WorkerW（圖示後方，無輸入）；掛在 Progman/WorkerW 容器（舊版 DefView 被搬走時會被留在後方）；top-level HWND_BOTTOM 視窗（Win+D 時會被蓋掉，需額外偵測）。
**Consequences**: widget 會蓋住其下方圖示；「隱藏桌面圖示」時 widget 仍顯示。
**Author**: Claude（依規格「掛在桌面圖示層」推論）

---

### 2026-10-24 — Surface 使用 layered 子視窗（UsesPerPixelTransparency）
**Decision**: `HwndSource` + `WS_CHILD` + `UsesPerPixelTransparency`（→ `WS_EX_LAYERED`）。
**Reason**: 24H2+ Progman 帶 `WS_EX_NOREDIRECTIONBITMAP`，非 layered 子視窗不會被畫出；同時取得逐像素透明與透明處點穿。
**Alternatives**: WPF `Window` + `SetParent`（Window 不適合當子視窗，layered 位置處理易出錯）。
**Consequences**: WPF 以軟體渲染 + UpdateLayeredWindow；小尺寸 widget 效能可接受。
**Author**: Claude

---

### 2026-10-24 — 重新掛載 = 重建 HwndSource
**Decision**: 每次（重新）掛載都建立新的 `HwndSource`，沿用同一個 WPF content 物件。
**Reason**: 實測 explorer 結束時跨行程子視窗會被銷毀；單一程式路徑最簡單，且 content 狀態保留。
**Alternatives**: 對仍存活的 HWND 做 `SetParent`（多一條路徑，實測不會發生）。
**Consequences**: 偵測點為 `HwndSource.Disposed`（→ `DesktopSurface.Lost`）＋ `TaskbarCreated`，以 1 秒 `DispatcherTimer` 重試到成功。
**Author**: Claude

---

### 2026-10-24 — Win32 互通用 CsWin32
**Decision**: P/Invoke 由 CsWin32 產生（`NativeMethods.txt`）。
**Reason**: 規格建議；後續階段需要 COM（IShellItemImageFactory、IShellLink），CsWin32 可直接產生。
**Alternatives**: 手寫 `LibraryImport`。
**Consequences**: `AllowUnsafeBlocks=true`；CsWin32 未含 `HWND_TOP` 等巨集，需自行定義。
**Author**: Claude

---

### 2026-10-24 — 文件語言
**Decision**: CLAUDE.md / AGENTS.md / context 用繁體中文；程式碼、註解、commit 用英文。
**Reason**: 與使用者規格語言一致；開源程式碼維持英文。
**Alternatives**: 全英文。
**Consequences**: 階段 7 的 README 以英文撰寫。
**Author**: Claude

---

### 2026-10-24 — 階段 2 config 語意
**Decision**: widget 位置存實體螢幕像素（載入時跑出工作區就夾回）；面板欄數為全域一個設定；重掃時清除已不存在資料夾/項目的設定（換根目錄等同全新開始）；啟動時根目錄不合格 → 提示後改用預設並寫回 config。
**Reason**: 實體像素與 `DesktopSurface` 一致，改縮放比例時位置不跑；全域欄數像手機資料夾且不需 per-widget UI；config 只反映現存資料夾。
**Alternatives**: DIP / 工作區相對比例；per-widget 欄數；永久保留殘留設定；提示後結束程式。
**Consequences**: 刪除後從資源回收筒還原的資料夾會回到預設位置。
**Author**: 使用者決定

---

### 2026-10-24 — 掃描與 config 協調拆成 FolderScanner + WidgetCatalog
**Decision**: `FolderScanner` 只管檔案系統（掃描、watcher、debounce、rename 事件）；`WidgetCatalog` 把掃描結果與 config 合併（排序、清除、rename 轉移、存檔）。
**Reason**: 單一職責；合併邏輯（`Reconcile`）是純函式，可不碰磁碟直接測。
**Alternatives**: FolderScanner 直接依賴 ConfigStore 一手包辦。
**Consequences**: 之後的 UI 只訂閱 `WidgetCatalog.Changed`。
**Author**: Claude

---

### 2026-10-24 — 事件序列化靠 SynchronizationContext，debounce 後整個重掃
**Decision**: `FolderScanner` 捕捉建立時的 `SynchronizationContext`（沒有就丟例外），所有事件依序 post 回去；debounce 結束後整個根目錄重掃，不做增量更新。
**Reason**: config 只在 UI 執行緒存取，不需鎖；rename 一定先於後續 refresh。資料量小（數十資料夾 × 數十項目），重掃成本可忽略且不會漏事件。
**Alternatives**: 背景執行緒 + lock；依事件做增量更新。
**Consequences**: 測試需在 WPF Dispatcher 執行緒上建立 scanner（`DispatcherThread` helper）。
**Author**: Claude

---

### 2026-10-24 — 項目範圍與預設排序
**Decision**: 項目 = widget 資料夾內的可見檔案（略過隱藏/系統檔、子資料夾、根目錄散檔）；首次依 Explorer 自然排序，之後新項目接在最後。
**Reason**: 規格支援 .lnk/.url/.exe/一般檔案，不含資料夾；desktop.ini、Office 暫存檔不該出現；接在最後符合手機加入 App 的直覺。
**Alternatives**: 依名稱或修改時間每次重排；子資料夾也顯示為項目。
**Consequences**: 目前沒有手動重排 UI，排序只由加入順序決定。
**Author**: Claude（慣例預設，已於階段回報告知使用者）

---

### 2026-10-24 — 測試框架 xUnit v3
**Decision**: `tests/DeskFolders.Tests`，xUnit v3 3.2.2 + VSTest runner；`Log.FilePath` 可由測試改寫，避免寫進真實 log。
**Reason**: 開源專案常見；Config/Folders 是純邏輯，適合自動測試。
**Alternatives**: MSTest、NUnit；只做手動測試。
**Consequences**: `InternalsVisibleTo("DeskFolders.Tests")`；共用設定移到 `Directory.Build.props`。
**Author**: Claude

---

### 2026-10-24 — 項目一律顯示圖示，不用內容縮圖
**Decision**: `SIIGBF_ICONONLY`；圖片、PDF、影片也顯示檔案類型圖示。
**Reason**: 照規格「取圖示」；全部正方形、風格一致像手機資料夾；不觸發縮圖產生，較快。
**Alternatives**: `SIIGBF_RESIZETOFIT`（有縮圖就用，非正方形需加邊）。
**Consequences**: 看不到圖片內容。
**Author**: 使用者決定（附實測對照圖）

---

### 2026-10-24 — Shell 圖示以直通 alpha 解讀
**Decision**: 從 DIB section 複製像素，以 `PixelFormats.Bgra32` 建立。
**Reason**: 實測多個圖示有 RGB > A 的像素，預乘格式不可能出現，故為直通 alpha；`CreateBitmapSourceFromHBitmap` 會丟 alpha。
**Alternatives**: `Pbgra32`（邊緣會偏亮，alpha=0 但有色的像素會變成可見雜訊）。
**Consequences**: 若某些 Shell handler 回傳預乘資料，邊緣會略暗（未觀察到）。
**Author**: Claude

---

### 2026-10-24 — IconService 執行緒與快取
**Decision**: Shell 呼叫與合成在單一專用 STA 執行緒；快取只放記憶體，以 (路徑, LastWriteTimeUtc) 為 key 並快取 `Task`；預覽依前 4 項的 key 決定是否重建；`Retain` 隨 catalog 刷新清除。
**Reason**: Shell handler 可能很慢，不可卡 UI；frozen bitmap 可跨執行緒；mtime 讓「檔案變動才重建」不需要額外事件。
**Alternatives**: UI 執行緒同步取圖；磁碟快取；多執行緒取圖。
**Consequences**: 啟動時每個項目要向 Shell 取一次；捷徑目標圖示變了但 .lnk 沒變時不會更新。
**Author**: Claude

---

### 2026-10-24 — Widget 外觀、尺寸、預設位置、對齊格線
**Decision**: 淺色半透明圓角方塊；48 DIP（與原生中型圖示同大）；新 widget 從主螢幕工作區右上角往下排；放開後對齊格線。
**Reason**: 使用者看過對照圖（A/B/C × 48/64/80）後選定。
**Alternatives**: 深色半透明 / 無底框；64、80 DIP；左上角 / 螢幕中央；自由擺放。
**Consequences**: 內部小圖示約 18 DIP；右側有原生圖示時會疊在一起。
**Author**: 使用者決定

---

### 2026-10-24 — 格子定義與衝突處理
**Decision**: 格子 = widget 佔位 76×96 DIP（依螢幕 DPI 換成實體像素），錨定工作區右上角；一格一個 widget；載入與放開都吸到「最近的空格」。
**Reason**: 76×96 與原生圖示格距接近；同一規則處理拖放、載入、DPI 變更後的舊座標，位置永遠在格上且不重疊。
**Alternatives**: 允許重疊；放到有人的格子時交換位置；錨定左上角。
**Consequences**: 放到有人的格子會跑到次近空格；格子與 Windows 原生圖示格不對齊（原點不同）。
**Author**: Claude（使用者選「對齊格線」後的實作細節，已於回報告知）

---

### 2026-10-24 — 暫時性結束入口
**Decision**: 階段 4–5 期間，widget 右鍵選單提供「結束 DeskFolders」。
**Reason**: 移除 spike 後在系統匣（階段 6）之前沒有結束方式；右鍵 → 選單比直接右鍵結束不易誤觸。
**Alternatives**: 只能用工作管理員結束；提前做系統匣。
**Consequences**: 階段 6 要移除，字串 `MenuExit` 會沿用到系統匣選單。
**Author**: Claude

---

### 2026-10-24 — 面板位置、大小、標題、可自訂背景
**Decision**: 面板開在 widget 旁；項目 48 DIP；顯示資料夾名稱標題；背景可自訂：三種預設（system 毛玻璃跟隨系統〔預設〕/ dark / light）+ custom（顏色 + 不透明度）；config 新增 `panelBackground`（超出原規格 5 項）；設定 UI 放階段 6。
**Reason**: 使用者選定與同意。
**Alternatives**: 螢幕中央；64 DIP；無標題；固定單一背景。
**Consequences**: 階段 6 設定視窗需包含背景選擇與顏色/不透明度。
**Author**: 使用者決定

---

### 2026-10-24 — 面板行為預設
**Decision**: 最多顯示 4 列後捲動；點項目啟動後收合；再點同一 widget 關閉、點另一 widget 直接切換；同時一個面板；項目名稱用 Shell 顯示名稱；空資料夾顯示提示文字。
**Reason**: 慣例（手機資料夾 / 系統彈出選單）；Shell 顯示名稱與檔案總管一致。
**Alternatives**: 依螢幕高度決定列數；啟動後保持開啟；檔名直接顯示。
**Consequences**: 無。
**Author**: Claude（已於階段回報告知）

---

### 2026-10-25 — Acrylic 面板的視窗做法
**Decision**: Acrylic 樣式用 `SingleBorderWindow` + `WindowChrome(GlassFrameThickness=-1, CaptionHeight=0)`，拿掉 `WS_SYSMENU`，自己 Measure 內容設定 Width/Height（不用 SizeToContent）；其他樣式用 layered 視窗。
**Reason**: 實測無邊框 `WS_POPUP` 拿不到 system backdrop；有 `WS_SYSMENU` 時 DWM 會在延伸框架畫關閉鈕；SizeToContent 會多算已被移除的外框，視窗過大且超出螢幕。
**Alternatives**: 自己模擬模糊（layered 無法取樣背後內容）；只提供純色。
**Consequences**: Acrylic 與 layered 必須在建立視窗前決定，因此每次開面板都建新視窗（也順便讀到最新設定）。
**Author**: Claude

---

### 2026-10-25 — 桌面 surface 不啟用桌面（MA_NOACTIVATE）
**Decision**: `DesktopSurface` 處理 `WM_MOUSEACTIVATE` 回 `MA_NOACTIVATE`。
**Reason**: 實測面板開著時點 widget，啟用 Explorer 桌面的那次點擊被 Explorer 吞掉，widget 收不到按下（點另一個 widget 要點兩次）。不啟用桌面後，面板的開關完全由 `PanelController` 決定，不再需要「關閉後 500ms 不重開」的防護。
**Alternatives**: 保留桌面啟用 + 計時防護 + 重新捕捉滑鼠（試過，按下根本沒送達，無效）。
**Consequences**: 點 widget 不會讓桌面成為前景視窗；面板開著時拖曳 widget 不會關閉面板。
**Author**: Claude

---

### 2026-10-25 — 啟動項目用 Process.Start(UseShellExecute)
**Decision**: 以 `Process.Start` + `UseShellExecute=true`（內部即 ShellExecuteEx）啟動；工作目錄 = 項目所在資料夾；`ErrorDialog=true`。
**Reason**: 符合「一律用 ShellExecute」，比手寫 SHELLEXECUTEINFO 簡潔；行為與檔案總管雙擊一致；錯誤由 Windows 自己顯示。
**Alternatives**: CsWin32 直接呼叫 `ShellExecuteEx`。
**Consequences**: 失敗（含 UAC 取消）只記 log。
**Author**: Claude

---

### 2026-10-25 — 拖放規則與開機自啟方式
**Decision**: 一般檔案 → 建捷徑；非桌面的 `.lnk`/`.url` → 一律複製；資料夾 → 建捷徑；`.exe` → 建捷徑（規格）；桌面捷徑 → 依拖入模式（規格）。開機自啟用 HKCU Run 機碼。
**Reason**: 使用者選定；建捷徑不會意外搬走使用者的文件；不破壞開始功能表等原處。Run 機碼免管理員、會出現在工作管理員「啟動應用程式」。
**Alternatives**: 一般檔案依拖入模式或一律複製；其他捷徑跟桌面一樣；工作排程；啟動資料夾。
**Consequences**: Run 值存在但工作管理員停用時視為關閉，我們切換時清掉該旗標。
**Author**: 使用者決定

---

### 2026-10-25 — 階段 6 慣例預設
**Decision**: 只有 widget 接受拖放（面板不接受）；原生 Win32 系統匣選單，左右鍵都開；設定視窗確定/取消、非模態、只能開一個；同名加「 (2)」；公用桌面捷徑沒權限移動就改複製；單一執行個體（`Local\` mutex）；自畫 App 圖示；拖放後對來源回報 Copy。
**Reason**: 慣例且簡單；回報 Move 會讓來源刪除它以為還在的檔案。
**Alternatives**: 面板也接受拖放；WPF ContextMenu；即時套用設定；允許多開。
**Consequences**: 原生選單在深色模式下為淺色。
**Author**: Claude（已於階段回報告知）

---

### 2026-10-25 — Shell 互通集中到 Shell/
**Decision**: `ShellImage`（原 Icons/）、`ShellItem`（原 Panel/）與新的 `ShortcutFile` 移到 `Shell/`。
**Reason**: 都是 Shell COM / ShellExecute 互通，且被多個模組使用（Icons、Panel、Folders、App）。
**Alternatives**: 留在各自使用的模組。
**Consequences**: namespace `DeskFolders.Shell`。
**Author**: Claude

---

### 2026-10-25 — XAML 不引用本組件型別
**Decision**: 對話框文字在 code-behind 設定，不用 `x:Static res:Strings.*`；`Strings` 維持 internal。
**Reason**: XAML 引用本地型別會觸發 WPF 暫時標記組件編譯，而 `Strings` 由 resx 在 build 時產生於 `obj/`，暫時組件編譯不到。
**Alternatives**: 把產生的 Strings.Designer.cs 放進原始碼（需手動同步）；自訂 build target 提早產生（會重複加入 Compile）。
**Consequences**: 每個視窗多一段 `LocalizeTexts()`。
**Author**: Claude

---

### 2026-10-25 — 發布：公開 repo、tag 自動 Release、MIT（Ryan Kuo）、雙語 README
**Decision**: 公開 repo `Ryan970714/DeskFolders`；push/PR 跑 CI（build、test、publish artifact）；推 `v*` tag 自動建 Release（附單一 exe 與 SHA256）；LICENSE 著作權人 Ryan Kuo；README 英文 + 繁中。
**Reason**: 使用者選定。
**Alternatives**: 私人 repo；只要 CI；只有英文 README。
**Consequences**: CLAUDE.md、context/ 也會公開（已掃描無個人資訊）；推送 workflow 檔需要 gh `workflow` 權限。
**Author**: 使用者決定

---

### 2026-10-25 — 打包設定
**Decision**: publish profile `win-x64`：self-contained + PublishSingleFile + IncludeNativeLibrariesForSelfExtract + 壓縮 + 無 pdb，輸出到 `artifacts/publish/`。CI 用 `dotnet-version: 10.0.x`（global.json 的 rollForward 仍適用）。README 不放截圖（避免露出個人桌面）。
**Reason**: 真正的單一 exe；壓縮後約 62 MB；WPF 不能 trim。
**Alternatives**: 框架相依（小但需安裝 .NET 10 Desktop Runtime）；不壓縮（約 150 MB，啟動略快）。
**Consequences**: 首次執行會把 WPF 原生 DLL 解到 %TEMP%。
**Author**: Claude

---
