# YnyrWASD 使用說明

免費開源的 Windows 工具：讓遊戲把你的 Xbox 或 Nintendo Switch 2 Pro 手把當成
PlayStation DualShock 4，顯示 PS 按鍵圖示。對應付費軟體 reWASD 最常用的「偽裝成 DS4」功能，
改用免費元件 ViGEmBus（虛擬 DS4）與 HidHide（隱藏實體手把）完成。
目前為 **0.4.0 預覽版**，介面支援繁體中文與英文。

![主視窗](images/editor.zh-TW.png)

## 功能

- **自動偵測輸入（預設）：** 同時監看 NS2 Pro（USB）與 Xbox／XInput 手把，按哪一支就用哪一支，
  閒置的手把不會搶走控制權；也可以指定只用其中一種。
- **持續存在的虛擬 DS4：** 整段映射期間保持連線；手把斷線時輸出歸零，驅動出錯會自動重連。
- **自動隱藏實體手把：** 安裝 HidHide 後，啟動映射就會把實體手把藏起來，遊戲只看得到虛擬 DS4，
  圖示不會在 Xbox／PS 之間跳動。停止映射會完整還原原本的 HidHide 設定。
- **處理 Steam：** 偵測到 Steam 比映射早開（Steam Input 會繼續轉送實體手把）時，提示並可一鍵重啟 Steam。
- **震動、PS 鍵與體感：** 遊戲的震動會轉給 Xbox 或 NS2 Pro；Xbox 的 Guide 鍵與 NS2 的 Home 鍵當作 PS 鍵，
  NS2 的截圖鍵當作觸控板按下；NS2 Pro 的陀螺儀與加速度計會轉成 DS4 體感。
- **設定一次就好：** 可選擇登入 Windows 時自動啟動、縮在系統匣直接映射，並在隱藏手把後才啟動或重啟 Steam，
  從此不用手動重啟 Steam。
- 常用按鍵、方向鍵、搖桿、扳機；死區與輪詢頻率；設定檔匯入／匯出。
- 不連網、不收集遙測、沒有背景服務與自動更新。

**PS 圖示仍取決於遊戲：** 遊戲必須原生支援 DS4，或透過 Steam Input 支援。只內建 Xbox 圖示的遊戲還是會顯示 Xbox。

尚未支援：任意按鍵重排、巨集、觸控板滑動、鍵鼠、DualSense 輸出、依遊戲切換設定檔、疊圖。
NS2 Pro 僅支援 USB，C 鍵與背面 GL/GR 尚未映射。

## 安裝與快速開始

1. Windows 10／11 x64（Windows 11 為主要測試平台）。
2. 安裝 [.NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0)。
3. 從[官方下載頁](https://docs.nefarius.at/Downloads/)安裝 **ViGEmBus**（必要）與 **HidHide**（強烈建議，不需手動設定），
   依提示重新開機。ViGEmBus 已停止維護，請參考[上游公告](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)。
4. 從 [Releases](https://github.com/CYRLLC/TSCC_WASD/releases) 下載 ZIP，解壓後執行 `YnyrWASD.App.exe`。
   預覽版尚未數位簽章，SmartScreen 可能出現警告；請先比對 ZIP 與 `.sha256` 檔（[說明](SIGNING.md)）。
5. 輸入手把保持「自動偵測」，勾選「映射時自動隱藏實體手把」，按「啟動映射」。
6. 若提示 Steam 已在執行，選「是」重新啟動 Steam。
7. **最後才開遊戲。**

### 建議：開機自動啟動

在「程式設定」勾選「登入 Windows 時自動啟動並開始映射」與「自動開始映射後啟動 Steam」，
並到 Steam 設定關閉「電腦開機時執行 Steam」。登入後 YnyrWASD 會先隱藏手把、在系統匣開始映射，
然後才啟動 Steam，Steam 就永遠看不到實體手把。萬一 Steam 還是先開了，
「自動開始映射時，若 Steam 已先開啟就自動重啟它」會處理。

### 為什麼要注意開啟順序

HidHide 只能阻止程式「打開」手把，不能把已經打開的手把搶回來，所以：

- **遊戲已經開著：** 啟動映射後請重開遊戲。
- **Steam 已經開著：** Steam Input 會繼續把實體手把轉給 Steam 遊戲，圖示就會在 Xbox／PS 間交替。
  映射後重啟 Steam 即可（程式會詢問）。該遊戲的 Steam Input 請保持「預設／啟用」：
  像人中之龍 0 這類遊戲靠 Steam Input 決定 PS 圖示，停用後反而會顯示 Xbox 圖示。

停止映射後實體手把會恢復可見；關閉 YnyrWASD 不會關閉 Steam。若 Steam 是在手把被隱藏期間啟動的，
停止映射或關閉程式時會詢問是否再重啟一次，讓 Steam 重新認得實體手把。

## 運作方式

啟動映射時，程式會：

1. 先把目前的 HidHide 狀態記錄到 `%APPDATA%\YnyrWASD\hidhide-restore.json`；
2. 把自己加入 HidHide 允許清單；
3. 隱藏 NS2 Pro 與 Xbox 手把（藍牙／HID 以及有線 XUSB／GIP 類別），但不動 ViGEm 建立的虛擬手把（例如 DS4Windows 的輸出）；
4. 開啟隱藏功能。

之後每 5 秒會把新接上的手把也隱藏起來；停止時只還原自己改過的部分。程式若被強制結束，下次開啟會自動還原。
HidHide 為反向清單模式時不會更動。藍牙 Xbox 與 NS2 Pro 的隱藏已實機驗證；有線 Xbox 已實作並有單元測試，但尚未用實體有線手把驗證。

## 設定檔與程式設定

- 設定檔：`%APPDATA%\YnyrWASD\profiles.json`，前一版備份為 `.json.bak`。「新增／移除／匯入」後要按「儲存全部」才會寫入。
  損壞的檔案不會被覆寫，可透過「設定資料夾」修復或還原備份。
- 程式設定（開機啟動、系統匣、Steam、語言）：同資料夾的 `settings.json`，變更後立即儲存。
- 「登入 Windows 時自動啟動」會寫入目前使用者的 `HKCU\...\Run`，取消勾選即移除。
- 語言可選「跟隨 Windows／繁體中文／English」，重新開啟程式後套用。

NS2 Pro 若推到底卻像輕推、或放開時飄移，按「校準 NS2 Pro 搖桿」照步驟做一次即可（映射中也可以）。
NS2 Pro 的細節見 [NS2 Pro 設定說明](NS2-PRO.md)，常見問題見[疑難排解](TROUBLESHOOTING.md)。

## 授權

本專案採 [MIT 授權](../LICENSE)；改作自 SDL 的 NS2 Pro 協定部分（初始化、震動編碼、報告格式）保留 zlib 授權。
隨附與外部元件見[第三方聲明](../THIRD-PARTY-NOTICES.md)。PlayStation、DualShock、Xbox、
Nintendo Switch、Steam、reWASD 為各自所有者的商標，本專案與其無關聯。

建置、測試與貢獻方式見[主 README](../README.md)。真實手把與遊戲相容性見[驗證紀錄](VALIDATION.md)。
