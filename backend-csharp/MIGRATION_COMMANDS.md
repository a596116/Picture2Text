# 資料庫設定與遷移指南

## 🚀 快速開始

### 1. 安裝 EF Core Tools
```bash
dotnet tool install --global dotnet-ef
```

### 2. 配置資料庫連接
編輯 `appsettings.json`：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=authcenter;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### 3. 執行遷移
```bash
# 創建遷移
dotnet ef migrations add AddAuthCenterTables

# 更新資料庫
dotnet ef database update
```

## 📊 資料表結構

升級後的認證中心包含以下資料表：

### User（使用者）
```sql
CREATE TABLE [User] (
    [ID] INT PRIMARY KEY IDENTITY,
    [ID_NO] NVARCHAR(50) UNIQUE NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Password] NVARCHAR(255) NOT NULL
);
```

### RefreshToken（刷新令牌）
```sql
CREATE TABLE [RefreshToken] (
    [ID] INT PRIMARY KEY IDENTITY,
    [UserID] INT NOT NULL,
    [Token] NVARCHAR(500) NOT NULL,
    [TokenId] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [IsRevoked] BIT NOT NULL,
    [DeviceInfo] NVARCHAR(500),
    [IpAddress] NVARCHAR(50),
    FOREIGN KEY ([UserID]) REFERENCES [User]([ID])
);
```

### UserSession（使用者會話）
```sql
CREATE TABLE [UserSession] (
    [ID] INT PRIMARY KEY IDENTITY,
    [UserID] INT NOT NULL,
    [SessionId] NVARCHAR(100) UNIQUE NOT NULL,
    [RefreshTokenId] INT,
    [DeviceName] NVARCHAR(200),
    [LoginAt] DATETIME2 NOT NULL,
    [LastActivityAt] DATETIME2 NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [IsActive] BIT NOT NULL,
    FOREIGN KEY ([UserID]) REFERENCES [User]([ID])
);
```

### LoginHistory（登入歷史）
```sql
CREATE TABLE [LoginHistory] (
    [ID] INT PRIMARY KEY IDENTITY,
    [UserID] INT,
    [AttemptedUserId] NVARCHAR(50) NOT NULL,
    [IsSuccess] BIT NOT NULL,
    [FailureReason] NVARCHAR(200),
    [IpAddress] NVARCHAR(50),
    [AttemptedAt] DATETIME2 NOT NULL,
    FOREIGN KEY ([UserID]) REFERENCES [User]([ID])
);
```

## 🔧 常見連接字串

### 本地 SQL Server（Windows 驗證）
```json
"DefaultConnection": "Server=localhost;Database=authcenter;Integrated Security=True;TrustServerCertificate=True;"
```

### 本地 SQL Server（SQL 驗證）
```json
"DefaultConnection": "Server=localhost;Database=authcenter;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

### Docker SQL Server
```json
"DefaultConnection": "Server=localhost,1433;Database=authcenter;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
```

### Azure SQL Database
```json
"DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=authcenter;User Id=yourusername;Password=yourpassword;Encrypt=True;"
```

## 📝 常用指令

### 查看遷移狀態
```bash
dotnet ef migrations list
```

### 回滾到上一個遷移
```bash
dotnet ef database update <上一個遷移名稱>
```

### 移除最後一個遷移（未應用時）
```bash
dotnet ef migrations remove
```

### 生成 SQL 腳本（不直接執行）
```bash
dotnet ef migrations script
```

### 指定環境
```bash
# 開發環境
dotnet ef database update --environment Development

# 生產環境
dotnet ef database update --environment Production
```

## 🎯 創建測試資料

```sql
-- 創建測試使用者（密碼：password123）
INSERT INTO [User] ([ID_NO], [Name], [Password])
VALUES (
    'A123456789',
    '測試使用者',
    '$2a$11$Xj9R7ZqYX5Z7kQ8K1ZqW4.H5Y7QH5Y7QH5Y7QH5Y7QH5Y7QH5Y7QH5'
);
```

## ⚠️ 注意事項

1. **備份資料庫** - 執行遷移前務必備份
2. **測試環境先行** - 先在測試環境驗證
3. **檢查索引** - 確認所有索引已正確創建
4. **權限檢查** - 確保資料庫使用者有足夠權限

## 🐛 常見問題

### 問題：連接失敗
```bash
# 檢查 SQL Server 是否運行
# Windows
Get-Service MSSQLSERVER

# 檢查連接字串是否正確
# 測試連接
sqlcmd -S localhost -U sa -P YourPassword
```

### 問題：遷移失敗
```bash
# 清除並重新創建
dotnet ef database drop --force
dotnet ef database update
```

### 問題：找不到 dotnet-ef
```bash
# 更新工具
dotnet tool update --global dotnet-ef
```
