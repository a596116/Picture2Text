# 🚪 API Gateway 統一驗證整合指南

## 📖 架構說明

使用 API Gateway (Nginx/IIS) 作為統一入口，在 Gateway 層面驗證 Token，後端服務完全不需要處理認證邏輯。

```
使用者 → API Gateway (驗證 Token) → 後端服務 (純業務邏輯)
```

## 🏗️ 完整架構圖

```
┌──────────────┐
│   使用者      │
└──────┬───────┘
       │ 1. 登入
       ▼
┌─────────────────────────────┐
│     認證中心 (Auth Center)   │
│     http://auth:5000        │
│                             │
│  POST /api/auth/login       │
│  POST /api/auth/validate    │
│  POST /api/auth/refresh     │
└─────────────────────────────┘
       ▲
       │ 2. Gateway 驗證 Token
       │
┌──────┴───────────────────────────────────────────┐
│              Nginx / IIS (API Gateway)           │
│                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────┴───┐
│  │ HR Gateway   │  │財務 Gateway   │  │庫存 Gateway  │
│  │ :80/hr/*     │  │:80/finance/*  │  │:80/inv/*    │
│  └──────┬───────┘  └──────┬───────┘  └──────┬──────┘
└─────────┼──────────────────┼──────────────────┼─────┘
          │                  │                  │
          │ 3. 傳遞使用者資訊 │                  │
          │ X-User-Id        │                  │
          │ X-User-Name      │                  │
          ▼                  ▼                  ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  HR 後端 API    │  │  財務後端 API    │  │  庫存後端 API    │
│  localhost:3001 │  │  localhost:3002 │  │  localhost:3003 │
│                 │  │                 │  │                 │
│  不需驗證 Token  │  │  不需驗證 Token  │  │  不需驗證 Token  │
│  信任 Header    │  │  信任 Header    │  │  信任 Header    │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

## 🔄 完整流程

### 1️⃣ 使用者登入

```
使用者               認證中心
  │                     │
  │  POST /api/auth/login
  │  {userId, password} │
  │────────────────────▶│
  │                     │
  │  {accessToken,      │
  │   refreshToken}     │
  │◀────────────────────│
  │                     │
  │ 存儲 Token          │
  │                     │
```

### 2️⃣ 訪問 HR 系統 API（透過 Gateway）

```
使用者          Nginx Gateway       認證中心        HR 後端
  │                  │                  │              │
  │ GET /hr/employees │                  │              │
  │ Bearer token      │                  │              │
  │─────────────────▶│                  │              │
  │                  │                  │              │
  │                  │ 2. 驗證 Token    │              │
  │                  │ /api/auth/validate              │
  │                  │─────────────────▶│              │
  │                  │                  │              │
  │                  │ 3. 有效 ✓        │              │
  │                  │    userId=1      │              │
  │                  │◀─────────────────│              │
  │                  │                  │              │
  │                  │ 4. 轉發請求 (加 Header)         │
  │                  │    GET /employees               │
  │                  │    X-User-Id: 1                 │
  │                  │    X-User-Name: xxx             │
  │                  │────────────────────────────────▶│
  │                  │                  │              │
  │                  │                  │  5. 執行業務邏輯
  │                  │                  │     (信任 Header)
  │                  │                  │              │
  │                  │ 6. 返回結果                      │
  │                  │◀────────────────────────────────│
  │                  │                  │              │
  │ 7. 返回給使用者  │                  │              │
  │◀─────────────────│                  │              │
  │                  │                  │              │
```

## 🔧 Nginx 配置

### 方案 1：使用 auth_request 模組（推薦）

#### nginx.conf 主配置

```nginx
# 認證服務配置（內部使用）
upstream auth_service {
    server localhost:5000;  # 認證中心地址
}

# HR 系統後端
upstream hr_backend {
    server localhost:3001;
}

# 財務系統後端
upstream finance_backend {
    server localhost:3002;
}

# 庫存系統後端
upstream inventory_backend {
    server localhost:3003;
}

