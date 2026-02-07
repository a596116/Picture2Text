using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Picture2Text.Api.Data;
using Picture2Text.Api.Services;
using Serilog;

// 配置 Serilog（在建立 builder 之前）
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("==========================================");
    Log.Information("正在啟動 Picture2Text API...");
    Log.Information("==========================================");

    var builder = WebApplication.CreateBuilder(args);

    // 使用 Serilog 作為日誌提供者
    builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Picture2Text.Api.Filters.ValidationErrorFilter>();
});

// 禁用 ASP.NET Core 的自動模型驗證回應，改用自訂的過濾器
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// 配置 Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 配置 JWT 認證（支援多套系統）
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey 未設定");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AuthCenter.Api";
var jwtAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<List<string>>() 
    ?? new List<string> { "AllSystems" };

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudiences = jwtAudiences,  // 支援多個系統的 Audience
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 註冊 HttpContextAccessor（用於取得 HTTP 請求資訊）
builder.Services.AddHttpContextAccessor();

// 註冊服務
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<LoginHistoryService>();
builder.Services.AddScoped<AuthService>();

// 註冊背景服務
builder.Services.AddHostedService<TokenCleanupService>();

// 配置 CORS（針對多套系統）
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCompanySystems", policy =>
    {
        policy.WithOrigins(allowedOrigins)  // 只允許公司內部系統
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // 支援 Cookie（用於 Refresh Token）
    });
    
    // 開發環境可以保留 AllowAll
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 配置 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "認證中心 API",
        Description = "微服務認證中心 - 提供完整的 JWT 認證、Refresh Token、會話管理功能",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });

    // 包含 XML 註解
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // 添加 OAuth2 Password Flow 認證配置
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Password = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri("/api/auth/login", UriKind.Relative),
                Scopes = new Dictionary<string, string>()
            }
        },
        Description = "使用 OAuth2 Password Flow 進行認證"
    });

    // 也保留 Bearer Token 方式（手動輸入 token）
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT 授權標頭使用 Bearer 方案。範例：\"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// 啟用 Swagger（在所有環境中）- 必須在其他中間件之前
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Picture2Text API v1");
    options.RoutePrefix = "swagger"; // Swagger UI 路徑：/swagger
    options.DocumentTitle = "Picture2Text API 文檔";
    options.DefaultModelsExpandDepth(-1); // 隱藏模型預設展開
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List); // 預設摺疊所有端點
    
    // 配置 OAuth2
    options.OAuthClientId("swagger-ui");
    options.OAuthAppName("Picture2Text API - Swagger");
    options.OAuthUsePkce();
});

// 只在開發環境啟用 HTTPS 重定向
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 生產環境使用限制的 CORS，開發環境使用 AllowAll
if (app.Environment.IsProduction())
{
    app.UseCors("AllowCompanySystems");
}
else
{
    app.UseCors("AllowAll");
}

app.UseAuthentication();
app.UseAuthorization();

// 自動創建資料庫和資料表（如果不存在）
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     try
//     {
//         var context = services.GetRequiredService<ApplicationDbContext>();
//         context.Database.EnsureCreated();
//         Log.Information("資料庫連接成功，資料表已就緒");
//     }
//     catch (Exception ex)
//     {
//         Log.Error(ex, "資料庫初始化時發生錯誤");
//     }
// }

app.MapControllers();

    // 輸出 Swagger 資訊到控制台
    var environment = app.Environment;
    var swaggerUrl = environment.IsDevelopment() 
        ? "http://localhost:5000/swagger" 
        : $"http://localhost:5000/swagger";

    Log.Information("==========================================");
    Log.Information("Picture2Text API 已啟動成功！");
    Log.Information("==========================================");
    Log.Information("環境: {Environment}", environment.EnvironmentName);
    Log.Information("Swagger UI: {SwaggerUrl}", swaggerUrl);
    Log.Information("日誌檔案位置: {LogPath}", Path.Combine(Directory.GetCurrentDirectory(), "Logs"));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "應用程式啟動失敗");
    throw;
}
finally
{
    Log.Information("應用程式正在關閉...");
    Log.CloseAndFlush();
}
