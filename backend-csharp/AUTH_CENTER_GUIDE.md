# 微服務認證中心使用指南

## 📖 概述

這是一個完整的微服務認證中心，提供以下功能：

- ✅ JWT Access Token 和 Refresh Token 機制
- ✅ 使用者會話管理（多裝置登入）
- ✅ 登入歷史追蹤和安全審計
- ✅ Token 驗證服務（供其他微服務使用）
- ✅ 防暴力破解保護
- ✅ 自動清理過期 Token 和會話
- ✅ 完整的 Swagger API 文檔

## 🚀 快速開始

### 1. 設定資料庫

確保 `appsettings.json` 中的資料庫連接字串正確：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=authcenter;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### 2. 執行資料庫遷移

```bash
# 安裝 EF Core Tools
dotnet tool install --global dotnet-ef

# 創建遷移
dotnet ef migrations add AddAuthCenterTables

# 更新資料庫
dotnet ef database update
```

### 3. 設定 JWT 密鑰

在 `appsettings.json` 中設定強密鑰（至少 32 個字元）：

```json
{
  "Jwt": {
    "SecretKey": "your-very-long-and-secure-secret-key-minimum-32-characters",
    "Issuer": "AuthCenter.Api",
    "Audience": "Microservices.Client",
    "ExpirationMinutes": "30",
    "RefreshTokenExpirationDays": "7"
  }
}
```

### 4. 啟動服務

```bash
dotnet run
```

訪問 Swagger UI：`http://localhost:5000/swagger`

## 📡 API 端點

### 認證相關

#### 1. 登入
```http
POST /api/auth/login
Content-Type: application/json

{
  "userId": 1,
  "password": "yourpassword"
}
```

回應：
```json
{
  "code": 200,
  "message": "登入成功",
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "aBc123...",
    "tokenType": "Bearer",
    "expiresAt": "2026-01-14T12:30:00Z",
    "refreshTokenExpiresAt": "2026-01-21T11:30:00Z",
    "sessionId": "uuid-session-id"
  }
}
```

#### 2. 刷新 Token
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "aBc123..."
}
```

#### 3. 撤銷 Token（登出）
```http
POST /api/auth/revoke
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "refreshToken": "aBc123...",
  "revokeAllDevices": false
}
```

#### 4. 驗證 Token（供其他微服務使用）
```http
POST /api/auth/validate
Content-Type: application/json

{
  "token": "eyJhbGc..."
}
```

回應：
```json
{
  "code": 200,
  "message": "Token 驗證成功",
  "data": {
    "isValid": true,
    "userId": 1,
    "idNo": "A123456789",
    "name": "使用者名稱",
    "expiresAt": "2026-01-14T12:30:00Z",
    "tokenId": "uuid-token-id"
  }
}
```

#### 5. 取得當前使用者資訊
```http
GET /api/auth/me
Authorization: Bearer {accessToken}
```

### 會話管理

#### 1. 取得所有活躍會話
```http
GET /api/session/active
Authorization: Bearer {accessToken}
```

回應：
```json
{
  "code": 200,
  "message": "獲取成功",
  "data": [
    {
      "sessionId": "uuid-session-id",
      "deviceName": "Windows 電腦",
      "ipAddress": "192.168.1.100",
      "loginAt": "2026-01-14T10:00:00Z",
      "lastActivityAt": "2026-01-14T11:30:00Z",
      "isCurrent": true,
      "expiresAt": "2026-01-21T10:00:00Z"
    }
  ]
}
```

#### 2. 結束指定會話
```http
DELETE /api/session/{sessionId}
Authorization: Bearer {accessToken}
```

#### 3. 取得登入歷史
```http
GET /api/session/history?limit=50
Authorization: Bearer {accessToken}
```

## 🔧 其他微服務整合

### 方案 1：在其他微服務中驗證 Token

在其他微服務中，調用認證中心的 `/api/auth/validate` 端點：

```csharp
// 在你的其他微服務中
public async Task<bool> ValidateTokenAsync(string token)
{
    var client = _httpClientFactory.CreateClient();
    var response = await client.PostAsJsonAsync(
        "http://auth-center/api/auth/validate",
        new { Token = token }
    );

    if (!response.IsSuccessStatusCode)
        return false;

    var result = await response.Content.ReadFromJsonAsync<ValidateTokenResponse>();
    return result?.Data?.IsValid == true;
}
```

### 方案 2：使用相同的 JWT 密鑰（推薦）

在其他微服務中使用相同的 JWT 設定，直接驗證 Token：

```json
// 其他微服務的 appsettings.json
{
  "Jwt": {
    "SecretKey": "same-secret-key-as-auth-center",
    "Issuer": "AuthCenter.Api",
    "Audience": "Microservices.Client"
  }
}
```

```csharp
// 在其他微服務的 Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = "AuthCenter.Api",
            ValidateAudience = true,
            ValidAudience = "Microservices.Client",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

