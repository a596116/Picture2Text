using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Picture2Text.Api.Data;

namespace Picture2Text.Api.Services;

public class JwtService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly List<string> _audiences;
    private readonly int _expirationMinutes;

    public JwtService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey 未設定");
        _issuer = configuration["Jwt:Issuer"] ?? "AuthCenter.Api";
        
        // 支援多個 Audience（多套系統）
        _audiences = configuration.GetSection("Jwt:Audiences").Get<List<string>>() 
            ?? new List<string> { "AllSystems" };
        
        _expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "30");
    }

    /// <summary>
    /// 生成 Access Token（支援多個 Audience）
    /// </summary>
    public string GenerateToken(int userId, string idNo, string? sessionId = null, string? targetSystem = null)
    {
        var tokenId = Guid.NewGuid().ToString();
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, idNo),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId)
        };

        // 添加會話 ID（如果提供）
        if (!string.IsNullOrEmpty(sessionId))
        {
            claims.Add(new Claim("sessionId", sessionId));
        }

        // 添加目標系統（如果提供）
        if (!string.IsNullOrEmpty(targetSystem))
        {
            claims.Add(new Claim("targetSystem", targetSystem));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 使用第一個 audience 或指定的 targetSystem
        var audience = !string.IsNullOrEmpty(targetSystem) && _audiences.Contains(targetSystem)
            ? targetSystem
            : _audiences.FirstOrDefault() ?? "AllSystems";

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 驗證並解析 Token（支援多個 Audience）
    /// </summary>
    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudiences = _audiences,  // 支援多個 Audience
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // 檢查是否為 JWT token
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.FromResult<ClaimsPrincipal?>(null);
            }

            return Task.FromResult<ClaimsPrincipal?>(principal);
        }
        catch
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
    }

    /// <summary>
    /// 從 Token 中提取使用者 ID
    /// </summary>
    public int? GetUserIdFromToken(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// 從 Token 中提取會話 ID
    /// </summary>
    public string? GetSessionIdFromToken(ClaimsPrincipal principal)
    {
        return principal.FindFirst("sessionId")?.Value;
    }

    /// <summary>
    /// 從 Token 中提取 JTI（Token ID）
    /// </summary>
    public string? GetTokenIdFromToken(ClaimsPrincipal principal)
    {
        return principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    }

    /// <summary>
    /// 取得 Token 過期時間
    /// </summary>
    public DateTime GetTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_expirationMinutes);
    }
}