# 認證驗證端點（內部使用）
server {
    listen 8080;
    server_name localhost;
    
    location = /auth/validate {
        internal;  # 只能內部調用
        proxy_pass http://auth_service/api/auth/validate;
        proxy_pass_request_body off;
        proxy_set_header Content-Length "";
        proxy_set_header X-Original-URI $request_uri;
        
        # 傳遞 Authorization header
        proxy_set_header Authorization $http_authorization;
        proxy_set_header Content-Type "application/json";
        
        # 構造請求體
        proxy_set_body '{"token":"$http_authorization"}';
    }
}

# HR 系統 Gateway
server {
    listen 80;
    server_name hr.yourcompany.com;
    
    location /hr/ {
        # 1. 先驗證 Token
        auth_request /auth/validate;
        
        # 2. 從認證響應中提取使用者資訊
        auth_request_set $user_id $upstream_http_x_user_id;
        auth_request_set $user_name $upstream_http_x_user_name;
        auth_request_set $user_idno $upstream_http_x_user_idno;
        
        # 3. 驗證失敗時返回 401
        error_page 401 = @error401;
        
        # 4. 轉發到後端，並附加使用者資訊
        proxy_pass http://hr_backend/;
        proxy_set_header X-User-Id $user_id;
        proxy_set_header X-User-Name $user_name;
        proxy_set_header X-User-IdNo $user_idno;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header Host $host;
        
        # 移除 Authorization header（後端不需要）
        proxy_set_header Authorization "";
    }
    
    location @error401 {
        return 401 '{"code":401,"message":"未授權，請重新登入"}';
        add_header Content-Type application/json;
    }
}

# 財務系統 Gateway（配置類似）
server {
    listen 80;
    server_name finance.yourcompany.com;
    
    location /finance/ {
        auth_request /auth/validate;
        auth_request_set $user_id $upstream_http_x_user_id;
        auth_request_set $user_name $upstream_http_x_user_name;
        auth_request_set $user_idno $upstream_http_x_user_idno;
        
        error_page 401 = @error401;
        
        proxy_pass http://finance_backend/;
        proxy_set_header X-User-Id $user_id;
        proxy_set_header X-User-Name $user_name;
        proxy_set_header X-User-IdNo $user_idno;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header Authorization "";
    }
    
    location @error401 {
        return 401 '{"code":401,"message":"未授權，請重新登入"}';
        add_header Content-Type application/json;
    }
}

# 庫存系統 Gateway（配置類似）
server {
    listen 80;
    server_name inventory.yourcompany.com;
    
    location /inventory/ {
        auth_request /auth/validate;
        auth_request_set $user_id $upstream_http_x_user_id;
        auth_request_set $user_name $upstream_http_x_user_name;
        auth_request_set $user_idno $upstream_http_x_user_idno;
        
        error_page 401 = @error401;
        
        proxy_pass http://inventory_backend/;
        proxy_set_header X-User-Id $user_id;
        proxy_set_header X-User-Name $user_name;
        proxy_set_header X-User-IdNo $user_idno;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header Authorization "";
    }
    
    location @error401 {
        return 401 '{"code":401,"message":"未授權，請重新登入"}';
        add_header Content-Type application/json;
    }
}
```

### 方案 2：使用 Lua 腳本（更靈活）

#### nginx.conf with Lua

```nginx
http {
    # Lua 腳本路徑
    lua_package_path "/etc/nginx/lua/?.lua;;";
    
    # 共享字典用於緩存驗證結果（可選）
    lua_shared_dict token_cache 10m;
    
    upstream auth_service {
        server localhost:5000;
    }
    
    upstream hr_backend {
        server localhost:3001;
    }
    
    server {
        listen 80;
        server_name hr.yourcompany.com;
        
        location /hr/ {
            # 使用 Lua 腳本驗證
            access_by_lua_file /etc/nginx/lua/auth_validate.lua;
            
            # 轉發到後端
            proxy_pass http://hr_backend/;
            # X-User-* headers 已在 Lua 中設定
        }
    }
}
```

#### /etc/nginx/lua/auth_validate.lua

```lua
local http = require "resty.http"
local cjson = require "cjson"

-- 獲取 Authorization header
local auth_header = ngx.var.http_authorization
if not auth_header then
    ngx.status = ngx.HTTP_UNAUTHORIZED
    ngx.say(cjson.encode({code=401, message="缺少認證 Token"}))
    return ngx.exit(ngx.HTTP_UNAUTHORIZED)
