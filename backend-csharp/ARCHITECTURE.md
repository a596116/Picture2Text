# 認證中心架構設計

## 🏗️ API Gateway 架構（推薦）

```
┌──────────────┐
│   使用者      │
└──────┬───────┘
       │ 1. 登入（直接打認證中心）
       ▼
┌─────────────────────────────┐
│    🔐 認證中心 (Auth Center) │
│     Port: 5000              │
│  • 登入/登出                 │
│  • Token 驗證（供 Gateway）  │
│  • 會話管理                  │
└─────────────────────────────┘
       ▲
       │ 2. Gateway 驗證 Token
       │
┌──────┴──────────────────────────────────┐
│    Nginx / IIS (API Gateway)            │
│  ┌──────────┐  ┌──────────┐  ┌────────┐│
│  │HR Gateway│  │財務Gateway│ │庫存Gate││
│  │驗證 Token│  │驗證 Token│  │驗證Token││
│  └────┬─────┘  └────┬─────┘  └───┬────┘│
└───────┼─────────────┼─────────────┼─────┘
        │             │             │
        │ 附帶使用者資訊（X-User-* Headers）
        ▼             ▼             ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ HR 後端 API  │ │ 財務後端 API  │ │ 庫存後端 API  │
│ (Pure Logic) │ │ (Pure Logic) │ │ (Pure Logic) │
│ 不驗證 Token │ │ 不驗證 Token │ │ 不驗證 Token │
│ 信任 Headers │ │ 信任 Headers │ │ 信任 Headers │
└──────────────┘ └──────────────┘ └──────────────┘
```

## 🎯 核心組件

### 認證中心 API
```
Controllers/
├── AuthController      # 登入、驗證、登出
├── SessionController   # 會話管理
└── ProfileController   # 使用者資料

Services/
├── AuthService         # 認證業務邏輯
├── JwtService          # JWT 生成和驗證
├── RefreshTokenService # Refresh Token 管理
├── SessionService      # 會話管理
└── LoginHistoryService # 登入歷史
```

## 📦 專案結構

```
backend-csharp/
├── Controllers/              # API 控制器
│   ├── AuthController.cs     # 認證端點（登入、刷新、撤銷、驗證）
│   ├── SessionController.cs  # 會話管理端點
│   └── ProfileController.cs  # 使用者資料端點
│
├── Services/                 # 業務邏輯層
│   ├── AuthService.cs        # 認證服務（主要業務邏輯）
│   ├── JwtService.cs         # JWT Token 生成和驗證
│   ├── RefreshTokenService.cs # Refresh Token 管理
│   ├── SessionService.cs     # 會話管理
│   ├── LoginHistoryService.cs # 登入歷史記錄
│   └── TokenCleanupService.cs # 背景清理服務
│
├── Models/                   # 資料模型
│   ├── User.cs              # 使用者模型
│   ├── RefreshToken.cs      # Refresh Token 模型
│   ├── UserSession.cs       # 會話模型
│   └── LoginHistory.cs      # 登入歷史模型
│
├── DTOs/                     # 資料傳輸物件
│   ├── Requests/            # 請求 DTO
│   │   ├── LoginRequest.cs
│   │   ├── RefreshTokenRequest.cs
│   │   ├── RevokeTokenRequest.cs
│   │   └── ValidateTokenRequest.cs
│   └── Responses/           # 回應 DTO
│       ├── ApiResponse.cs
│       ├── TokenResponse.cs
│       ├── ValidateTokenResponse.cs
│       ├── SessionResponse.cs
│       └── LoginHistoryResponse.cs
│
├── Data/                     # 資料存取層
│   └── ApplicationDbContext.cs
│
├── Filters/                  # 過濾器
│   └── ValidationErrorFilter.cs
│
└── Documentation/            # 文檔
    ├── AUTH_CENTER_GUIDE.md
    ├── ARCHITECTURE.md
    └── MIGRATION_COMMANDS.md
```

