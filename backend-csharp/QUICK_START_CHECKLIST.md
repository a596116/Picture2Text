# ✅ 快速啟動檢查清單

## 📋 最小化部署步驟

### 1. 配置 appsettings.json
```bash
- [ ] 資料庫連接字串
- [ ] JWT SecretKey（至少 32 字元）
- [ ] Audiences（系統列表）
- [ ] CORS AllowedOrigins
```

### 2. 執行資料庫遷移
```bash
- [ ] dotnet tool install --global dotnet-ef
- [ ] dotnet ef migrations add AddAuthCenterTables
- [ ] dotnet ef database update
```

### 3. 啟動認證中心
```bash
- [ ] dotnet run
- [ ] 訪問 http://localhost:5000/swagger
```

### 4. 配置 API Gateway（Nginx/IIS）
```bash
- [ ] 複製 nginx.conf.example
- [ ] 修改上游地址
- [ ] 重新載入 Gateway
```

### 5. 測試
```bash
- [ ] 登入測試（POST /api/auth/login）
- [ ] Gateway 驗證測試
- [ ] 後端 API 調用測試
```

## 🧪 功能測試

### 1. 登入測試
- [ ] 使用 Swagger UI 測試 `POST /api/auth/login`
- [ ] 使用正確的帳號密碼
  ```json
  {
    "userId": 1,
    "password": "yourpassword"
  }
  ```
- [ ] 確認回應包含：
  - [ ] accessToken
  - [ ] refreshToken
  - [ ] expiresAt
  - [ ] refreshTokenExpiresAt
  - [ ] sessionId
- [ ] 複製 accessToken 用於後續測試

### 2. 認證端點測試
- [ ] 點擊 Swagger UI 右上角 "Authorize" 按鈕
- [ ] 輸入：`Bearer {accessToken}`
- [ ] 測試 `GET /api/auth/me`
- [ ] 確認返回當前使用者資訊

### 3. Token 刷新測試
- [ ] 測試 `POST /api/auth/refresh`
  ```json
  {
    "refreshToken": "從登入回應取得"
  }
  ```
- [ ] 確認獲得新的 accessToken 和 refreshToken
- [ ] 確認舊的 refreshToken 已失效（再次使用會失敗）

### 4. 會話管理測試
- [ ] 測試 `GET /api/session/active`
- [ ] 確認可以看到當前會話
- [ ] 確認會話資訊包含：
  - [ ] deviceName
  - [ ] ipAddress
  - [ ] loginAt
  - [ ] isCurrent: true

### 5. 登入歷史測試
- [ ] 測試 `GET /api/session/history`
- [ ] 確認可以看到登入記錄
- [ ] 確認包含成功和失敗的記錄

### 6. 登出測試
- [ ] 測試 `POST /api/auth/revoke`
  ```json
  {
    "refreshToken": "當前的 refreshToken",
    "revokeAllDevices": false
  }
  ```
- [ ] 確認登出成功
- [ ] 嘗試使用撤銷的 refreshToken 刷新（應該失敗）

### 7. Token 驗證測試（供微服務使用）
- [ ] 測試 `POST /api/auth/validate`
  ```json
  {
    "token": "有效的 accessToken"
  }
  ```
- [ ] 確認返回 isValid: true 和使用者資訊

### 8. 防暴力破解測試
- [ ] 使用錯誤密碼連續登入 5 次
- [ ] 確認第 6 次返回 429 錯誤（Too Many Requests）
- [ ] 等待 15 分鐘或清空 LoginHistory 表後恢復

## 🔒 安全檢查

### 生產環境部署前
- [ ] JWT SecretKey 已更換為強密鑰（至少 64 個隨機字元）
- [ ] 資料庫密碼已更換
- [ ] HTTPS 已啟用
- [ ] CORS 政策已根據實際需求配置
- [ ] 不在公開環境暴露 Swagger UI（或添加認證）
- [ ] 資料庫連接字串不包含在版本控制中
- [ ] 敏感配置使用環境變數或 Azure Key Vault

