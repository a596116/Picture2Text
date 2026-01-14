using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services;

/// <summary>
/// Refresh Token 管理服務
/// </summary>
public class RefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly int _refreshTokenExpirationDays;

    public RefreshTokenService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
    }

    /// <summary>
    /// 生成新的 Refresh Token
    /// </summary>
    public async Task<RefreshToken> GenerateRefreshTokenAsync(
        int userId, 
        string? ipAddress, 
        string? userAgent, 
        string? deviceInfo)
    {
        var tokenId = Guid.NewGuid().ToString();
        var token = GenerateTokenString();

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = HashToken(token), // 儲存加密後的 token
            TokenId = tokenId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            IsRevoked = false,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = deviceInfo
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // 返回原始 token（未加密）給客戶端
        refreshToken.Token = token;
        return refreshToken;
    }

    /// <summary>
    /// 驗證 Refresh Token
    /// </summary>
    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
    {
        var hashedToken = HashToken(token);
        
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return null;
        }

        // 更新最後使用時間
        refreshToken.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    /// <summary>
    /// 撤銷 Refresh Token
    /// </summary>
    public async Task<bool> RevokeRefreshTokenAsync(string token, string? replacedByToken = null)
    {
        var hashedToken = HashToken(token);
        
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

        if (refreshToken == null || refreshToken.IsRevoked)
        {
            return false;
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = replacedByToken;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 撤銷使用者的所有 Refresh Token
    /// </summary>
    public async Task RevokeAllUserTokensAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 清理過期的 Refresh Token
    /// </summary>
    public async Task CleanupExpiredTokensAsync()
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 生成隨機 Token 字串
    /// </summary>
    private static string GenerateTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// 對 Token 進行雜湊加密
    /// </summary>
    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}
