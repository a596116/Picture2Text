using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services;

/// <summary>
/// 使用者會話管理服務
/// </summary>
public class SessionService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly int _sessionExpirationDays;

    public SessionService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _sessionExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
    }

    /// <summary>
    /// 創建新會話
    /// </summary>
    public async Task<UserSession> CreateSessionAsync(
        int userId,
        int? refreshTokenId,
        string? ipAddress,
        string? userAgent,
        string? deviceName = null)
    {
        var session = new UserSession
        {
            UserId = userId,
            SessionId = Guid.NewGuid().ToString(),
            RefreshTokenId = refreshTokenId,
            DeviceName = deviceName ?? ParseDeviceName(userAgent),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            LoginAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_sessionExpirationDays),
            IsActive = true
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    /// <summary>
    /// 更新會話活躍時間
    /// </summary>
    public async Task UpdateSessionActivityAsync(string sessionId)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 結束會話（登出）
    /// </summary>
    public async Task<bool> EndSessionAsync(string sessionId)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

        if (session == null)
        {
            return false;
        }

        session.IsActive = false;
        session.LogoutAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 結束使用者的所有會話
    /// </summary>
    public async Task EndAllUserSessionsAsync(int userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.LogoutAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 取得使用者的所有活躍會話
    /// </summary>
    public async Task<List<UserSession>> GetUserActiveSessionsAsync(int userId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }

    /// <summary>
    /// 清理過期會話
    /// </summary>
    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _context.UserSessions
            .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
            session.LogoutAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 從 User-Agent 解析裝置名稱
    /// </summary>
    private static string ParseDeviceName(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return "未知裝置";
        }

        // 簡單的裝置識別邏輯
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
            return "iPhone";
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "iPad";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android 裝置";
        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return "Windows 電腦";
        if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase))
            return "Mac 電腦";
        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            return "Linux 電腦";

        return "未知裝置";
    }
}
