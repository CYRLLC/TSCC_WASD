# Nintendo Switch 2 Pro (USB)

Supported hardware identity: Nintendo VID `057E`, PID `2069`. This mode targets
the Switch 2 Pro, not the original Switch Pro, Joy-Con or Bluetooth variants.
Only one NS2 Pro can be connected at a time. Virtual DS4 devices are excluded by
the VID/PID filter, so the mapper does not feed its own output back as input.

## 使用方式

1. 接上 NS2 Pro 的 USB 線，開啟 **0.4.0** 或更新版本。
2. 新增或選擇設定檔，「輸入手把」維持 **自動偵測**（或指定只用 NS2 Pro）。
3. 按「儲存全部」，再按「啟動映射」。
4. 底部應顯示「映射運作中：NS2 Pro … → DualShock 4」。
   按鍵及搖桿數字會即時更新。

### Steam 開著：HID 共用模式（使用者已確認虛擬 PS4 收到輸入）

Steam 若占用 USB 控制介面，程式會讀取 Steam 已初始化的 HID 報告，
不會關閉 Steam 或更換驅動。此模式使用 12-bit 預設中心／範圍；
搖桿的中心偏差可由死區吸收，但滿行程可能與原廠校準不同。
若一直等待輸入，確認 Steam 已辨識並初始化 NS2 Pro，並檢查 HidHide。

### Steam 完整退出：原生 USB 初始化（已驗證目前連線；冷插拔待測）

USB 控制介面可用時，程式會讀取原廠與使用者搖桿校準，設定輸入報告，
之後釋放控制介面並持續讀取 HID。流程只讀取 flash 校準資料，不會改寫韌體、
配對或校準；不需要安裝額外驅動。2026-09-11 已在目前硬體驗證 Steam
退出後的初始化、校準讀取及 30 秒隱藏實體手把並輸出虛擬 DS4；手把未在
測試間拔插，因此完全斷電後的初始化仍待驗證。詳見 [驗證紀錄](VALIDATION.md)。

初始化後可再開 Steam；如果手把重插時介面被占用，會回到共用模式。
遇到裝置占用、沒有回報、校準或存取錯誤，原因會顯示於狀態列。

## 按鍵配置與限制

- 依實體位置：Nintendo **B → Cross、A → Circle、Y → Square、X → Triangle**。
- `− / +` → Share / Options；L/R → L1/R1；搖桿按下 → L3/R3。
- ZL/ZR 是數位開關，輸出為 0 或 255，不會變成真正的類比扳機。
- **Home → PS 鍵、Capture（截圖）→ 觸控板按下。**
- **陀螺儀／加速度計 → DS4 體感**（0.4.0 起；已實機確認有資料、比例正確，遊戲內方向待驗證）。
- **震動：** 遊戲送給虛擬 DS4 的震動會轉成 NS2 Pro 的 HD 震動（可在設定檔關閉）。
- C 鍵與背面 GL/GR 尚未映射。
- 即時診斷中的 A/B/X/Y 是中介 XInput 位置名稱；以本頁 PS 對應為準。
- 超過 250ms 沒有有效資料，輸出歸零；讀取逾時或拔除後會嘗試重新連線。
- 映射時會自動用 HidHide 隱藏 NS2 Pro；Steam 若比映射早開，程式會提示重新啟動 Steam。

## Development smoke check

```powershell
dotnet build YnyrWASD.sln -c Release
pwsh -File scripts/smoke-ns2.ps1 -Seconds 15
# Optional: also submit the sampled state to a temporary virtual DS4
pwsh -File scripts/smoke-ns2.ps1 -AppDirectory YnyrWASD.App/bin/Release/net8.0-windows -Seconds 15 -MapToDs4
```

The script reports observed buttons and left-stick X range, then disposes all handles.
Unit tests cover the USB report parser and calibration math without hardware.
See [validation record](VALIDATION.md) for what was actually exercised.
