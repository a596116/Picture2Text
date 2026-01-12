# 配置檔案說明

## appsettings.json vs appsettings.Development.json

### 載入順序

ASP.NET Core 會按照以下順序載入配置檔案（後面的會覆蓋前面的）：

1. **appsettings.json** - 基礎配置（所有環境都會載入）
2. **appsettings.{Environment}.json** - 環境特定配置（根據 `ASPNETCORE_ENVIRONMENT` 變數載入）
3. **環境變數**
4. **命令列參數**

### 差異說明

#### appsettings.json（基礎配置）
- **用途**：所有環境的共用配置
- **載入時機**：無論什麼環境都會載入
- **內容**：
  - Logging 設定
  - AllowedHosts
  - ConnectionStrings（預設值）
  - Jwt 設定（預設值）

#### appsettings.Development.json（開發環境配置）
- **用途**：僅在開發環境使用的配置
- **載入時機**：當 `ASPNETCORE_ENVIRONMENT=Development` 時才會載入
- **內容**：
  - Logging 設定（覆蓋基礎配置）
  - ConnectionStrings（覆蓋基礎配置，通常使用本地開發資料庫）
  - **不包含** Jwt 設定（使用基礎配置的值）
  - **不包含** AllowedHosts（使用基礎配置的值）

### 實際運作方式

當應用程式在 **Development** 環境下運行時：

```json
// 最終載入的配置 = appsettings.json + appsettings.Development.json（覆蓋相同鍵值）
{
  "Logging": {
    // 來自 appsettings.Development.json（覆蓋）
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore": "Information"
  },
  "AllowedHosts": "*",  // 來自 appsettings.json（未被覆蓋）
  "ConnectionStrings": {
    // 來自 appsettings.Development.json（覆蓋）
    "DefaultConnection": "Server=localhost;Database=testdb;..."
  },
  "Jwt": {
    // 來自 appsettings.json（未被覆蓋）
    "SecretKey": "your-secret-key-change-this-to-a-long-random-string-at-least-32-characters",
    "Issuer": "Picture2Text.Api",
    "Audience": "Picture2Text.Client",
    "ExpirationMinutes": "30"
  }
}
```

### 最佳實踐

#### 建議的配置方式

**appsettings.json**（基礎配置）：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=production-server;Database=prod;..."
  },
  "Jwt": {
    "SecretKey": "production-secret-key-very-long-and-secure",
    "Issuer": "Picture2Text.Api",
    "Audience": "Picture2Text.Client",
    "ExpirationMinutes": "30"
  }
}
```

**appsettings.Development.json**（開發環境覆蓋）：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",  // 開發環境需要更詳細的日誌
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"  // 開發時查看 EF 日誌
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=testdb;..."  // 本地開發資料庫
  }
  // Jwt 設定使用基礎配置，不需要重複
}
```

**appsettings.Production.json**（生產環境，可選）：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Error",  // 生產環境只記錄錯誤
      "Microsoft.AspNetCore": "Error"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=production;..."  // 生產資料庫
  }
}
```

### 環境變數設定

環境變數可以通過以下方式設定：

**在 launchSettings.json 中**（開發環境）：
```json
{
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

**在系統環境變數中**：
```bash
# Linux/macOS
export ASPNETCORE_ENVIRONMENT=Development

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT="Development"

# Windows CMD
set ASPNETCORE_ENVIRONMENT=Development
```

**在執行時指定**：
```bash
dotnet run --environment Development
```

### 檢查當前環境

在程式碼中檢查當前環境：
```csharp
if (app.Environment.IsDevelopment())
{
    // 開發環境特定邏輯
}
```

### 安全注意事項

⚠️ **重要**：
- 不要在配置檔案中存放敏感資訊（如密碼、API 金鑰）
- 使用 **User Secrets**（開發環境）或 **環境變數**（生產環境）存放敏感資料
- 不要將包含敏感資訊的配置檔案提交到版本控制系統

**使用 User Secrets（開發環境）**：
```bash
dotnet user-secrets set "Jwt:SecretKey" "your-secret-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```