## 🔄 認證流程

### 1. 使用者登入流程

```
客戶端                     認證中心                    資料庫
  │                          │                          │
  │──POST /api/auth/login───▶│                          │
  │  {userId, password}      │                          │
  │                          │──查詢使用者────────────────▶│
  │                          │◀─────────────────────────│
  │                          │──驗證密碼                  │
  │                          │──檢查暴力破解              │
  │                          │──生成 Access Token        │
  │                          │──生成 Refresh Token───────▶│
  │                          │──創建會話──────────────────▶│
  │                          │──記錄登入歷史──────────────▶│
  │◀─────────────────────────│                          │
  │  {accessToken,           │                          │
  │   refreshToken,          │                          │
  │   expiresAt}             │                          │
  │                          │                          │
```

### 2. Access Token 過期後刷新流程

```
客戶端                     認證中心                    資料庫
  │                          │                          │
  │─POST /api/auth/refresh──▶│                          │
  │  {refreshToken}          │                          │
  │                          │──驗證 Refresh Token───────▶│
  │                          │◀─────────────────────────│
  │                          │──生成新 Access Token      │
  │                          │──生成新 Refresh Token─────▶│
  │                          │──撤銷舊 Refresh Token─────▶│
  │                          │──更新會話──────────────────▶│
  │◀─────────────────────────│                          │
  │  {accessToken,           │                          │
  │   refreshToken}          │                          │
  │                          │                          │
```

### 3. 其他微服務驗證 Token 流程

```
微服務A                    認證中心                    資料庫
  │                          │                          │
  │─POST /api/auth/validate─▶│                          │
  │  {token}                 │                          │
  │                          │──驗證 JWT 簽名            │
  │                          │──檢查過期時間              │
  │                          │──查詢使用者資訊────────────▶│
  │                          │◀─────────────────────────│
  │◀─────────────────────────│                          │
  │  {isValid, userId,       │                          │
  │   idNo, name}            │                          │
  │                          │                          │
```

## 🔐 安全機制

### 1. Token 雙層設計

| Token 類型 | 有效期 | 用途 | 存儲位置 |
|-----------|--------|------|---------|
| Access Token | 30 分鐘 | API 訪問 | 記憶體（不建議存儲） |
| Refresh Token | 7 天 | 刷新 Access Token | HttpOnly Cookie 或安全存儲 |

### 2. Refresh Token Rotation

每次使用 Refresh Token 刷新時：
1. 驗證舊的 Refresh Token
2. 生成新的 Access Token 和 Refresh Token
3. 撤銷舊的 Refresh Token（標記 `ReplacedByToken`）
4. 返回新的 Token 對

**優點**：即使 Refresh Token 被竊取，攻擊者只能使用一次，之後合法使用者刷新時會檢測到異常。

### 3. 防暴力破解

- 15 分鐘內失敗 5 次 → 暫時鎖定
- 記錄所有登入嘗試（成功和失敗）
- 可擴展：添加 CAPTCHA、郵件通知等

### 4. Token 加密存儲

Refresh Token 在資料庫中使用 SHA-256 加密存儲，即使資料庫洩露也無法直接使用。

### 5. 會話管理

- 追蹤每個登入的裝置、IP、User-Agent
- 支援多裝置登入
- 使用者可查看並手動結束可疑會話

### 6. 自動清理

背景服務每小時自動清理：
- 過期的 Refresh Token
- 過期的會話
- 90 天前的登入歷史

## 🌐 API Gateway 驗證流程

### 1. 使用者登入
```
使用者 → 認證中心 → 返回 Token（Access + Refresh）
```

### 2. API 調用流程
```
使用者
  ↓ Bearer Token
Gateway（驗證一次）
  ↓ X-User-Id, X-User-Name
後端服務（信任 Headers，純業務邏輯）
```

