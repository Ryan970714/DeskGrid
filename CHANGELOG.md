# Changelog

## 0.2.3

### Changed

- The settings window uses the Fluent look: settings are grouped into rounded cards, and the window follows the system
  light or dark mode.

### Fixed

- The taskbar shortcut to all folders is now named "DeskGrid" in every language; a shortcut left under the old name
  is removed when it is created again. A taskbar pin made from the old shortcut keeps its name: unpin it and pin the
  new one.
- The plus on the "new folder" tile on the desktop sat off the tile's center.
- Switching from the folder list to a folder with more items, or a folder gaining items while its panel was open,
  didn't resize the panel.
- Clicking a taskbar pin while its panel was open reopened the panel instead of closing it.
- After a drag in a panel, a rescan held back during the drag could undo the new order.
- Dropping something into a panel that couldn't be added left the gap opened for it.

## 0.2.2

### Changed

- The Update button now does the whole update in one dialog and shows its status live: the download with percent and
  size, then verifying the checksum, then installing and restarting. If the swap fails, the error is shown in the
  dialog and the update can be retried, instead of opening the release page.

## 0.2.1

### Fixed

- Shortcuts to Microsoft Store apps (for example Slack and WhatsApp) showed a blank document icon in widget previews
  and panels. These shortcuts have no file path, only an AppUserModelID, so the Shell was asked for the icon of the
  `.lnk` file itself. DeskGrid now looks the app up in the Apps folder and asks for its logo. Some packages have no logo
  of the size the icon-only request wants (WhatsApp); for those the request is repeated without icon-only, which finds
  a smaller logo.
- Dragging a folder in the "all folders" panel lifted only its preview image, which looked enlarged and lost the
  translucent tile under it. The drag ghost now carries the tile with the same padding as the grid cell.

### Added

- The folders in the "all folders" panel (opened from the taskbar) can be rearranged by dragging, with the same sliding
  animation as items inside a folder. The order is saved in `config.json` as `folderOrder`, new folders go last, and a
  renamed folder keeps its place. Dragging a folder never moves or copies it on disk.

### Changed

- The title of the "all folders" panel is now "DeskGrid".

## 修正紀錄（繁體中文）

### 0.2.3

#### 變更

- 設定視窗改用 Fluent 外觀：設定分組成圓角卡片，並跟隨系統淺色 / 深色模式。

#### 修正

- 「所有資料夾」的工作列捷徑在任何語言下都叫「DeskGrid」；重新建立時會刪掉舊名稱的捷徑。用舊捷徑釘選的工作列按鈕
  名稱不會變，請取消釘選後改釘新的。
- 桌面上「新增資料夾」磚的 + 不在正中間。
- 從資料夾列表點進項目較多的資料夾，或面板開著時資料夾新增了項目，面板大小沒有跟著變。
- 面板開著時再點工作列按鈕，面板會重新打開而不是收合。
- 面板內拖曳後，拖曳中延後的重掃可能蓋掉新的排序。
- 拖進面板但沒有任何項目可加入時，為它開的缺口不會合起來。

### 0.2.2

#### 變更

- 「更新」按鈕現在在同一個對話框內完成整個更新，並即時顯示狀態：下載（百分比與大小）、驗證校驗碼、安裝並重新啟動。
  換檔失敗時錯誤會顯示在對話框內，可以重試，不再改開 release 頁面。

### 0.2.1

#### 修正

- Microsoft Store App（例如 Slack、WhatsApp）的捷徑在 widget 預覽與面板中顯示空白文件圖示。這類捷徑沒有檔案路徑，
  只有 AppUserModelID，先前是對 `.lnk` 檔案本身要圖示。現在改為在「應用程式」資料夾中找到該 App 並取得它的 logo。
  部分套件（WhatsApp）沒有 icon-only 要求的尺寸，此時改用不帶 icon-only 的要求，取得較小的 logo。
- 在「所有資料夾」面板拖曳資料夾時，影子只有預覽圖，看起來被放大且少了底下的半透明底板。影子現在帶著底板，內距與格子相同。

#### 新增

- 「所有資料夾」面板（從工作列開啟）中的資料夾可以拖曳排序，滑動動畫與資料夾內項目排序相同。順序存在
  `config.json` 的 `folderOrder`，新資料夾排最後，資料夾改名後順位不變。拖曳資料夾不會在磁碟上移動或複製它。

#### 變更

- 「所有資料夾」面板的標題改為「DeskGrid」。
