# Entity Framework Core Migrations 完整操作指南

## 什麼是 Migrations？

Migrations 是 Entity Framework Core 提供的資料庫版本管理系統，可以：
- 追蹤資料庫結構變更歷史
- 自動生成 SQL 腳本
- 支援版本回滾
- 適合團隊協作和生產環境

## 前置準備

### 1. 確認已安裝 EF Core 工具

專案已經包含 `Microsoft.EntityFrameworkCore.Tools` 套件，可以直接使用。

如果需要全域安裝 EF Core 工具：

```bash
dotnet tool install --global dotnet-ef
```

檢查是否已安裝：

```bash
dotnet ef --version
```

### 2. 確認資料庫連接字串

確保 `appsettings.json` 或 `appsettings.Development.json` 中有正確的連接字串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Picture2Text;User Id=sa;Password=your_password;TrustServerCertificate=True;"
  }
}
```

## 基本操作

### 1. 建立第一個 Migration（初始化）

```bash
dotnet ef migrations add InitialCreate
```

**說明**：
- `InitialCreate` 是 Migration 的名稱，可以自訂
- 會在專案根目錄建立 `Migrations` 資料夾
- 生成兩個檔案：
  - `{timestamp}_InitialCreate.cs` - Migration 類別（包含 Up 和 Down 方法）
  - `ApplicationDbContextModelSnapshot.cs` - 當前模型快照

**範例輸出**：
```
Build started...
Build succeeded.
Done. To undo this action, use 'dotnet ef migrations remove'
```

### 2. 查看 Migration 狀態

```bash
dotnet ef migrations list
```

**說明**：
- 顯示所有已建立的 Migration
- 標記哪些已套用到資料庫（Applied）
- 標記哪些尚未套用（Pending）

**範例輸出**：
```
Build started...
Build succeeded.
20240101120000_InitialCreate (Pending)
```

### 3. 套用 Migration 到資料庫

```bash
dotnet ef database update
```

**說明**：
- 套用所有待處理的 Migration 到資料庫
- 如果資料庫不存在，會自動創建
- 會執行 Migration 中的 `Up` 方法

**範例輸出**：
```
Build started...
Build succeeded.
Applying migration '20240101120000_InitialCreate'.
Done.
```

### 4. 套用到特定 Migration

```bash
# 套用到指定的 Migration
dotnet ef database update InitialCreate

# 或使用完整名稱
dotnet ef database update 20240101120000_InitialCreate
```

### 5. 回滾到上一個 Migration

```bash
dotnet ef database update PreviousMigrationName
```

**範例**：
```bash
# 假設有兩個 Migration: InitialCreate 和 AddUserTable
# 回滾到 InitialCreate
dotnet ef database update InitialCreate
```

### 6. 回滾所有 Migration（刪除所有資料表）

```bash
dotnet ef database update 0
```

**警告**：這會刪除資料庫中所有由 Migration 創建的資料表！

## 進階操作

### 1. 移除最後一個 Migration（未套用時）

如果 Migration 尚未套用到資料庫，可以移除：

```bash
dotnet ef migrations remove
```

**說明**：
- 只能移除最後一個 Migration
- 如果已經套用到資料庫，需要先回滾才能移除

### 2. 生成 SQL 腳本

生成 Migration 的 SQL 腳本，而不直接套用：

```bash
# 生成所有待處理 Migration 的 SQL
dotnet ef migrations script

# 生成從特定 Migration 到另一個 Migration 的 SQL
dotnet ef migrations script InitialCreate AddUserTable

# 生成從特定 Migration 到最新版本的 SQL
dotnet ef migrations script InitialCreate

# 輸出到檔案
dotnet ef migrations script -o migration.sql
```

**範例輸出**（migration.sql）：
```sql
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [User] (
    [ID] int NOT NULL IDENTITY,
    [ID_NO] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Password] nvarchar(255) NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY ([ID])
);
GO

