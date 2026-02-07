# Serilog 日誌系統使用說明

## 📦 已安裝套件

- **Serilog.AspNetCore** (v9.0.0) - 核心套件
- **Serilog.Sinks.File** (v6.0.0) - 檔案輸出
- **Serilog.Settings.Configuration** (v9.0.0) - 支援從 appsettings.json 讀取配置

## 📋 配置說明

### 日誌輸出位置

日誌檔案會自動儲存在：
```
backend-csharp/Logs/log-20260207.txt
backend-csharp/Logs/log-20260208.txt
...
```

### 主要參數說明（appsettings.json）

| 參數 | 說明 | 目前設定 |
|------|------|----------|
| `rollingInterval` | 日誌分割間隔 | `Day`（每天一個檔案） |
| `retainedFileCountLimit` | 保留檔案數量 | `30`（保留 30 天） |
| `fileSizeLimitBytes` | 單一檔案大小限制 | `10485760`（10 MB） |
| `rollOnFileSizeLimit` | 達到大小限制時分割 | `true` |
| `shared` | 允許多個進程寫入 | `true` |

### 日誌等級

| 等級 | 說明 | 使用時機 |
|------|------|----------|
| `Trace` | 最詳細的訊息 | 追蹤程式流程 |
| `Debug` | 除錯資訊 | 開發環境除錯 |
| `Information` | 一般資訊 | 重要操作記錄 |
| `Warning` | 警告訊息 | 潛在問題 |
| `Error` | 錯誤訊息 | 處理失敗 |
| `Fatal` | 嚴重錯誤 | 系統崩潰 |

### 環境別設定

- **Production**（正式環境）：`Information` 等級以上
- **Development**（開發環境）：`Debug` 等級以上

## 💻 使用範例

### 在 Controller 中使用

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        _logger.LogInformation("正在查詢用戶，ID: {UserId}", id);
        
        try
        {
            // 查詢用戶邏輯...
            _logger.LogDebug("用戶資料: {@UserData}", userData);
            return Ok(userData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢用戶失敗，ID: {UserId}", id);
            return StatusCode(500, "查詢失敗");
        }
    }
}
```

### 在 Service 中使用

```csharp
public class LoginHistoryService
{
    private readonly ILogger<LoginHistoryService> _logger;

    public LoginHistoryService(ILogger<LoginHistoryService> logger)
    {
        _logger = logger;
    }

    public async Task LogLoginAttempt(string username, string ipAddress, bool success)
    {
        if (success)
        {
            _logger.LogInformation("用戶登入成功 - 用戶名: {Username}, IP: {IpAddress}", 
                username, ipAddress);
        }
        else
        {
            _logger.LogWarning("用戶登入失敗 - 用戶名: {Username}, IP: {IpAddress}", 
                username, ipAddress);
        }
    }
}
```

### 結構化日誌（推薦）

使用 `{}` 佔位符可以產生結構化日誌，便於後續查詢：

```csharp
// ✅ 好的寫法（結構化）
_logger.LogInformation("用戶 {UserId} 從 {IpAddress} 登入", userId, ipAddress);

// ❌ 不推薦的寫法（字串拼接）
_logger.LogInformation($"用戶 {userId} 從 {ipAddress} 登入");
```

### 記錄物件資料

使用 `@` 符號可以序列化整個物件：

```csharp
var user = new { Id = 1, Name = "John", Email = "john@example.com" };
_logger.LogDebug("用戶資料: {@User}", user);

// 輸出：
// 用戶資料: {"Id": 1, "Name": "John", "Email": "john@example.com"}
```

## 🔧 進階配置

### 自訂日誌格式

在 `appsettings.json` 中的 `outputTemplate`：

```json
"outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}"
```

格式說明：
- `{Timestamp:yyyy-MM-dd HH:mm:ss.fff}` - 時間戳記
- `{Level:u3}` - 日誌等級（3個字元，大寫）
- `{SourceContext}` - 來源類別名稱
- `{Message:lj}` - 訊息（l = 字面值，j = JSON）
- `{Exception}` - 例外訊息

### 修改日誌保留天數

```json
"retainedFileCountLimit": 7  // 只保留 7 天
```

### 修改檔案大小限制

```json
"fileSizeLimitBytes": 52428800  // 50 MB = 50 * 1024 * 1024
```

### 改為每小時分割

```json
"rollingInterval": "Hour"  // 可選：Year, Month, Day, Hour, Minute
```

### 改為每月分割

```json
"path": "Logs/log-.txt",
"rollingInterval": "Month"
```

日誌檔案會變成：
```
Logs/log-202602.txt
Logs/log-202603.txt
```

## 🚀 安裝步驟（如果尚未安裝）

```bash
cd backend-csharp
dotnet restore
```

## 📂 查看日誌

### 方式 1：直接開啟檔案
```bash
# 查看今天的日誌
cat Logs/log-20260207.txt

# 即時監看（macOS/Linux）
tail -f Logs/log-20260207.txt
```

### 方式 2：使用 VS Code
直接在 VS Code 中開啟 `Logs/` 資料夾中的檔案。

### 方式 3：使用日誌查看工具
- [Seq](https://datalust.co/seq) - 強大的日誌查看與分析工具
- [Serilog Analyzer](https://github.com/serilog/serilog-analyzer)

## 🎯 最佳實踐

1. **使用結構化日誌**：使用 `{}` 佔位符而非字串拼接
2. **合適的日誌等級**：
   - `Information`：重要的業務操作（登入、訂單建立等）
   - `Warning`：可能的問題但不影響功能
   - `Error`：錯誤但系統仍可運行
   - `Fatal`：嚴重錯誤導致系統無法運行
3. **不要記錄敏感資訊**：密碼、信用卡號、個人隱私等
4. **適當的資訊量**：生產環境避免過度記錄，影響效能
5. **使用相關性 ID**：在分散式系統中追蹤請求

## 📊 效能考量

Serilog 是異步寫入，不會阻塞主執行緒，但仍需注意：

- 避免在迴圈中大量記錄 `Debug` 訊息
- 生產環境建議使用 `Information` 或以上等級
- 定期清理舊日誌（已設定 30 天自動清理）

## 🔗 相關資源

- [Serilog 官方文件](https://serilog.net/)
- [Serilog GitHub](https://github.com/serilog/serilog)
- [最佳實踐指南](https://github.com/serilog/serilog/wiki/Writing-Log-Events)
