# YnyrWASD 使用說明

免費開源的 Windows 工具：讓遊戲把你的 Xbox 或 Nintendo Switch 2 Pro 手把當成
PlayStation DualShock 4，顯示 PS 按鍵圖示。對應付費軟體 reWASD 最常用的「偽裝成 DS4」功能，
改用免費元件 ViGEmBus（虛擬 DS4）與 HidHide（隱藏實體手把）完成。
目前為 **0.3.0 預覽版**；NS2 Pro 詳見 [NS2 Pro 設定說明](NS2-PRO.md)。
原創程式採 MIT 授權，SDL 協定改作部分保留 zlib 授權；介面使用繁體中文。

## 安裝與操作

1. 使用 Windows 10／11 x64，以及 Xbox／XInput 手把或 USB 連接的 NS2 Pro。
2. 安裝最新修補版 [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0)。
3. 從 [官方下載頁](https://docs.nefarius.at/Downloads/) 安裝 ViGEmBus（必要）與 HidHide（強烈建議，免手動設定），依提示重新開機。
   此驅動已停止維護，請先閱讀[上游公告](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)。
4. 解壓發布包，執行 `YnyrWASD.App.exe`，輸入手把保持「自動偵測」並勾選「映射時自動隱藏實體手把」，按「啟動映射」。
   若提示 Steam 已在執行，選「是」重新啟動 Steam；**最後才開遊戲**。
5. 「停止」後可編輯名稱、說明、死區及輪詢頻率；按「儲存全部」才會寫入磁碟。

輸入預設為**自動偵測**：同時監看 NS2 Pro USB 與第一個可用的 XInput 插槽（0–3），
按哪一支就用哪一支，閒置的手把不會搶走控制權；也可指定只用其中一種。
虛擬 PS4 在整段映射期間持續連線：手把斷線時輸出歸零，驅動出錯會自動重連。
支援常用按鍵、方向鍵、搖桿、扳機；停止或關閉時釋放虛擬裝置。
輪詢頻率是目標值，實際頻率受 Windows 排程影響。

「新增／移除／匯入」先修改記憶體中的設定；儲存後才會保留。
「重新載入」與關閉視窗會捨棄未儲存修改。匯出會保存目前完整清單。
設定檔位置：`%APPDATA%\YnyrWASD\profiles.json`；前一版備份為 `.json.bak`。
損壞的檔案不會被覆寫，可透過「設定資料夾」修復或還原備份，再重新載入。

## PS 圖示與雙重輸入

**遊戲必須支援 DS4 與 PS 按鍵圖示，本工具無法保證所有遊戲都顯示 PS 圖示。**
Steam Input 或其他映射工具也可能影響辨識結果。

遊戲同時看到實體與虛擬手把時，每次按鍵會收到兩次，圖示也會在 Xbox／PS 之間跳動。
安裝 [HidHide](https://docs.nefarius.at/Downloads/) 即可，不需手動設定：勾選
「映射時自動隱藏實體手把」（預設開啟）後，啟動映射會自動把本程式加入允許清單、
隱藏 NS2 Pro 與 Xbox 手把並開啟隱藏；之後才接上的手把約 5 秒內也會被隱藏。
停止映射會完整還原原本的 HidHide 設定；程式若被強制結束，下次開啟時自動還原。

請**先啟動映射再開遊戲**，已經開啟實體手把的遊戲不受影響。
**Steam 是最常見的情況**：Steam 若比映射早開，Steam Input 會繼續轉送實體手把，
Steam 遊戲的圖示就會在 Xbox／PS 間交替。程式偵測到時會詢問是否重新啟動 Steam
（也可按「重新啟動 Steam」）。像人中之龍 0 這類靠 Steam Input 決定 PS 圖示的遊戲，
請讓該遊戲的 Steam Input 保持「預設／啟用」。HidHide 反向清單模式不會被更動。
使用 XUSB 驅動的有線 Xbox 手把不是 HID 裝置，目前不會被隱藏。
本工具不會安裝驅動，驅動按鈕只開啟官方下載頁。

## 目前範圍

已提供自動偵測輸入、自動隱藏實體手把、Steam 偵測與重啟、設定檔管理、死區、輪詢頻率及 DS4 映射。
尚未提供自訂按鍵重排、巨集、震動回傳、鍵鼠、DirectInput、觸控板、陀螺儀、
DualSense 輸出、依遊戲自動切換設定檔及疊圖。
這是範圍明確的開源預覽版，並非完整 reWASD 替代品。

## 授權

本專案採 [MIT 授權](../LICENSE)；改作自 SDL 的 NS2 Pro 協定部分保留 zlib 授權。
隨附與外部元件見[第三方聲明](../THIRD-PARTY-NOTICES.md)。PlayStation、DualShock、Xbox、
Nintendo Switch、Steam、reWASD 為各自所有者的商標，本專案與其無關聯。

建置指令、測試與開源貢獻方式見[主 README](../README.md)。
真實手把、熱插拔與遊戲相容性仍需依[驗證清單](VALIDATION.md)測試。