end

-- 提取 token
local token = string.gsub(auth_header, "Bearer ", "")

-- 檢查緩存（可選，提升性能）
local token_cache = ngx.shared.token_cache
local cached_user = token_cache:get(token)
if cached_user then
    local user = cjson.decode(cached_user)
    ngx.req.set_header("X-User-Id", user.userId)
    ngx.req.set_header("X-User-Name", user.name)
    ngx.req.set_header("X-User-IdNo", user.idNo)
    return  -- 緩存命中，直接通過
end

-- 調用認證中心驗證
local httpc = http.new()
local res, err = httpc:request_uri("http://localhost:5000/api/auth/validate", {
    method = "POST",
    body = cjson.encode({token = token}),
    headers = {
        ["Content-Type"] = "application/json",
    },
    keepalive_timeout = 60,
    keepalive_pool = 10
})

if not res then
    ngx.log(ngx.ERR, "驗證請求失敗: ", err)
    ngx.status = ngx.HTTP_UNAUTHORIZED
    ngx.say(cjson.encode({code=401, message="認證服務不可用"}))
    return ngx.exit(ngx.HTTP_UNAUTHORIZED)
end

local response = cjson.decode(res.body)

if response.code == 200 and response.data.isValid then
    -- Token 有效，設定使用者資訊到 header
    local user_data = response.data
    ngx.req.set_header("X-User-Id", tostring(user_data.userId))
    ngx.req.set_header("X-User-Name", user_data.name or "")
    ngx.req.set_header("X-User-IdNo", user_data.idNo or "")
    
    -- 移除 Authorization header（後端不需要）
    ngx.req.set_header("Authorization", "")
    
    -- 緩存驗證結果（5分鐘）
    local cache_data = cjson.encode({
        userId = user_data.userId,
        name = user_data.name,
        idNo = user_data.idNo
    })
    token_cache:set(token, cache_data, 300)  -- 5分鐘緩存
    
else
    -- Token 無效
    ngx.status = ngx.HTTP_UNAUTHORIZED
    ngx.say(cjson.encode({code=401, message="Token 無效或已過期"}))
    return ngx.exit(ngx.HTTP_UNAUTHORIZED)
end
```

## 🪟 IIS 配置

### 方案 1：使用 URL Rewrite + ARR（推薦）

#### 1. 安裝必要模組
```powershell
# 需要安裝
- Application Request Routing (ARR)
- URL Rewrite Module
```

#### 2. web.config 配置

```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <system.webServer>
        <rewrite>
            <rules>
                <!-- HR 系統規則 -->
                <rule name="HR API Gateway" stopProcessing="true">
                    <match url="^hr/(.*)" />
                    <conditions>
                        <!-- 1. 先驗證 Token -->
                        <add input="{HTTP_AUTHORIZATION}" pattern="^Bearer (.+)$" />
                    </conditions>
                    <action type="Rewrite" url="http://localhost:3001/{R:1}" />
                    <serverVariables>
                        <!-- 添加自定義驗證 header -->
                        <set name="HTTP_X_VALIDATE_TOKEN" value="{C:1}" />
                    </serverVariables>
                </rule>
                
                <!-- 財務系統規則 -->
                <rule name="Finance API Gateway" stopProcessing="true">
                    <match url="^finance/(.*)" />
                    <conditions>
                        <add input="{HTTP_AUTHORIZATION}" pattern="^Bearer (.+)$" />
                    </conditions>
                    <action type="Rewrite" url="http://localhost:3002/{R:1}" />
                    <serverVariables>
                        <set name="HTTP_X_VALIDATE_TOKEN" value="{C:1}" />
                    </serverVariables>
                </rule>
            </rules>
        </rewrite>
        
        <!-- 自定義驗證模組 -->
        <modules>
            <add name="TokenValidationModule" type="YourCompany.TokenValidationModule" />
        </modules>
    </system.webServer>
