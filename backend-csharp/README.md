# 🔐 微服務認證中心 (ASP.NET Core)

企業級微服務認證中心，提供完整的 JWT 認證、Refresh Token、會話管理和安全審計功能。

## ✨ 核心功能

### 認證功能
- ✅ JWT Access Token + Refresh Token 雙重認證
- ✅ Token Rotation（自動撤銷舊 token）
- ✅ Token 驗證服務（供其他微服務使用）
- ✅ 多裝置登入支援
- ✅ 安全登出（單一或全部裝置）

### 安全功能
- ✅ 防暴力破解（15分鐘5次失敗鎖定）
- ✅ Token 加密存儲（SHA-256）
- ✅ 登入歷史追蹤
- ✅ 會話管理（追蹤所有活躍登入）
- ✅ IP、裝置、User-Agent 記錄

### 管理功能
- ✅ 查看所有活躍會話
- ✅ 結束指定會話
- ✅ 查看登入歷史
- ✅ 自動清理過期資料（背景服務）

### 微服務支援
- ✅ 集中式驗證（調用 API）
- ✅ 分散式驗證（共享 JWT 密鑰）
- ✅ 完整的整合範例和文檔

## 🛠️ 技術棧

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core 8.0
- SQL Server 2019+
- JWT Bearer Authentication
- BCrypt.Net (密碼加密)
- Swagger/OpenAPI 文檔
- Background Service (自動清理)

## 📁 專案結構

```
backend-csharp/
├── Controllers/                      # API 控制器
│   ├── AuthController.cs             # 認證 API（登入、刷新、驗證、登出）
│   ├── SessionController.cs          # 會話管理 API
│   └── ProfileController.cs          # 使用者資料 API
│
├── Services/                         # 業務邏輯服務
│   ├── AuthService.cs                # 認證服務（主要業務邏輯）
│   ├── JwtService.cs                 # JWT Token 生成和驗證
│   ├── RefreshTokenService.cs        # Refresh Token 管理
│   ├── SessionService.cs             # 會話管理
│   ├── LoginHistoryService.cs        # 登入歷史記錄
│   └── TokenCleanupService.cs        # 背景清理服務
│
├── Models/                           # 資料模型
│   ├── User.cs                       # 使用者模型
│   ├── RefreshToken.cs               # Refresh Token 模型
│   ├── UserSession.cs                # 會話模型
│   └── LoginHistory.cs               # 登入歷史模型
│
├── DTOs/                             # 資料傳輸物件
│   ├── Requests/                     # 請求 DTOs
│   │   ├── LoginRequest.cs
│   │   ├── RefreshTokenRequest.cs
│   │   ├── RevokeTokenRequest.cs
│   │   └── ValidateTokenRequest.cs
│   └── Responses/                    # 回應 DTOs
│       ├── ApiResponse.cs
│       ├── TokenResponse.cs
│       ├── ValidateTokenResponse.cs
│       ├── SessionResponse.cs
│       └── LoginHistoryResponse.cs
│
├── Data/                             # 資料存取層
│   └── ApplicationDbContext.cs
│
├── Filters/                          # 過濾器
│   └── ValidationErrorFilter.cs
│
├── Documentation/                    # 📚 完整文檔
│   ├── AUTH_CENTER_GUIDE.md          # 使用指南
│   ├── ARCHITECTURE.md               # 架構設計
│   ├── MICROSERVICE_INTEGRATION_EXAMPLE.md  # 整合範例
│   ├── MIGRATION_COMMANDS.md         # 資料庫遷移
│   ├── UPGRADE_SUMMARY.md            # 升級總結
│   └── QUICK_START_CHECKLIST.md      # 快速啟動檢查清單
│
├── appsettings.json                  # 應用程式設定
└── Program.cs                        # 應用程式入口點
```

## ⚙️ 快速開始

### 1. 安裝依賴

```bash
# 安裝 EF Core Tools
dotnet tool install --global dotnet-ef

# 還原套件
dotnet restore
```

### 2. 設定資料庫

編輯 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=authcenter;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### 3. 執行資料庫遷移

```bash
# 創建遷移
dotnet ef migrations add AddAuthCenterTables

# 更新資料庫
dotnet ef database update
```

### 4. 設定 JWT 密鑰

**重要**：生產環境請使用強密鑰（至少 64 個隨機字元）

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

### 5. 啟動服務

```bash
dotnet run
```

訪問 Swagger UI：`http://localhost:5000/swagger`

## 🗄️ 資料庫結構

系統使用 EF Core Migrations 管理資料庫，包含以下資料表：

### 資料表說明

1. **User** - 使用者基本資料
2. **RefreshToken** - Refresh Token 存儲（SHA-256 加密）
3. **UserSession** - 使用者會話追蹤
4. **LoginHistory** - 登入歷史記錄（成功/失敗）

詳細的資料表結構和關聯請參考 [ARCHITECTURE.md](./ARCHITECTURE.md)

## 📡 主要 API 端點

### 認證相關

| 方法 | 路徑 | 說明 | 認證 |
|-----|------|------|------|
| POST | `/api/auth/login` | 使用者登入，返回 Access Token 和 Refresh Token | ❌ |
| POST | `/api/auth/refresh` | 刷新 Token，獲取新的 Access Token | ❌ |
| POST | `/api/auth/revoke` | 撤銷 Token（登出） | ✅ |
| POST | `/api/auth/validate` | 驗證 Token（供其他微服務使用） | ❌ |
| GET | `/api/auth/me` | 取得當前使用者資訊 | ✅ |

### 會話管理