### 3. 優勢
- ✅ **性能極佳** - 驗證只在 Gateway 做一次
- ✅ **後端簡化** - 後端不需任何認證代碼
- ✅ **集中管理** - 認證邏輯在 Gateway
- ✅ **即時撤銷** - 登出後 Gateway 立即驗證失敗
- ✅ **易於擴展** - 新系統只需添加 Gateway 配置

## 📊 資料模型關係

```
User (使用者)
  │
  ├──▶ RefreshToken (1:N)
  │     ├── Token（加密）
  │     ├── ExpiresAt
  │     ├── IsRevoked
  │     └── DeviceInfo
  │
  ├──▶ UserSession (1:N)
  │     ├── SessionId
  │     ├── RefreshTokenId ──▶ RefreshToken
  │     ├── LoginAt
  │     ├── LastActivityAt
  │     └── IsActive
  │
  └──▶ LoginHistory (1:N)
        ├── IsSuccess
        ├── FailureReason
        ├── IpAddress
        └── AttemptedAt
```

## ⚡ 性能優化建議

### 1. 資料庫索引

已在 `ApplicationDbContext` 中配置：
- `RefreshToken.Token` 索引（快速查找）
- `RefreshToken.TokenId` 索引
- `UserSession.SessionId` 唯一索引
- 複合索引：`(UserId, IsRevoked)`、`(UserId, IsActive)` 等

### 2. 快取策略

可考慮添加 Redis 快取：
```csharp
// 快取使用者資訊（減少資料庫查詢）
public async Task<User?> GetUserByIdAsync(int userId)
{
    var cacheKey = $"user:{userId}";
    var cachedUser = await _cache.GetAsync<User>(cacheKey);
    
    if (cachedUser != null)
        return cachedUser;
    
    var user = await _context.Users.FindAsync(userId);
    await _cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(5));
    
    return user;
}
```

### 3. 連接池

Entity Framework Core 已自動管理連接池，確保 `DbContext` 使用 Scoped 生命週期。

### 4. 非同步處理

所有資料庫操作都已使用 `async/await`，避免阻塞執行緒。

## 🔄 擴展建議

### 短期（1-2 週）

1. **使用者註冊功能**
   - Email 驗證
   - 密碼強度檢查

2. **忘記密碼**
   - 郵件驗證碼
   - 重設密碼

3. **使用者資料管理**
   - 修改個人資料
   - 修改密碼

### 中期（1-2 個月）

4. **Two-Factor Authentication (2FA)**
   - TOTP（Google Authenticator）
   - SMS 驗證碼

5. **角色和權限管理（RBAC）**
   - 角色表
   - 權限表
   - JWT 中包含角色資訊

6. **OAuth2/OpenID Connect**
   - Google、Facebook 登入
   - 使用 IdentityServer

### 長期（3-6 個月）

7. **高可用性部署**
   - 多實例部署
   - 負載均衡
   - Redis 會話共享

8. **監控和告警**
   - Application Insights
   - Prometheus + Grafana
   - 異常登入告警

9. **合規性**
   - GDPR 合規（資料匯出、刪除）
   - 稽核日誌

10. **API Gateway 整合**
    - Kong、Ocelot 等
    - 統一認證入口

## 📈 監控指標

建議監控的關鍵指標：

1. **業務指標**
   - 登入成功率
   - Token 刷新頻率
   - 活躍使用者數
   - 平均會話時長

2. **性能指標**
   - API 回應時間（P50、P95、P99）
   - 資料庫查詢時間
   - Token 驗證時間

3. **安全指標**
   - 失敗登入次數
   - IP 黑名單觸發次數
   - 異常登入地點

4. **系統指標**
   - CPU 使用率
   - 記憶體使用率
   - 資料庫連接數

## 🎯 總結

這個認證中心提供了：
- ✅ 完整的 JWT + Refresh Token 機制
- ✅ 多裝置會話管理
- ✅ 安全審計和追蹤
- ✅ 微服務友好的設計
- ✅ 可擴展的架構

適用於中小型微服務系統，可根據實際需求進一步擴展。