</configuration>
```

### 方案 2：使用 YARP (反向代理)

#### Program.cs

```csharp
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// 添加 YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        // 在每個請求前驗證 Token
        builderContext.AddRequestTransform(async transformContext =>
        {
            var httpContext = transformContext.HttpContext;
            var token = httpContext.Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    code = 401,
                    message = "缺少認證 Token"
                });
                return;
            }

            // 調用認證中心驗證
            using var httpClient = new HttpClient();
            var validateResponse = await httpClient.PostAsJsonAsync(
                "http://localhost:5000/api/auth/validate",
                new { token }
            );

            if (!validateResponse.IsSuccessStatusCode)
            {
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    code = 401,
                    message = "Token 驗證失敗"
                });
                return;
            }

            var result = await validateResponse.Content
                .ReadFromJsonAsync<ValidateTokenResponse>();

            if (result?.Data?.IsValid == true)
            {
                // 添加使用者資訊到 header
                httpContext.Request.Headers.Add("X-User-Id", 
                    result.Data.UserId.ToString());
                httpContext.Request.Headers.Add("X-User-Name", 
                    result.Data.Name ?? "");
                httpContext.Request.Headers.Add("X-User-IdNo", 
                    result.Data.IdNo ?? "");
                
                // 移除 Authorization header
                httpContext.Request.Headers.Remove("Authorization");
            }
            else
            {
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    code = 401,
                    message = "Token 無效或已過期"
                });
            }
        });
    });

var app = builder.Build();

app.MapReverseProxy();

app.Run();
```

#### appsettings.json (YARP 配置)

```json
{
  "ReverseProxy": {
    "Routes": {
      "hr-route": {
        "ClusterId": "hr-cluster",
        "Match": {
          "Path": "/hr/{**catch-all}"
        }
      },
      "finance-route": {
        "ClusterId": "finance-cluster",
        "Match": {
          "Path": "/finance/{**catch-all}"
        }
      },
      "inventory-route": {
        "ClusterId": "inventory-cluster",
        "Match": {
          "Path": "/inventory/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "hr-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:3001/"
          }
        }
      },
      "finance-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:3002/"
          }
        }
      },
      "inventory-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:3003/"
          }
        }
      }
    }
  }
}
```

## 🔧 認證中心需要的調整

為了讓 Gateway 能從驗證響應中提取使用者資訊，需要調整認證中心的響應 Header：

### 修改 AuthController.cs

```csharp
[HttpPost("validate")]
public async Task<ActionResult<ValidateTokenResponse>> ValidateToken([FromBody] ValidateTokenRequest request)
{
    var response = await _authService.ValidateTokenAsync(request);

    if (response.Code == 200 && response.Data?.IsValid == true)
    {
        // ✨ 添加使用者資訊到 Response Header（供 Nginx 使用）
        Response.Headers.Add("X-User-Id", response.Data.UserId.ToString());
        Response.Headers.Add("X-User-Name", response.Data.Name ?? "");
        Response.Headers.Add("X-User-IdNo", response.Data.IdNo ?? "");
    }

    if (response.Code != 200)
    {
        return Unauthorized(response);
    }

    return Ok(response);
}
```

## 💻 後端服務的調整（變得更簡單！）

### Node.js/Express (HR 系統後端)

```javascript
const express = require('express');
const app = express();

// ✨ 不需要驗證 Token！直接信任 Gateway 傳來的 Header

// 從 Header 提取使用者資訊的中介軟體
function extractUser(req, res, next) {
    req.user = {
        userId: parseInt(req.headers['x-user-id']),
        name: req.headers['x-user-name'],
        idNo: req.headers['x-user-idno']
    };
    
    // 安全檢查：確保來自信任的 Gateway
    const trustedIPs = ['127.0.0.1', 'gateway-ip'];
    if (!trustedIPs.includes(req.ip)) {
        return res.status(403).json({ message: '禁止直接訪問' });
    }
    
    next();
}

// 所有 API 都使用這個中介軟體
app.use(extractUser);

app.get('/api/employees', async (req, res) => {
    // req.user 已經有使用者資訊了！
    console.log('當前使用者:', req.user);
    
    const employees = await db.query('SELECT * FROM employees');
    res.json(employees);
});

app.listen(3001, () => {
    console.log('HR 後端運行在 http://localhost:3001');
});
```

### Python/Flask (財務系統後端)

```python
from flask import Flask, request, jsonify
from functools import wraps

app = Flask(__name__)

