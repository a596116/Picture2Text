using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Picture2Text.Api.Data;
using Picture2Text.Api.Services;

var builder = WebApplication.CreateBuilder(args);

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

// 配置 JWT 認證
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey 未設定");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Picture2Text.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Picture2Text.Client";

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
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 註冊服務
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();

// 配置 CORS
builder.Services.AddCors(options =>
{
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
        Title = "Picture2Text API",
        Description = "Picture2Text API 文檔 - 使用 JWT 認證的後端 API",
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

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// 取得 logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// 自動創建資料庫和資料表（如果不存在）
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     try
//     {
//         var context = services.GetRequiredService<ApplicationDbContext>();
//         context.Database.EnsureCreated();
//         logger.LogInformation("資料庫連接成功，資料表已就緒");
//     }
//     catch (Exception ex)
//     {
//         logger.LogError(ex, "資料庫初始化時發生錯誤");
//     }
// }

app.MapControllers();

// 輸出 Swagger 資訊到控制台
var environment = app.Environment;
var swaggerUrl = environment.IsDevelopment() 
    ? "http://localhost:5000/swagger" 
    : $"http://localhost:5000/swagger";

logger.LogInformation("==========================================");
logger.LogInformation("Picture2Text API 已啟動");
logger.LogInformation("==========================================");
logger.LogInformation("Swagger UI: {SwaggerUrl}", swaggerUrl);

app.Run();
