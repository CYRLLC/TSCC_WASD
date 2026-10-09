# 程式碼簽章

## 一定要簽章嗎？

**不一定。** 沒有簽章的程式一樣可以下載、執行和發布。差別只在於使用者第一次執行時，
Windows SmartScreen 可能顯示「Windows 已保護您的電腦」。按「其他資訊 → 仍要執行」即可。
下載量累積一段時間後，警告通常會減少，但不保證消失。

目前 YnyrWASD 的預覽版**都沒有簽章**。執行前請先比對 ZIP 的 SHA-256 與 Release 附的 `.sha256` 檔：

```powershell
Get-FileHash .\YnyrWASD-0.4.0-win-x64.zip -Algorithm SHA256
```

## 方案比較

| 方案 | 費用 | 條件 |
| --- | --- | --- |
| 不簽章（目前） | 免費 | 無；使用者可能看到 SmartScreen 警告 |
| [SignPath Foundation](https://signpath.org/) | **免費**（限開源專案） | 需申請並通過審核，見下方 |
| [Azure Artifact Signing](https://azure.microsoft.com/products/artifact-signing)（原 Trusted Signing） | 每月付費（約 10 美元起，以官方計價為準） | 需通過 Azure 的身分驗證；可申請的國家／身分有限制 |
| 自簽憑證 | 免費 | 其他人的電腦不信任，**無法**消除警告，不建議 |

## SignPath Foundation（建議的免費方案）

SignPath Foundation 用**基金會名下的憑證**免費幫開源專案簽章，而且在 GitHub Actions 上從公開原始碼建置，
簽章能證明程式確實由這份原始碼產生。只有專案擁有者能申請，審核由對方決定，可能被拒絕。

### 主要條件（依 [SignPath 條款](https://signpath.org/terms)整理）

- 所有元件使用 OSI 認可的開源授權，沒有商業雙授權、沒有閉源程式碼（本專案為 MIT ✓）。
- 不能是惡意或不受歡迎的軟體，也不能是掃描漏洞的資安工具（本專案不是 ✓）。
- 專案持續維護中，而且要簽章的版本**已經公開發布**。
- 下載頁要說明程式功能；不能有未揭露的資料傳輸（本專案不連網 ✓）；
  會改動系統的行為要清楚提示。本專案會在映射期間更動 HidHide 設定，並可寫入開機啟動項，README 已說明。
- 團隊所有成員在 GitHub 與 SignPath 都要啟用**多重要素驗證（MFA）**。
- 要定義作者、審查者、核准者等角色，**每次發布都要人工核准**。
- 專案網站要有一頁「程式碼簽章政策」，寫明規定的聲明、團隊角色與隱私說明。

### 申請步驟

1. 確認 GitHub 帳號已啟用 MFA，並至少公開發布一個版本（v0.4.0）。
2. 到 https://signpath.org/apply 申請。說明本程式會呼叫 ViGEmBus 與 HidHide，但本身不安裝驅動。
3. 通過後，在 SignPath 建立：
   - 連到 `CYRLLC/TSCC_WASD` 的 GitHub 受信任建置系統；
   - 一個簽 ZIP 內 `YnyrWASD.App.exe`、`YnyrWASD.App.dll`、`YnyrWASD.Core.dll` 的成品設定；
   - 名為 `release-signing` 的簽章政策。
4. 在 GitHub 專案的 Actions secrets 加入 `SIGNPATH_API_TOKEN` 與 `SIGNPATH_ORGANIZATION_ID`。
5. 在 `.github/workflows/release.yml` 的「Build, test and package」與「Create draft preview」之間，
   上傳未簽章的 ZIP，加入
   [`SignPath/github-action-submit-signing-request`](https://github.com/SignPath/github-action-submit-signing-request)
   步驟（`wait-for-completion: true`），再用簽好的 ZIP 取代原檔並重新計算 `.sha256`。
6. 在 README 加上「程式碼簽章政策」說明，並更新本文件。

### Azure Artifact Signing

若不想等 SignPath 審核，可以改用 Azure Artifact Signing，以
[`Azure/artifact-signing-action`](https://github.com/Azure/artifact-signing-action) 接到 GitHub Actions。
需要 Azure 訂閱與身分驗證，按月計費。

**切勿**把憑證、私鑰或簽章權杖提交到專案裡。
