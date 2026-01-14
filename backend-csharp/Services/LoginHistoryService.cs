using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services;

/// <summary>
/// 登入歷史記錄服務
/// </summary>
public class LoginHistoryService
{
    private readonly ApplicationDbContext _context;

    public LoginHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 記錄登入嘗試
    /// </summary>
    public async Task RecordLoginAttemptAsync(
        string attemptedUserId,
        int? userId,
        bool isSuccess,
        string? failureReason,
        string? ipAddress,
        string? userAgent,
        string? deviceInfo = null)
    {
        var history = new LoginHistory
        {
            UserId = userId,
            AttemptedUserId = attemptedUserId,
            IsSuccess = isSuccess,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = deviceInfo,
            AttemptedAt = DateTime.UtcNow
        };

        _context.LoginHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 取得使用者的登入歷史
    /// </summary>
    public async Task<List<LoginHistory>> GetUserLoginHistoryAsync(int userId, int limit = 50)
    {
        return await _context.LoginHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.AttemptedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 取得最近的失敗登入嘗試次數（用於防暴力破解）
    /// </summary>
    public async Task<int> GetRecentFailedAttemptsAsync(string attemptedUserId, int minutesWindow = 15)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-minutesWindow);
        
        return await _context.LoginHistories
            .Where(h => h.AttemptedUserId == attemptedUserId 
                     && !h.IsSuccess 
                     && h.AttemptedAt >= windowStart)
            .CountAsync();
    }

    /// <summary>
    /// 清理舊的登入歷史記錄
    /// </summary>
    public async Task CleanupOldHistoryAsync(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldRecords = await _context.LoginHistories
            .Where(h => h.AttemptedAt < cutoffDate)
            .ToListAsync();

        _context.LoginHistories.RemoveRange(oldRecords);
        await _context.SaveChangesAsync();
    }
}