CREATE UNIQUE INDEX [IX_User_ID_NO] ON [User] ([ID_NO]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240101120000_InitialCreate', N'9.0.0');
GO

COMMIT;
GO
```

### 3. 檢查資料庫狀態

```bash
dotnet ef database info
```

**說明**：
- 顯示資料庫中已套用的 Migration
- 顯示待處理的 Migration
- 顯示資料庫連接資訊

### 4. 指定 DbContext

如果專案中有多個 DbContext：

```bash
dotnet ef migrations add InitialCreate --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext
```

### 5. 指定連接字串

不從設定檔讀取，直接指定連接字串：

```bash
dotnet ef migrations add InitialCreate --connection "Server=localhost;Database=testdb;User Id=sa;Password=password;TrustServerCertificate=True;"
```

### 6. 指定專案和啟動專案

如果有多個專案：

```bash
dotnet ef migrations add InitialCreate --project backend-csharp --startup-project backend-csharp
```

## 常見工作流程

### 場景 1：初始化新專案

```bash
# 1. 建立第一個 Migration
dotnet ef migrations add InitialCreate

# 2. 套用到資料庫
dotnet ef database update

# 3. 確認狀態
dotnet ef migrations list
```

### 場景 2：新增資料表或欄位

```bash
# 1. 修改 Model（例如：在 User.cs 中新增欄位）
# 2. 建立新的 Migration
dotnet ef migrations add AddEmailToUser

# 3. 檢查生成的 SQL（可選）
dotnet ef migrations script AddEmailToUser

# 4. 套用到資料庫
dotnet ef database update
```

### 場景 3：修改現有欄位

```bash
# 1. 修改 Model
# 2. 建立 Migration
dotnet ef migrations add ChangeUserNameLength

# 3. 套用到資料庫
dotnet ef database update
```

### 場景 4：回滾變更

```bash
# 1. 查看 Migration 列表
dotnet ef migrations list

# 2. 回滾到上一個版本
dotnet ef database update PreviousMigrationName

# 3. 移除最後一個 Migration（如果尚未套用）
dotnet ef migrations remove
```

### 場景 5：生產環境部署

```bash
# 1. 生成 SQL 腳本
dotnet ef migrations script -o production-migration.sql

# 2. 在生產環境資料庫執行 SQL 腳本
# 或使用以下命令直接套用（需確保連接字串正確）
dotnet ef database update --connection "生產環境連接字串"
```

## 在程式碼中使用 Migrations

### 自動套用 Migrations（啟動時）

在 `Program.cs` 中，可以使用 `Database.Migrate()` 替代 `EnsureCreated()`：

```csharp
// 自動套用 Migrations（如果存在）
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate(); // 自動套用所有待處理的 Migration
        logger.LogInformation("資料庫 Migrations 已套用");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "資料庫 Migration 時發生錯誤");
    }
}
```

**優點**：
- 自動套用所有待處理的 Migration
- 適合生產環境自動部署
- 會追蹤 Migration 歷史

**注意**：
- 確保應用程式有資料庫的完整權限
- 建議在部署前先測試 Migration

## Migration 檔案結構

### Migration 類別範例

```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 套用 Migration 時執行的操作
        migrationBuilder.CreateTable(
            name: "User",
            columns: table => new
            {
                ID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ID_NO = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_User", x => x.ID);
            });

        migrationBuilder.CreateIndex(
            name: "IX_User_ID_NO",
            table: "User",
            column: "ID_NO",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 回滾 Migration 時執行的操作
        migrationBuilder.DropIndex(
            name: "IX_User_ID_NO",
            table: "User");

        migrationBuilder.DropTable(
            name: "User");
    }
}
```

## 常見問題

### 1. Migration 名稱衝突

**問題**：`A migration with the name 'InitialCreate' already exists`

**解決方案**：
- 使用不同的名稱：`dotnet ef migrations add InitialCreateV2`
- 或移除舊的 Migration：`dotnet ef migrations remove`

### 2. 資料庫連接失敗

**問題**：`A network-related or instance-specific error occurred`

**解決方案**：
- 檢查連接字串是否正確
- 確認 SQL Server 正在運行
- 檢查防火牆設定

### 3. Migration 已套用但想重新套用

**問題**：Migration 已經在資料庫中，但想重新執行

**解決方案**：
```bash
# 先回滾
dotnet ef database update PreviousMigrationName