TRUSTED_IPS = ['127.0.0.1', 'gateway-ip']

def extract_user(f):
    """從 Header 提取使用者資訊"""
    @wraps(f)
    def decorated_function(*args, **kwargs):
        # 安全檢查
        if request.remote_addr not in TRUSTED_IPS:
            return jsonify({'message': '禁止直接訪問'}), 403
        
        # 提取使用者資訊
        request.user = {
            'userId': int(request.headers.get('X-User-Id', 0)),
            'name': request.headers.get('X-User-Name', ''),
            'idNo': request.headers.get('X-User-IdNo', '')
        }
        
        return f(*args, **kwargs)
    
    return decorated_function

@app.route('/api/invoices', methods=['GET'])
@extract_user
def get_invoices():
    # request.user 已經有使用者資訊了！
    print(f'當前使用者: {request.user}')
    
    invoices = db.query('SELECT * FROM invoices')
    return jsonify(invoices)

if __name__ == '__main__':
    app.run(port=3002)
```

### C# ASP.NET Core (庫存系統後端)

```csharp
// Middleware/UserExtractionMiddleware.cs
public class UserExtractionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _trustedIPs = new[] { "127.0.0.1", "gateway-ip" };

    public UserExtractionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 安全檢查
        var remoteIP = context.Connection.RemoteIpAddress?.ToString();
        if (!_trustedIPs.Contains(remoteIP))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { Message = "禁止直接訪問" });
            return;
        }

        // 提取使用者資訊
        if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdValue))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userIdValue.ToString()),
                new Claim(ClaimTypes.Name, context.Request.Headers["X-User-Name"].ToString()),
                new Claim("IdNo", context.Request.Headers["X-User-IdNo"].ToString())
            };

            var identity = new ClaimsIdentity(claims, "Gateway");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}

// Program.cs
app.UseMiddleware<UserExtractionMiddleware>();

// Controller
[HttpGet]
public IActionResult GetInventory()
{
    // User 已經有資訊了！
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var name = User.FindFirst(ClaimTypes.Name)?.Value;
    
    return Ok(inventory);
}
```

## 🎯 性能優化建議

### 1. Token 緩存（Redis）

```lua
-- 在 Lua 腳本中使用 Redis 緩存
local redis = require "resty.redis"
local red = redis:new()
red:connect("127.0.0.1", 6379)

-- 檢查緩存
local cached = red:get("token:" .. token)
if cached ~= ngx.null then
    -- 緩存命中
    local user = cjson.decode(cached)
    -- 設定 headers...
    return
end

-- 驗證後存入緩存
red:setex("token:" .. token, 300, cjson.encode(user_data))
```

### 2. 連接池

```nginx
upstream auth_service {
    server localhost:5000;
    keepalive 32;  # 連接池大小
}
```

### 3. 速率限制

```nginx
http {
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    
    server {
        location /hr/ {
            limit_req zone=api_limit burst=20;
            # ...
        }
    }
}
```

## 📊 監控和日誌

### Nginx 訪問日誌

```nginx
log_format api_gateway '$remote_addr - $remote_user [$time_local] '
                      '"$request" $status $body_bytes_sent '
                      '"$http_referer" "$http_user_agent" '
                      'user_id:$http_x_user_id '
                      'upstream:$upstream_addr '
                      'upstream_time:$upstream_response_time';

access_log /var/log/nginx/api_gateway.log api_gateway;
```

## 🎯 總結

### ✅ 使用 API Gateway 的優勢

1. **性能更好** - 驗證只在入口做一次，不需要每個後端都調用
2. **後端更簡單** - 後端服務完全不用管認證，只需信任 Header
3. **集中管理** - 所有認證邏輯在 Gateway 層
4. **易於擴展** - 新增系統只需添加 Gateway 配置
5. **安全性高** - 內網服務之間是受信任網絡

### 📝 實施步驟

1. ✅ 部署認證中心
2. ✅ 配置 Nginx/IIS Gateway
3. ✅ 調整後端服務（移除認證邏輯，改為提取 Header）
4. ✅ 測試驗證流程
5. ✅ 添加監控和日誌

---

**推薦方案**：Nginx + auth_request（簡單） 或 Nginx + Lua（靈活）
