# YnyrWASD 使用說明

將 NS2 Pro USB 或 Xbox／XInput 手把輸入轉成虛擬 DualShock 4 的 Windows 工具。
目前為 **0.2.0 預覽版**，新增 NS2 Pro USB 輸入，詳見 [NS2 Pro 設定說明](NS2-PRO.md)。
原創程式採 MIT 授權，SDL 協定改作部分保留 zlib 授權；介面使用繁體中文。

## 安裝與操作

1. 使用 Windows 10／11 x64 與 XInput 相容手把。
2. 安裝最新修補版 [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0)。
3. 從 [官方下載頁](https://docs.nefarius.at/Downloads/) 安裝 ViGEmBus，依提示重新開機。
   此驅動已停止維護，請先閱讀[上游公告](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)。
4. 解壓發布包，執行 `YnyrWASD.App.exe`，選擇設定檔並按「啟動映射」。
5. 「停止」後可編輯名稱、說明、死區及輪詢頻率；按「儲存全部」才會寫入磁碟。

可選第一個可用的 XInput 插槽（0–3）或單支 NS2 Pro USB，只輸出一個虛擬 PS4。
支援常用按鍵、方向鍵、搖桿、扳機；斷線會將輸出歸零，停止或關閉時釋放虛擬裝置。
輪詢頻率是目標值，實際頻率受 Windows 排程影響。

「新增／移除／匯入」先修改記憶體中的設定；儲存後才會保留。
「重新載入」與關閉視窗會捨棄未儲存修改。匯出會保存目前完整清單。
設定檔位置：`%APPDATA%\YnyrWASD\profiles.json`；前一版備份為 `.json.bak`。
損壞的檔案不會被覆寫，可透過「設定資料夾」修復或還原備份，再重新載入。

## PS 圖示與雙重輸入

**遊戲必須支援 DS4 與 PS 按鍵圖示，本工具無法保證所有遊戲都顯示 PS 圖示。**
Steam Input 或其他映射工具也可能影響辨識結果。

遊戲同時看到實體和虛擬手把時，可選用 HidHide。依照[官方設定指南](https://docs.nefarius.at/projects/HidHide/Simple-Setup-Guide/)，
將 `YnyrWASD.App.exe` 加入允許清單，選取實體手把後啟用隱藏。不要隱藏虛擬 DS4。
更換程式資料夾後需更新允許路徑；要還原，請在 HidHide 停用裝置隱藏。
本工具的驅動按鈕只開啟官方下載頁，不會自動安裝或更動 HidHide。

## 首版範圍

已提供設定檔管理、死區、輪詢頻率、手動啟停和基本 XInput → DS4 映射。
尚未提供自訂按鍵重排、巨集、震動回傳、鍵鼠、DirectInput、觸控板、陀螺儀、
DualSense 輸出、遊戲自動偵測／切換及疊圖。
這是範圍明確的開源預覽版，並非完整 reWASD 替代品。

建置指令、測試與開源貢獻方式見[主 README](../README.md)。
真實手把、熱插拔與遊戲相容性仍需依[驗證清單](VALIDATION.md)測試。