這樣其他微服務就可以直接驗證 JWT，無需每次調用認證中心。

## 🔒 安全性功能

### 1. Refresh Token Rotation
每次刷新 Token 時，舊的 Refresh Token 會被撤銷，並生成新的 Refresh Token，防止 Token 被竊取後持續使用。

### 2. 防暴力破解
15 分鐘內失敗 5 次會暫時鎖定帳號。

### 3. Token 加密存儲
Refresh Token 在資料庫中以 SHA-256 加密存儲。

### 4. 會話追蹤
記錄每個登入的 IP、裝置、時間，方便審計和異常檢測。

### 5. 自動清理
背景服務每小時自動清理過期的 Token、會話和舊的登入歷史。

## 📊 資料表說明

### User
原有的使用者資料表。

### RefreshToken
存儲 Refresh Token，支援 Token Rotation 和撤銷機制。

### UserSession
追蹤使用者的活躍會話，支援多裝置管理。

### LoginHistory
記錄所有登入嘗試（成功和失敗），用於安全審計。

## ⚙️ 配置參數

| 參數 | 說明 | 預設值 |
|-----|------|--------|
| `Jwt:SecretKey` | JWT 簽名密鑰 | 必須設定 |
| `Jwt:Issuer` | Token 發行者 | AuthCenter.Api |
| `Jwt:Audience` | Token 受眾 | Microservices.Client |
| `Jwt:ExpirationMinutes` | Access Token 有效期（分鐘） | 30 |
| `Jwt:RefreshTokenExpirationDays` | Refresh Token 有效期（天） | 7 |

## 🧪 測試

使用 Swagger UI 測試所有端點：
1. 訪問 `http://localhost:5000/swagger`
2. 先調用 `/api/auth/login` 登入
3. 複製返回的 `accessToken`
4. 點擊右上角的 "Authorize" 按鈕
5. 輸入 `Bearer {accessToken}`
6. 現在可以測試需要認證的端點

## 🐛 常見問題

### Q: 如何增加 Access Token 的有效期？
A: 修改 `appsettings.json` 中的 `Jwt:ExpirationMinutes`。

### Q: 如何禁止多裝置同時登入？
A: 在 `AuthService.LoginAsync` 中添加邏輯，先調用 `_sessionService.EndAllUserSessionsAsync(user.Id)` 結束所有舊會話。

### Q: 如何實現 "記住我" 功能？
A: 可以根據客戶端請求動態調整 `RefreshTokenExpirationDays`，例如記住我設定為 30 天。

### Q: Token 被盜用怎麼辦？
A: 使用者可以查看活躍會話（`/api/session/active`），手動結束可疑會話。或者調用 `/api/auth/revoke` 並設定 `revokeAllDevices: true` 登出所有裝置。

## 📝 下一步建議

1. **實現使用者註冊** - 添加註冊端點
2. **實現忘記密碼** - 郵件驗證碼重設密碼
3. **實現 Two-Factor Authentication (2FA)** - 提高安全性
4. **實現角色和權限管理** - 在 JWT 中加入角色資訊
5. **添加速率限制** - 使用 AspNetCoreRateLimit
6. **實現 IP 白名單/黑名單** - 進階安全控制
7. **添加日誌記錄** - 使用 Serilog 記錄所有操作
8. **實現使用者鎖定機制** - 多次失敗登入後鎖定帳號
9. **添加郵件/簡訊通知** - 異常登入時通知使用者
10. **實現 OAuth2/OpenID Connect** - 支援第三方登入

## 📄 授權

MIT License
