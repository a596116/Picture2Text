# Picture2Text API (ASP.NET Core)

這是 Picture2Text 專案的 ASP.NET Core 後端 API，使用 Entity Framework Core 連接 MSSQL 資料庫。

## 功能

- ✅ 使用者登入（JWT 認證）
- ✅ 使用者資料查詢（Profile）

## 技術棧

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- BCrypt.Net (密碼加密)
- Swagger/OpenAPI

## 專案結構

```
backend-csharp/
├── Controllers/              # API 控制器
│   ├── AuthController.cs        # 登入 API
│   └── ProfileController.cs     # 使用者資料 API
├── Data/                     # 資料存取層
│   └── ApplicationDbContext.cs
├── DTOs/                     # 資料傳輸物件
│   ├── Requests/                # 請求 DTOs
│   │   └── LoginRequest.cs
│   └── Responses/               # 回應 DTOs
│       ├── LoginResponse.cs
│       ├── ProfileResponse.cs
│       ├── ApiResponse.cs
│       └── ValidationErrorResponse.cs
├── Filters/                  # 過濾器
│   └── ValidationErrorFilter.cs # 驗證錯誤過濾器
├── Models/                    # 資料模型
│   └── User.cs
├── Services/                  # 業務邏輯服務
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── IJwtService.cs
│   └── JwtService.cs
├── appsettings.json           # 應用程式設定
└── Program.cs                 # 應用程式入口點
```

## 設定

### 資料庫連線

在 `appsettings.json` 中設定資料庫連線字串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=testdb;User Id=sa;Password=password;TrustServerCertificate=True;"
  }
}
```

### JWT 設定

在 `appsettings.json` 中設定 JWT 相關參數：

```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-change-this-to-a-long-random-string-at-least-32-characters",
    "Issuer": "Picture2Text.Api",
    "Audience": "Picture2Text.Client",
    "ExpirationMinutes": "30"
  }
}
```

## 資料庫

### 自動創建資料庫和資料表

**重要**：應用程式會在啟動時自動檢查並創建資料庫和資料表（如果不存在）。

當您首次啟動應用程式時，系統會：
- 自動創建資料庫（如果不存在）
- 自動創建 `User` 資料表（如果不存在）
- 根據模型配置創建所有必要的索引

**User 表的結構**（會自動創建）：

```sql
CREATE TABLE [User] (
    [ID] INT PRIMARY KEY IDENTITY(1,1),
    [ID_NO] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Password] NVARCHAR(255) NOT NULL
);

-- 自動創建的索引
CREATE UNIQUE INDEX IX_User_ID_NO ON [User]([ID_NO]);
```

**注意事項**：
- 使用 `Database.EnsureCreated()` 方法自動創建
- 只會在資料庫或資料表不存在時創建
- 如果資料表已存在但結構不同，**不會自動更新**表結構
- 適合開發環境使用
- 生產環境建議使用 Migrations（詳見 `DATABASE_SETUP.md`）

## API 端點

### 1. 登入

**POST** `/api/auth/login`

請求體：
```json
{
  "username": "testuser",
  "password": "password123"
}
```

回應：
```json
{
  "code": 200,
  "message": "登入成功",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresAt": "2024-01-01T12:00:00Z"
  }
}
```

### 2. 獲取使用者資料

**GET** `/api/profile`

Headers:
```
Authorization: Bearer {token}
```

回應：
```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "id": 1,
    "username": "testuser",
    "email": "test@example.com",
    "isActive": true,
    "isSuperuser": false,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
}
```

## 執行

### 使用 .NET CLI

```bash
# 還原套件
dotnet restore

# 執行應用程式
dotnet run
```

### 啟用熱更新（Hot Reload）

使用 `dotnet watch` 命令可以啟用熱更新，當您修改程式碼時，應用程式會自動重新編譯並重啟：

```bash
# 使用熱更新執行應用程式
dotnet watch run
```

**熱更新功能：**
- 自動監聽 `.cs`、`.json`、`.csproj` 等檔案變更
- 修改程式碼後自動重新編譯
- 自動重啟應用程式
- 保持應用程式狀態（部分情況下）

**注意事項：**
- 某些重大變更（如修改 `Program.cs` 的結構）可能需要手動重啟
- 資料庫連線字串變更需要重啟
- 配置檔案變更會觸發重啟

應用程式預設會在 `https://localhost:5001` 或 `http://localhost:5000` 執行。

### 使用 Visual Studio

1. 開啟 `Picture2Text.Api.csproj`
2. 按 F5 執行

## Swagger 文檔

在開發環境下，可以透過以下網址存取 Swagger 文檔：

- `https://localhost:5001/swagger` 或
- `http://localhost:5000/swagger`

## 注意事項

1. **密碼加密**：專案使用 BCrypt 進行密碼加密，確保資料庫中的密碼是使用 BCrypt 加密的。
2. **JWT Secret Key**：請在生產環境中使用強隨機字串作為 JWT Secret Key。
3. **資料庫連線**：請根據實際環境調整連線字串。
4. **CORS**：目前設定為允許所有來源，生產環境請調整為特定來源。
