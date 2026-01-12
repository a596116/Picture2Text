# 資料庫連接設定指南

## 連接字串設定

專案使用 Entity Framework Core 連接 MSSQL 資料庫。連接字串設定在 `appsettings.json` 或 `appsettings.Development.json` 中。

### 基本連接字串格式

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=伺服器位址;Database=資料庫名稱;User Id=使用者名稱;Password=密碼;TrustServerCertificate=True;"
  }
}
```

## 常見連接字串範例

### 1. 本地 SQL Server（Windows 驗證）

```json
"DefaultConnection": "Server=localhost;Database=testdb;Integrated Security=True;TrustServerCertificate=True;"
```

### 2. 本地 SQL Server（SQL Server 驗證）

```json
"DefaultConnection": "Server=localhost;Database=testdb;User Id=sa;Password=your_password;TrustServerCertificate=True;"
```

### 3. 指定連接埠

```json
"DefaultConnection": "Server=localhost,1433;Database=testdb;User Id=sa;Password=your_password;TrustServerCertificate=True;"
```

### 4. 遠端 SQL Server

```json
"DefaultConnection": "Server=192.168.1.100,1433;Database=testdb;User Id=sa;Password=your_password;TrustServerCertificate=True;"
```

### 5. 使用命名實例

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=testdb;User Id=sa;Password=your_password;TrustServerCertificate=True;"
```

### 6. 包含額外選項的完整連接字串

```json
"DefaultConnection": "Server=localhost;Database=testdb;User Id=sa;Password=your_password;TrustServerCertificate=True;Encrypt=True;Connection Timeout=30;"
```

## 連接字串參數說明

| 參數 | 說明 | 範例 |
|------|------|------|
| `Server` | SQL Server 位址 | `localhost`, `192.168.1.100`, `server.database.windows.net` |
| `Database` | 資料庫名稱 | `testdb`, `Picture2Text` |
| `User Id` | 使用者名稱 | `sa`, `admin` |
| `Password` | 密碼 | `your_password` |
| `Integrated Security` | 使用 Windows 驗證 | `True` |
| `TrustServerCertificate` | 信任伺服器憑證（開發環境） | `True` |
| `Encrypt` | 是否加密連接 | `True`, `False` |
| `Connection Timeout` | 連接逾時時間（秒） | `30` |
| `MultipleActiveResultSets` | 允許多個活動結果集 | `True` |

## 設定步驟

### 步驟 1: 修改 appsettings.json

編輯 `appsettings.json` 檔案，修改 `ConnectionStrings:DefaultConnection`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=你的伺服器位址;Database=你的資料庫名稱;User Id=你的使用者名稱;Password=你的密碼;TrustServerCertificate=True;"
  }
}
```

### 步驟 2: 自動創建資料庫和資料表

**重要**：應用程式會在啟動時自動檢查並創建資料庫和資料表（如果不存在）。

應用程式使用 `Database.EnsureCreated()` 方法，會在首次啟動時：
- 自動創建資料庫（如果不存在）
- 自動創建所有定義的資料表（如果不存在）
- 根據 `ApplicationDbContext` 中的模型配置創建表結構

**User 表的結構**（會自動創建）：

```sql
CREATE TABLE [User] (
    [ID] INT PRIMARY KEY IDENTITY(1,1),
    [ID_NO] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Password] NVARCHAR(255) NOT NULL
);

-- 建立索引
CREATE UNIQUE INDEX IX_User_ID_NO ON [User]([ID_NO]);
```

**注意事項**：
- `EnsureCreated()` 只會在資料庫或資料表不存在時創建
- 如果資料表已存在但結構不同，**不會自動更新**表結構
- 適合開發環境使用
- 生產環境建議使用 Migrations 進行資料庫版本管理

### 步驟 3: 測試連接

執行應用程式來測試連接：

```bash
dotnet run
```

如果連接成功，應用程式會正常啟動。如果連接失敗，會顯示錯誤訊息。

## 使用環境變數（推薦用於生產環境）

為了安全起見，生產環境建議使用環境變數或 User Secrets 來儲存連接字串。

### 使用 User Secrets（開發環境）

```bash
# 設定 User Secret
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=testdb;User Id=sa;Password=password;TrustServerCertificate=True;"
```

### 使用環境變數

在 Linux/macOS：
```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=testdb;User Id=sa;Password=password;TrustServerCertificate=True;"
```

在 Windows PowerShell：
```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=testdb;User Id=sa;Password=password;TrustServerCertificate=True;"
```

## 常見問題

### 1. 連接逾時

**問題**: `A network-related or instance-specific error occurred`

**解決方案**:
- 檢查 SQL Server 是否正在運行
- 檢查防火牆設定
- 增加 `Connection Timeout` 參數

### 2. 認證失敗

**問題**: `Login failed for user`

**解決方案**:
- 確認使用者名稱和密碼正確
- 確認 SQL Server 允許 SQL Server 驗證
- 檢查使用者是否有資料庫存取權限

### 3. 資料庫不存在

**問題**: `Cannot open database`

**解決方案**:
- 確認資料庫名稱正確
- **應用程式會自動創建資料庫**（如果 SQL Server 使用者有創建資料庫的權限）
- 如果自動創建失敗，可以手動創建資料庫：
  ```sql
  CREATE DATABASE [資料庫名稱];
  ```
- 檢查使用者是否有資料庫存取權限

### 4. 憑證驗證錯誤

**問題**: `The certificate chain was issued by an authority that is not trusted`

**解決方案**:
- 在開發環境添加 `TrustServerCertificate=True`
- 在生產環境使用正確的憑證

## 驗證連接

可以建立一個簡單的測試端點來驗證資料庫連接：

```csharp
[HttpGet("test-connection")]
public async Task<IActionResult> TestConnection()
{
    try
    {
        await _context.Database.CanConnectAsync();
        return Ok(new { message = "資料庫連接成功" });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = "資料庫連接失敗", error = ex.Message });
    }
}
```

## 自動創建資料庫說明

### 工作原理

應用程式在 `Program.cs` 中配置了自動創建機制：

```csharp
// 自動創建資料庫和資料表（如果不存在）
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}
```

### 使用 Migrations（生產環境推薦）

如果需要更完整的資料庫版本管理，可以使用 Entity Framework Migrations：

```bash
# 安裝 EF Core 工具（如果尚未安裝）
dotnet tool install --global dotnet-ef

# 建立 Migration
dotnet ef migrations add InitialCreate

# 套用 Migration（自動更新資料庫）
dotnet ef database update
```

或者在程式碼中使用 `Database.Migrate()` 來自動套用 Migrations：

```csharp
context.Database.Migrate(); // 替代 EnsureCreated()
```

**Migrations 的優點**：
- 可以追蹤資料庫結構變更歷史
- 可以回滾到之前的版本
- 適合團隊協作和生產環境
- 可以生成 SQL 腳本

## 下一步

連接設定完成後，您可以：
1. 執行應用程式測試連接（資料庫和資料表會自動創建）
2. 使用 Swagger UI 測試 API 端點
3. 確認可以正常查詢和操作資料庫
