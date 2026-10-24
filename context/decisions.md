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
