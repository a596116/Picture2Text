# 🧪 測試 Serilog 配置

## 1️⃣ 安裝套件

```bash
cd backend-csharp
dotnet restore
```

## 2️⃣ 執行應用程式

```bash
dotnet run
```

## 3️⃣ 查看結果

### Console 輸出
啟動後，您應該會在 Console 看到類似這樣的日誌：

```
[2026-02-07 14:30:15 INF] ==========================================
[2026-02-07 14:30:15 INF] 正在啟動 Picture2Text API...
[2026-02-07 14:30:15 INF] ==========================================
[2026-02-07 14:30:16 INF] ==========================================
[2026-02-07 14:30:16 INF] Picture2Text API 已啟動成功！
[2026-02-07 14:30:16 INF] ==========================================
[2026-02-07 14:30:16 INF] 環境: Development
[2026-02-07 14:30:16 INF] Swagger UI: http://localhost:5000/swagger
[2026-02-07 14:30:16 INF] 日誌檔案位置: /Users/haodai/Code/Github/Picture2Text/backend-csharp/Logs
```

### 檔案輸出
檢查 `Logs/` 資料夾：

```bash
# 查看日誌資料夾
ls -lh Logs/

# 查看今天的日誌檔案
cat Logs/log-20260207.txt

# 即時監看日誌（macOS/Linux）
tail -f Logs/log-20260207.txt
```

## 4️⃣ 測試登入功能

### 測試成功登入
1. 開啟 Swagger：http://localhost:5000/swagger
2. 使用 `/api/auth/login` 端點進行登入
3. 查看 Console 和日誌檔案，應該會看到：

```
[2026-02-07 14:35:20 INF] 用戶登入成功 - UserId: 1, AttemptedUserId: "admin", IP: "::1"
[2026-02-07 14:35:20 DBG] 登入歷史已儲存 - LoginHistoryId: 123, Success: True
```

### 測試失敗登入
1. 故意輸入錯誤的密碼
2. 多次嘗試（超過 5 次）
3. 應該會看到警告日誌：

```
[2026-02-07 14:36:15 WRN] 用戶登入失敗 - AttemptedUserId: "admin", IP: "::1", Reason: "密碼錯誤"
[2026-02-07 14:36:30 ERR] ⚠️ 偵測到異常登入行為！AttemptedUserId: "admin" 在 15 分鐘內失敗 6 次
```

## 5️⃣ 檢查日誌格式

### 日誌檔案格式
開啟 `Logs/log-20260207.txt`，您會看到詳細的日誌格式：

```
[2026-02-07 14:30:15.123 +08:00 INF] Picture2Text.Api.Services.LoginHistoryService
用戶登入成功 - UserId: 1, AttemptedUserId: "admin", IP: "::1"
```

每行包含：
- `[時間戳記 等級]` - 完整的時間和日誌等級
- `來源類別` - 哪個 Service 或 Controller 產生的日誌
- `訊息` - 結構化的日誌訊息
- `例外訊息`（如果有錯誤）

## ✅ 預期結果

- ✅ Console 有彩色輸出
- ✅ `Logs/` 資料夾自動建立
- ✅ 每天一個日誌檔案（格式：`log-20260207.txt`）
- ✅ 日誌包含時間戳記、等級、來源、訊息
- ✅ 結構化日誌可以輕鬆搜尋和分析

## 📊 驗證日誌功能

### 搜尋特定用戶的日誌
```bash
# 搜尋特定 UserId
grep "UserId: 1" Logs/log-20260207.txt

# 搜尋所有登入失敗
grep "登入失敗" Logs/log-20260207.txt

# 搜尋特定 IP
grep "192.168.1.100" Logs/log-20260207.txt
```

### 查看錯誤日誌
```bash
# 只顯示 ERROR 和 FATAL
grep -E "\[ERR\]|\[FTL\]" Logs/log-20260207.txt
```

### 統計登入次數
```bash
# 統計今天登入成功次數
grep -c "登入成功" Logs/log-20260207.txt

# 統計登入失敗次數
grep -c "登入失敗" Logs/log-20260207.txt
```

## 🔧 常見問題

### Q1: 日誌檔案沒有建立？
確認 `Logs/` 資料夾權限，或手動建立：
```bash
mkdir -p backend-csharp/Logs
chmod 755 backend-csharp/Logs
```

### Q2: 日誌檔案太大？
修改 `appsettings.json` 中的 `fileSizeLimitBytes`：
```json
"fileSizeLimitBytes": 5242880  // 5 MB
```

### Q3: 想保留更多天數？
修改 `retainedFileCountLimit`：
```json
"retainedFileCountLimit": 90  // 保留 90 天
```

### Q4: 生產環境日誌太多？
在 `appsettings.json` 中調整日誌等級：
```json
"MinimumLevel": {
  "Default": "Warning"  // 只記錄 Warning 以上
}
```

## 📚 進階使用

詳細使用說明請參考：`SERILOG_使用說明.md`