| 方法 | 路徑 | 說明 | 認證 |
|-----|------|------|------|
| GET | `/api/session/active` | 查看所有活躍會話 | ✅ |
| DELETE | `/api/session/{sessionId}` | 結束指定會話 | ✅ |
| GET | `/api/session/history` | 查看登入歷史 | ✅ |

### 使用者資料

| 方法 | 路徑 | 說明 | 認證 |
|-----|------|------|------|
| GET | `/api/profile` | 取得使用者詳細資料 | ✅ |

### API 使用範例

#### 1. 登入
```bash
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
```bash
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "aBc123..."
}
```

#### 3. 驗證 Token（供其他微服務使用）
```bash
POST /api/auth/validate
Content-Type: application/json

{
  "token": "eyJhbGc..."
}
```

更多 API 範例請參考 [AUTH_CENTER_GUIDE.md](./AUTH_CENTER_GUIDE.md)

## 🌐 微服務整合

### 方案 1：共享 JWT 密鑰（推薦）

在其他微服務中使用相同的 JWT 配置：

```csharp
// 其他微服務的 Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("與認證中心相同的密鑰")),
            ValidateIssuer = true,
            ValidIssuer = "AuthCenter.Api",
            ValidateAudience = true,
            ValidAudience = "Microservices.Client"
        };
    });
```

### 方案 2：調用驗證 API

在其他微服務中調用 `/api/auth/validate` 端點驗證 Token。

完整的整合範例（包含 C#、JavaScript、Flutter 等）請參考：
- [MICROSERVICE_INTEGRATION_EXAMPLE.md](./MICROSERVICE_INTEGRATION_EXAMPLE.md)

## 📚 完整文檔（已優化）

### 🚀 快速開始
| 文檔 | 說明 |
|-----|------|
| **[QUICK_START_CHECKLIST.md](./QUICK_START_CHECKLIST.md)** | ✅ 5 步快速啟動指南 |
| **[MIGRATION_COMMANDS.md](./MIGRATION_COMMANDS.md)** | 🗄️ 資料庫設定與遷移 |

### 🏗️ 架構與整合
| 文檔 | 說明 |
|-----|------|
| **[API_GATEWAY_INTEGRATION.md](./API_GATEWAY_INTEGRATION.md)** ⭐ | 🚪 API Gateway 整合完整指南（Nginx/IIS） |
| **[ARCHITECTURE.md](./ARCHITECTURE.md)** | 🏗️ 系統架構設計與資料模型 |
| **[nginx.conf.example](./nginx.conf.example)** | ⚙️ Nginx 配置範例（可直接使用） |

### 📖 API 參考
| 文檔 | 說明 |
|-----|------|
| **[AUTH_CENTER_GUIDE.md](./AUTH_CENTER_GUIDE.md)** | 📖 所有 API 端點詳細說明 |
| **Swagger UI** | 📊 互動式 API 文檔 (http://localhost:5000/swagger) |

### 📝 其他
| 文檔 | 說明 |
|-----|------|
| **[UPGRADE_SUMMARY.md](./UPGRADE_SUMMARY.md)** | 📋 升級內容總結 |

## 🧪 測試

### Swagger UI

訪問 `http://localhost:5000/swagger` 進行測試：

1. 調用 `/api/auth/login` 登入
2. 複製返回的 `accessToken`
3. 點擊右上角 "Authorize" 按鈕
4. 輸入 `Bearer {accessToken}`
5. 測試需要認證的端點

### 使用 .NET CLI

```bash
# 開發模式運行
dotnet run

# 使用熱更新
dotnet watch run

# 執行測試（如果有）
dotnet test
```

## 🔒 安全性考量

### 生產環境部署前必做

1. ✅ **更換 JWT 密鑰** - 至少 64 個隨機字元
2. ✅ **啟用 HTTPS** - 強制使用加密連接
3. ✅ **更新資料庫密碼** - 使用強密碼
4. ✅ **調整 CORS 政策** - 限制允許的來源
5. ✅ **保護 Swagger UI** - 添加認證或禁用
6. ✅ **使用環境變數** - 敏感配置不要寫在代碼中
7. ✅ **啟用日誌記錄** - 監控所有操作
8. ✅ **設定速率限制** - 防止濫用

### 內建安全功能

- ✅ Refresh Token Rotation（防 Token 被盜用）
- ✅ Token 加密存儲（SHA-256）
- ✅ 防暴力破解（15分鐘5次失敗鎖定）
- ✅ 登入歷史追蹤（安全審計）
- ✅ 會話管理（可查看和結束可疑會話）

## 🎯 主要特性

### 為什麼選擇這個認證中心？

1. **完整功能** - Refresh Token、會話管理、登入歷史一應俱全
2. **微服務友好** - 支援集中式和分散式驗證
3. **安全可靠** - 多重安全機制，防止常見攻擊
4. **易於整合** - 完整的文檔和代碼範例
5. **自動維護** - 背景服務自動清理過期資料
6. **生產就緒** - 可直接用於生產環境

## 🚀 下一步

1. 📖 閱讀 [AUTH_CENTER_GUIDE.md](./AUTH_CENTER_GUIDE.md) 了解所有功能
2. ✅ 使用 [QUICK_START_CHECKLIST.md](./QUICK_START_CHECKLIST.md) 進行部署
3. 🔗 參考 [MICROSERVICE_INTEGRATION_EXAMPLE.md](./MICROSERVICE_INTEGRATION_EXAMPLE.md) 整合其他服務
4. 🏗️ 了解 [ARCHITECTURE.md](./ARCHITECTURE.md) 以便自訂和擴展

## 📝 授權

MIT License

---

**需要幫助？** 查看文檔或檢查 Swagger UI 的詳細 API 說明。