### 監控和日誌
- [ ] 已配置日誌記錄
- [ ] 已設定錯誤告警
- [ ] 已監控 API 回應時間
- [ ] 已監控資料庫性能

## 📊 資料庫檢查

### 確認資料正確寫入
登入一次後，檢查資料庫：

- [ ] User 表有測試使用者
  ```sql
  SELECT * FROM [User];
  ```

- [ ] RefreshToken 表有記錄
  ```sql
  SELECT * FROM RefreshToken WHERE UserID = 1;
  ```

- [ ] UserSession 表有記錄
  ```sql
  SELECT * FROM UserSession WHERE UserID = 1 AND IsActive = 1;
  ```

- [ ] LoginHistory 表有記錄
  ```sql
  SELECT * FROM LoginHistory WHERE UserID = 1 ORDER BY AttemptedAt DESC;
  ```

## 🌐 微服務整合檢查

### 在其他微服務中測試

- [ ] 已複製 JWT 配置到其他微服務
- [ ] 已實現 JWT 認證中介軟體
- [ ] 已測試受保護端點
- [ ] 已測試 Token 過期處理
- [ ] 已實現自動 Token 刷新（客戶端）

## 🔧 效能檢查

### 負載測試（可選但建議）
- [ ] 使用 JMeter 或類似工具進行負載測試
- [ ] 測試同時 100 個登入請求
- [ ] 測試同時 1000 個 Token 驗證請求
- [ ] 確認資料庫連接池設定合理
- [ ] 確認沒有記憶體洩漏

## 📝 文檔檢查

- [ ] 已閱讀 AUTH_CENTER_GUIDE.md
- [ ] 已閱讀 ARCHITECTURE.md
- [ ] 已閱讀 MICROSERVICE_INTEGRATION_EXAMPLE.md
- [ ] 已了解所有 API 端點的用途
- [ ] 已了解安全機制
- [ ] 團隊成員已了解如何使用

## 🎯 生產環境部署檢查

### 部署前
- [ ] 已在測試環境完整測試
- [ ] 已備份現有資料庫
- [ ] 已準備回滾計劃
- [ ] 已通知相關團隊
- [ ] 已安排維護時間窗口

### 部署中
- [ ] 執行資料庫遷移
- [ ] 部署新版本服務
- [ ] 驗證服務正常啟動
- [ ] 執行冒煙測試

### 部署後
- [ ] 監控錯誤日誌
- [ ] 監控 API 回應時間
- [ ] 監控資料庫性能
- [ ] 檢查背景服務是否正常運行
- [ ] 確認現有使用者可正常登入
- [ ] 確認新功能（Refresh Token）可正常使用

## ✅ 最終確認

- [ ] 所有測試通過
- [ ] 沒有編譯警告或錯誤
- [ ] 日誌輸出正常
- [ ] 資料正確寫入資料庫
- [ ] API 回應時間在可接受範圍內
- [ ] 團隊已培訓完成
- [ ] 文檔已更新

---

## 🚨 常見問題快速修復

### 問題 1: 資料庫連接失敗
```bash
# 檢查 SQL Server 是否運行
# 檢查防火牆設定
# 確認連接字串正確
# 測試連接：
sqlcmd -S localhost -U sa -P YourPassword
```

### 問題 2: JWT 驗證失敗
```csharp
// 確認所有微服務使用相同的：
// - SecretKey
// - Issuer
// - Audience
// - ClockSkew 設定
```

### 問題 3: Token 無法刷新
```csharp
// 檢查：
// 1. Refresh Token 是否已過期
// 2. Refresh Token 是否已被撤銷
// 3. 資料庫中是否有對應記錄
```

### 問題 4: 背景服務未啟動
```csharp
// 確認 Program.cs 中已註冊：
builder.Services.AddHostedService<TokenCleanupService>();
```

---

**祝部署順利！** 🎉

如有問題，請參考詳細文檔或檢查日誌輸出。
