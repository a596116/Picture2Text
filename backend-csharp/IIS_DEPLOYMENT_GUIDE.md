# 🚀 IIS 部署指南 - Picture2Text API

本指南將詳細說明如何將 Picture2Text API 部署到 Windows Server 上的 IIS（Internet Information Services）。

## 📋 目錄

1. [前置需求](#前置需求)
2. [安裝必要軟體](#安裝必要軟體)
3. [發布應用程式](#發布應用程式)
4. [配置 IIS](#配置-iis)
5. [設定應用程式設定](#設定應用程式設定)
6. [資料庫設定](#資料庫設定)
7. [測試部署](#測試部署)
8. [常見問題排除](#常見問題排除)
9. [安全性建議](#安全性建議)

---

## 前置需求

### 系統需求

- **作業系統**：Windows Server 2016 或更新版本，或 Windows 10/11（開發/測試環境）
- **IIS 版本**：IIS 10.0 或更新版本
- **.NET 版本**：.NET 9.0 Runtime 或 Hosting Bundle
- **資料庫**：SQL Server 2019 或更新版本（或 SQL Server Express）

### 權限需求

- 管理員權限（用於安裝軟體和配置 IIS）
- SQL Server 資料庫管理權限（用於建立資料庫和執行遷移）

---

## 安裝必要軟體

### 步驟 1：啟用 IIS

1. 開啟「伺服器管理員」（Server Manager）
2. 點擊「新增角色及功能」（Add Roles and Features）
3. 選擇「角色型或功能型安裝」（Role-based or feature-based installation）
4. 選擇目標伺服器
5. 在「伺服器角色」（Server Roles）中，勾選：
   - ✅ **Web 伺服器 (IIS)**
   - ✅ **管理工具** → **IIS 管理主控台**
6. 在「功能」（Features）中，勾選：
   - ✅ **.NET Framework 4.8**（如果尚未安裝）
7. 在「Web 伺服器角色 (IIS)」→「角色服務」中，確保勾選：
   - ✅ **HTTP 重新導向**
   - ✅ **靜態內容**
   - ✅ **預設文件**
   - ✅ **目錄瀏覽**
   - ✅ **HTTP 錯誤**
   - ✅ **ASP.NET 4.8**（如果使用）
   - ✅ **ISAPI 擴充程式**
   - ✅ **ISAPI 篩選器**
   - ✅ **要求篩選**
   - ✅ **Windows 驗證**（如果需要）
   - ✅ **基本驗證**（如果需要）
8. 完成安裝並重新啟動（如需要）

### 步驟 2：安裝 .NET 9.0 Hosting Bundle

1. 下載 **.NET 9.0 Hosting Bundle**：
   - 前往：https://dotnet.microsoft.com/download/dotnet/9.0
   - 下載 **ASP.NET Core Runtime 9.0.x - Windows Hosting Bundle Installer**
   - 選擇對應的架構（x64 或 ARM64）

2. 執行安裝程式：
   ```
   dotnet-hosting-9.0.x-win-x64.exe
   ```

3. 安裝完成後，**重新啟動 IIS**：
   ```powershell
   # 以管理員身份執行 PowerShell
   iisreset
   ```

   > **注意**：Hosting Bundle 包含 .NET Runtime 和 ASP.NET Core Module，這是 IIS 運行 ASP.NET Core 應用程式所必需的。

### 步驟 3：安裝 SQL Server（如果尚未安裝）

如果伺服器上還沒有 SQL Server，可以：

1. **安裝 SQL Server Express**（免費版本，適合小型應用）：
   - 下載：https://www.microsoft.com/zh-tw/sql-server/sql-server-downloads
   - 選擇「Express」版本
   - 安裝時選擇「基本」安裝

2. **或使用現有的 SQL Server 實例**

3. **啟用 SQL Server 驗證**（混合模式）：
   - 開啟 SQL Server Management Studio (SSMS)
   - 連接到伺服器
   - 右鍵點擊伺服器 → 屬性 → 安全性
   - 選擇「SQL Server 及 Windows 驗證模式」
   - 重新啟動 SQL Server 服務

---

## 發布應用程式

### 步驟 1：準備發布目錄

在伺服器上建立一個目錄來存放發布的應用程式：

```powershell
# 以管理員身份執行 PowerShell
New-Item -ItemType Directory -Path "C:\inetpub\Picture2TextApi" -Force
```

### 步驟 2：發布應用程式

#### 方法 A：在開發機器上發布（推薦）

1. 在開發機器上，開啟命令提示字元或 PowerShell
2. 導航到專案目錄：
   ```bash
   cd C:\Path\To\Picture2Text\backend-csharp
   ```

3. 執行發布命令：
   ```bash
   dotnet publish -c Release -o C:\Publish\Picture2TextApi
   ```

   或指定特定架構：
   ```bash
   dotnet publish -c Release -r win-x64 -o C:\Publish\Picture2TextApi
   ```

4. 將發布的檔案複製到伺服器：
   - 使用檔案共享、FTP、或遠端桌面複製到 `C:\inetpub\Picture2TextApi`

#### 方法 B：直接在伺服器上發布

1. 將專案原始碼複製到伺服器
2. 在伺服器上安裝 **.NET 9.0 SDK**（不只是 Runtime）
3. 執行發布命令：
   ```powershell
   cd C:\Path\To\backend-csharp
   dotnet publish -c Release -o C:\inetpub\Picture2TextApi
   ```

### 步驟 3：檢查發布結果

發布目錄應包含以下檔案和資料夾：

```
C:\inetpub\Picture2TextApi\
├── appsettings.json
├── appsettings.Development.json
├── Picture2Text.Api.dll
├── Picture2Text.Api.exe
├── web.config
├── Controllers/
├── Services/
├── Models/
└── ... (其他依賴檔案)
```

> **重要**：確保 `web.config` 檔案存在。如果不存在，ASP.NET Core 模組可能無法正確啟動應用程式。

---

## 配置 IIS

### 步驟 1：建立應用程式池

1. 開啟 **IIS 管理員**（IIS Manager）
2. 在左側連線面板中，展開伺服器節點
3. 右鍵點擊「應用程式集區」（Application Pools）→「新增應用程式集區」（Add Application Pool）

4. 設定如下：
   - **名稱**：`Picture2TextApi`
   - **.NET CLR 版本**：**無受控碼**（No Managed Code）
   - **受控管線模式**：**整合**（Integrated）

5. 點擊「確定」

6. 右鍵點擊新建立的應用程式池 →「進階設定」（Advanced Settings）

7. 設定以下項目：
   - **啟用 32 位元應用程式**：`False`
   - **識別**：選擇「ApplicationPoolIdentity」或自訂帳戶
   - **閒置逾時**：`00:00:00`（設為 0 表示永不逾時，或設定適當值如 `00:20:00`）
   - **啟動模式**：`AlwaysRunning`（可選，保持應用程式常駐記憶體）

8. 點擊「確定」

### 步驟 2：建立網站

1. 在 IIS 管理員中，右鍵點擊「網站」（Sites）→「新增網站」（Add Website）

2. 設定如下：
   - **網站名稱**：`Picture2TextApi`
   - **應用程式集區**：選擇剛才建立的 `Picture2TextApi`
   - **實體路徑**：`C:\inetpub\Picture2TextApi`
   - **繫結類型**：`http` 或 `https`
   - **IP 位址**：`全部未指派` 或特定 IP
   - **連接埠**：`80`（HTTP）或 `443`（HTTPS）
   - **主機名稱**：（可選）例如 `api.yourcompany.com`

3. 點擊「確定」

### 步驟 3：設定應用程式設定

1. 在 IIS 管理員中，選取剛建立的網站 `Picture2TextApi`
2. 雙擊「設定編輯器」（Configuration Editor）
3. 導航到 `system.webServer/aspNetCore`
4. 確認以下設定：
   - **processPath**：`dotnet`
   - **arguments**：`.\Picture2Text.Api.dll`
   - **stdoutLogEnabled**：`True`（用於除錯）
   - **stdoutLogFile**：`.\logs\stdout`（確保 logs 資料夾存在）

5. 或直接編輯 `web.config` 檔案：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\Picture2Text.Api.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

### 步驟 4：設定資料夾權限

1. 右鍵點擊 `C:\inetpub\Picture2TextApi` 資料夾 →「內容」（Properties）→「安全性」（Security）

2. 點擊「編輯」（Edit）→「新增」（Add）

3. 輸入應用程式池的識別：
   - 如果使用 `ApplicationPoolIdentity`，輸入：`IIS AppPool\Picture2TextApi`
   - 或使用自訂帳戶（例如：`DOMAIN\ServiceAccount`）

4. 授予以下權限：
   - ✅ **讀取和執行**
   - ✅ **列出資料夾內容**
   - ✅ **讀取**
   - ✅ **寫入**（用於日誌檔案）

5. 點擊「確定」

6. 如果使用自訂帳戶，確保該帳戶也有權限存取 SQL Server

### 步驟 5：建立日誌資料夾

```powershell
# 以管理員身份執行 PowerShell
New-Item -ItemType Directory -Path "C:\inetpub\Picture2TextApi\logs" -Force

# 設定權限
$acl = Get-Acl "C:\inetpub\Picture2TextApi\logs"
$permission = "IIS AppPool\Picture2TextApi","FullControl","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl "C:\inetpub\Picture2TextApi\logs" $acl
```

---

## 設定應用程式設定

### 步驟 1：編輯 appsettings.json

編輯 `C:\inetpub\Picture2TextApi\appsettings.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Picture2TextDb;User Id=sa;Password=YourSecurePassword;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Jwt": {
    "SecretKey": "your-production-secret-key-minimum-64-characters-long-and-random",
    "Issuer": "Picture2Text.Api",
    "Audiences": [
      "Picture2Text.Web",
      "Picture2Text.Mobile"
    ],
    "ExpirationMinutes": "30",
    "RefreshTokenExpirationDays": "7"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://www.yourdomain.com",
      "https://app.yourdomain.com"
    ]
  }
}
```

> **重要安全性提醒**：
> - 生產環境必須使用強密碼的 JWT SecretKey（至少 64 個隨機字元）
> - 使用強密碼的資料庫密碼
> - 限制 CORS 允許的來源，不要使用 `*`

### 步驟 2：使用環境變數（推薦）

為了更好的安全性，可以將敏感資訊設定為環境變數：

1. 在 IIS 管理員中，選取網站 →「設定編輯器」→ `system.webServer/aspNetCore/environmentVariables`

2. 新增以下環境變數：
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = `Server=localhost;Database=Picture2TextDb;...`
   - `Jwt__SecretKey` = `your-secret-key`
   - `Jwt__Issuer` = `Picture2Text.Api`

   或在 `web.config` 中設定：

```xml
<aspNetCore>
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=localhost;Database=Picture2TextDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" />
    <environmentVariable name="Jwt__SecretKey" value="your-production-secret-key" />
  </environmentVariables>
</aspNetCore>
```

> **注意**：在環境變數中使用雙底線 `__` 來表示 JSON 的階層結構（例如：`Jwt__SecretKey` 對應 `Jwt.SecretKey`）

---

## 資料庫設定

### 步驟 1：建立資料庫

1. 開啟 **SQL Server Management Studio (SSMS)**
2. 連接到 SQL Server 實例
3. 右鍵點擊「資料庫」→「新增資料庫」
4. 資料庫名稱：`Picture2TextDb`
5. 點擊「確定」

### 步驟 2：執行資料庫遷移

#### 方法 A：使用 EF Core Migrations（推薦）

1. 在開發機器或伺服器上，安裝 EF Core Tools：
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. 導航到專案目錄：
   ```bash
   cd C:\Path\To\backend-csharp
   ```

3. 執行遷移：
   ```bash
   dotnet ef database update
   ```

   或指定連接字串：
   ```bash
   dotnet ef database update --connection "Server=localhost;Database=Picture2TextDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
   ```

#### 方法 B：在應用程式啟動時自動建立（僅限開發/測試）

如果 `Program.cs` 中有啟用 `EnsureCreated()`，應用程式會在首次啟動時自動建立資料庫結構。

> **注意**：生產環境建議使用 Migrations，而不是 `EnsureCreated()`

### 步驟 3：建立初始使用者（可選）

如果需要建立初始使用者，可以使用 SQL 指令：

```sql
USE Picture2TextDb;
GO

-- 插入測試使用者（密碼已使用 BCrypt 加密，範例密碼：admin123）
INSERT INTO Users (Username, PasswordHash, Email, CreatedAt, IsActive)
VALUES ('admin', '$2a$11$YourBcryptHashHere', 'admin@example.com', GETDATE(), 1);
GO
```

> **注意**：實際的 BCrypt 雜湊值需要從應用程式產生。建議使用 API 的註冊端點或管理工具來建立使用者。

---

## 測試部署

### 步驟 1：啟動網站

1. 在 IIS 管理員中，選取 `Picture2TextApi` 網站
2. 在右側「動作」面板中，點擊「啟動」（Start）

### 步驟 2：檢查應用程式池狀態

1. 選取「應用程式集區」
2. 確認 `Picture2TextApi` 的狀態為「執行中」（Running）

### 步驟 3：測試 API

1. **測試基本連線**：
   ```powershell
   # 在 PowerShell 中執行
   Invoke-WebRequest -Uri "http://localhost/api/auth/validate" -Method POST -ContentType "application/json" -Body '{"token":"test"}'
   ```

2. **測試 Swagger UI**：
   開啟瀏覽器，訪問：
   ```
   http://localhost/swagger
   ```
   或
   ```
   http://your-server-ip/swagger
   ```

3. **測試登入端點**：
   ```bash
   curl -X POST http://localhost/api/auth/login \
     -H "Content-Type: application/json" \
     -d "{\"userId\":1,\"password\":\"yourpassword\"}"
   ```

### 步驟 4：檢查日誌

1. 檢查 IIS 日誌：
   - 位置：`C:\inetpub\logs\LogFiles\W3SVC{site-id}\`

2. 檢查應用程式日誌：
   - 位置：`C:\inetpub\Picture2TextApi\logs\stdout_*.log`

3. 檢查 Windows 事件檢視器：
   - 開啟「事件檢視器」→「Windows 記錄」→「應用程式」
   - 查看 ASP.NET Core 相關錯誤

---

## 常見問題排除

### 問題 1：HTTP 500.30 - ANCM In-Process Start Failure

**原因**：應用程式無法啟動

**解決方法**：
1. 檢查 `web.config` 中的 `processPath` 和 `arguments` 是否正確
2. 確認 .NET 9.0 Hosting Bundle 已正確安裝
3. 檢查應用程式日誌：`C:\inetpub\Picture2TextApi\logs\stdout_*.log`
4. 確認資料夾權限設定正確

### 問題 2：HTTP 500.0 - ANCM Out-Of-Process Start Failure

**原因**：無法啟動外部處理程序

**解決方法**：
1. 確認 `dotnet.exe` 在系統 PATH 中
2. 檢查應用程式池的識別是否有足夠權限
3. 確認 .NET Runtime 已安裝

### 問題 3：無法連接到資料庫

**原因**：資料庫連接字串錯誤或權限不足

**解決方法**：
1. 檢查 `appsettings.json` 中的連接字串
2. 確認 SQL Server 服務正在運行
3. 測試資料庫連接：
   ```powershell
   # 使用 sqlcmd 測試連接
   sqlcmd -S localhost -U sa -P YourPassword -Q "SELECT @@VERSION"
   ```
4. 確認應用程式池識別有資料庫存取權限

### 問題 4：CORS 錯誤

**原因**：前端應用程式無法存取 API

**解決方法**：
1. 檢查 `appsettings.json` 中的 `Cors:AllowedOrigins`
2. 確認前端應用程式的網域已加入允許清單
3. 檢查 IIS 的 CORS 模組設定（如果使用）

### 問題 5：Swagger UI 無法顯示

**原因**：路由或靜態檔案服務問題

**解決方法**：
1. 確認 `app.UseSwagger()` 和 `app.UseSwaggerUI()` 在 `Program.cs` 中已啟用
2. 檢查 IIS 的「靜態內容」功能是否已啟用
3. 確認應用程式可以正常啟動（檢查日誌）

### 問題 6：JWT Token 驗證失敗

**原因**：JWT 設定不一致

**解決方法**：
1. 確認 `Jwt:SecretKey` 在所有環境中一致
2. 檢查 `Jwt:Issuer` 和 `Jwt:Audiences` 設定
3. 確認時鐘同步（JWT 會驗證時間）

### 問題 7：應用程式池自動停止

**原因**：閒置逾時或錯誤導致

**解決方法**：
1. 在應用程式池的「進階設定」中，將「閒置逾時」設為 `00:00:00`（永不逾時）
2. 啟用「啟動模式」為 `AlwaysRunning`
3. 檢查應用程式日誌找出錯誤原因

---

## 安全性建議

### 1. 啟用 HTTPS

1. **取得 SSL 憑證**：
   - 使用 Let's Encrypt（免費）
   - 或購買商業 SSL 憑證
   - 或使用內部 CA 憑證

2. **在 IIS 中設定 HTTPS 繫結**：
   - 選取網站 →「繫結」（Bindings）
   - 點擊「新增」（Add）
   - 類型：`https`
   - 連接埠：`443`
   - SSL 憑證：選擇您的憑證
   - 點擊「確定」

3. **強制 HTTPS 重新導向**：
   在 `Program.cs` 中，確保生產環境啟用 HTTPS 重新導向：
   ```csharp
   if (!app.Environment.IsDevelopment())
   {
       app.UseHttpsRedirection();
   }
   ```

### 2. 保護 Swagger UI

生產環境建議禁用或保護 Swagger UI：

```csharp
// 在 Program.cs 中
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
// 生產環境不啟用 Swagger
```

或使用 IP 限制或基本驗證保護 Swagger 路徑。

### 3. 設定防火牆規則

只開放必要的連接埠：

```powershell
# 允許 HTTP (80)
New-NetFirewallRule -DisplayName "HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow

# 允許 HTTPS (443)
New-NetFirewallRule -DisplayName "HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

### 4. 使用強密碼和密鑰

- JWT SecretKey：至少 64 個隨機字元
- 資料庫密碼：符合複雜度要求
- 定期輪換密鑰和密碼

### 5. 限制 CORS

只允許信任的網域：

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://app.yourdomain.com"
    ]
  }
}
```

### 6. 啟用請求限制

在 IIS 中設定請求限制，防止濫用：

1. 選取網站 →「要求篩選」（Request Filtering）
2. 設定最大請求長度和連線數限制

### 7. 定期備份

- 定期備份資料庫
- 備份應用程式設定檔
- 建立災難復原計畫

### 8. 監控和日誌

- 啟用 IIS 日誌記錄
- 設定應用程式日誌輪替
- 使用監控工具（如 Application Insights）追蹤效能和錯誤

---

## 進階設定

### 使用反向代理（Nginx/IIS ARR）

如果需要使用 IIS 作為反向代理，可以安裝 **Application Request Routing (ARR)** 模組。

### 負載平衡

使用多個 IIS 伺服器時，可以設定負載平衡器（如 Azure Load Balancer 或硬體負載平衡器）。

### 自動部署

可以使用以下工具自動化部署：
- **Azure DevOps Pipelines**
- **GitHub Actions**
- **PowerShell 指令碼**
- **Octopus Deploy**

---

## 總結

完成以上步驟後，您的 Picture2Text API 應該已經成功部署到 IIS。記住：

1. ✅ 定期更新 .NET Runtime 和應用程式
2. ✅ 監控應用程式效能和錯誤
3. ✅ 定期備份資料庫
4. ✅ 保持安全性最佳實踐
5. ✅ 測試災難復原程序

如有問題，請參考：
- [官方 ASP.NET Core IIS 部署文檔](https://docs.microsoft.com/aspnet/core/host-and-deploy/iis/)
- [應用程式日誌](#測試部署)
- [Windows 事件檢視器](#測試部署)

---

**最後更新**：2025-01-14