# 再重新套用
dotnet ef database update
```

### 4. 手動修改資料庫後 Migration 不同步

**問題**：手動修改了資料庫結構，但 Migration 記錄不同步

**解決方案**：
```bash
# 1. 生成新的 Migration 來同步
dotnet ef migrations add SyncWithDatabase

# 2. 檢查生成的 Migration，可能需要手動調整
# 3. 套用 Migration
dotnet ef database update
```

### 5. 刪除所有 Migration 重新開始

**警告**：這會刪除所有 Migration 歷史！

```bash
# 1. 刪除 Migrations 資料夾
rm -rf Migrations  # Linux/macOS
# 或
Remove-Item -Recurse -Force Migrations  # Windows PowerShell

# 2. 建立新的初始 Migration
dotnet ef migrations add InitialCreate

# 3. 套用到資料庫（會重新創建所有表）
dotnet ef database update
```

## 最佳實踐

1. **命名規範**：
   - 使用描述性的名稱：`AddUserTable`、`ChangePasswordLength`
   - 避免使用 `Migration1`、`Migration2` 等無意義名稱

2. **版本控制**：
   - 將 `Migrations` 資料夾加入版本控制（Git）
   - 不要刪除已套用的 Migration

3. **測試**：
   - 在開發環境先測試 Migration
   - 使用 `dotnet ef migrations script` 檢查 SQL 腳本

4. **生產環境**：
   - 使用 SQL 腳本而非直接執行命令
   - 在套用前備份資料庫
   - 在維護時間窗口執行

5. **團隊協作**：
   - 拉取最新程式碼後執行 `dotnet ef database update`
   - 不要修改已套用的 Migration
   - 如有衝突，建立新的 Migration 來解決

## 完整範例：從零開始

```bash
# 1. 確認專案結構
cd backend-csharp

# 2. 建立第一個 Migration
dotnet ef migrations add InitialCreate

# 3. 查看生成的 Migration
cat Migrations/*_InitialCreate.cs

# 4. 生成 SQL 腳本（可選）
dotnet ef migrations script -o initial-create.sql

# 5. 套用到資料庫
dotnet ef database update

# 6. 確認狀態
dotnet ef migrations list
dotnet ef database info

# 7. 修改 Model（例如新增欄位）
# 編輯 Models/User.cs

# 8. 建立新的 Migration
dotnet ef migrations add AddEmailToUser

# 9. 套用到資料庫
dotnet ef database update

# 10. 查看所有 Migration
dotnet ef migrations list
```

## 相關命令速查表

| 命令 | 說明 |
|------|------|
| `dotnet ef migrations add <名稱>` | 建立新的 Migration |
| `dotnet ef migrations remove` | 移除最後一個 Migration |
| `dotnet ef migrations list` | 列出所有 Migration |
| `dotnet ef database update` | 套用所有待處理的 Migration |
| `dotnet ef database update <名稱>` | 套用到指定 Migration |
| `dotnet ef database update 0` | 回滾所有 Migration |
| `dotnet ef migrations script` | 生成 SQL 腳本 |
| `dotnet ef database info` | 查看資料庫狀態 |
| `dotnet ef dbcontext info` | 查看 DbContext 資訊 |

## 參考資源

- [Entity Framework Core Migrations 官方文檔](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [EF Core 工具參考](https://learn.microsoft.com/ef/core/cli/dotnet)
