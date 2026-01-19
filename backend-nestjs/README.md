# Auth Center - NestJS Version

微服務認證中心 - NestJS 版本

## 功能特色

- 🔐 JWT 認證與 Refresh Token 機制
- 🔄 Token 自動輪換（Token Rotation）
- 🛡️ 暴力破解防護（登入失敗鎖定）
- 📱 多裝置 Session 管理
- 📊 登入歷史記錄與安全審計
- ⏰ 自動清理過期 Token 與 Session
- 📖 Swagger API 文件

## 技術棧

- **Framework**: NestJS 10
- **Language**: TypeScript 5
- **Database**: SQL Server (TypeORM)
- **Authentication**: Passport.js + JWT
- **Documentation**: Swagger/OpenAPI
- **Task Scheduling**: @nestjs/schedule

## 快速開始

### 1. 安裝依賴

```bash
npm install
```

### 2. 設定環境變數

複製 `.env.example` 為 `.env` 並修改設定：

```bash
cp .env.example .env
```

### 3. 資料庫設定

確保 SQL Server 已啟動，並建立對應的資料庫。

### 4. 執行應用程式

開發模式：
```bash
npm run start:dev
```

生產模式：
```bash
npm run build
npm run start:prod
```

## API 端點

### Auth API (`/api/auth`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/login` | ❌ | 使用者登入 |
| POST | `/refresh` | ❌ | 刷新 Token |
| POST | `/revoke` | ✅ | 登出 |
| POST | `/validate` | ❌ | 驗證 Token（內部使用） |
| GET | `/me` | ✅ | 取得當前使用者 |

### Session API (`/api/session`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/active` | ✅ | 取得活躍 Session 列表 |
| DELETE | `/:sessionId` | ✅ | 結束指定 Session |
| GET | `/history` | ✅ | 取得登入歷史 |

### Profile API (`/api/profile`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | ✅ | 取得使用者資料 |

## 環境變數說明

| 變數 | 說明 | 預設值 |
|------|------|--------|
| `PORT` | 應用程式埠號 | 5000 |
| `NODE_ENV` | 執行環境 | development |
| `DB_HOST` | 資料庫主機 | localhost |
| `DB_PORT` | 資料庫埠號 | 1433 |
| `DB_USERNAME` | 資料庫使用者 | sa |
| `DB_PASSWORD` | 資料庫密碼 | - |
| `DB_DATABASE` | 資料庫名稱 | testdb |
| `JWT_SECRET_KEY` | JWT 金鑰 | - |
| `JWT_ISSUER` | JWT 發行者 | AuthCenter.Api |
| `JWT_AUDIENCES` | JWT 受眾（逗號分隔） | - |
| `JWT_EXPIRATION_MINUTES` | Access Token 有效時間（分鐘） | 30 |
| `JWT_REFRESH_TOKEN_EXPIRATION_DAYS` | Refresh Token 有效時間（天） | 7 |
| `CORS_ALLOWED_ORIGINS` | CORS 允許來源（逗號分隔） | - |

## 資料庫 Schema

### User 表

```sql
CREATE TABLE [User] (
    Id INT PRIMARY KEY IDENTITY,
    IdNo NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Password NVARCHAR(255) NOT NULL
);
```

### RefreshToken 表

```sql
CREATE TABLE RefreshToken (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    Token NVARCHAR(500) NOT NULL,
    TokenId NVARCHAR(100) NOT NULL UNIQUE,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ExpiresAt DATETIME NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    RevokedAt DATETIME NULL,
    ReplacedByToken NVARCHAR(500) NULL,
    DeviceInfo NVARCHAR(500) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(1000) NULL,
    LastUsedAt DATETIME NULL,
    FOREIGN KEY (UserId) REFERENCES [User](Id) ON DELETE CASCADE
);
```

### UserSession 表

```sql
CREATE TABLE UserSession (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    SessionId NVARCHAR(100) NOT NULL UNIQUE,
    RefreshTokenId INT NULL,
    DeviceName NVARCHAR(200) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(1000) NULL,
    LoginAt DATETIME NOT NULL,
    LastActivityAt DATETIME NOT NULL,
    LogoutAt DATETIME NULL,
    ExpiresAt DATETIME NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (UserId) REFERENCES [User](Id) ON DELETE CASCADE,
    FOREIGN KEY (RefreshTokenId) REFERENCES RefreshToken(Id) ON DELETE SET NULL
);
```

### LoginHistory 表

```sql
CREATE TABLE LoginHistory (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NULL,
    AttemptedUserId NVARCHAR(50) NOT NULL,
    IsSuccess BIT NOT NULL,
    FailureReason NVARCHAR(200) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(1000) NULL,
    DeviceInfo NVARCHAR(500) NULL,
    AttemptedAt DATETIME NOT NULL,
    Location NVARCHAR(200) NULL,
    FOREIGN KEY (UserId) REFERENCES [User](Id) ON DELETE SET NULL
);
```

## Swagger 文件

啟動應用程式後，訪問 `http://localhost:5000/swagger` 查看 API 文件。

## 專案結構

```
src/
├── config/                 # 設定檔
├── controllers/            # 控制器
├── database/               # 資料庫設定與遷移
├── decorators/             # 自訂裝飾器
├── dto/                    # 資料傳輸物件
│   ├── request/            # 請求 DTO
│   └── response/           # 回應 DTO
├── entities/               # TypeORM 實體
├── filters/                # 例外過濾器
├── guards/                 # 認證守衛
├── interfaces/             # TypeScript 介面
├── services/               # 業務邏輯服務
├── app.module.ts           # 主模組
└── main.ts                 # 進入點
```

## 安全特性

1. **暴力破解防護**: 15 分鐘內登入失敗 5 次將鎖定帳戶
2. **Token 輪換**: 每次 Refresh 都會產生新的 Token
3. **Token 雜湊**: Refresh Token 以 SHA-256 雜湊儲存
4. **Session 追蹤**: 可查看並終止任何裝置的 Session
5. **登入審計**: 記錄所有登入嘗試（成功/失敗）
6. **自動清理**: 每小時自動清理過期 Token 和 Session

## 與 C# 版本的對應

| C# 版本 | NestJS 版本 |
|---------|-------------|
| `AuthController` | `AuthController` |
| `SessionController` | `SessionController` |
| `ProfileController` | `ProfileController` |
| `JwtService` | `JwtTokenService` |
| `RefreshTokenService` | `RefreshTokenService` |
| `SessionService` | `SessionService` |
| `LoginHistoryService` | `LoginHistoryService` |
| `AuthService` | `AuthService` |
| `TokenCleanupService` | `TokenCleanupService` |

## License

UNLICENSED
