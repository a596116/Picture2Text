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
    private readonly ILogger<LoginHistoryService> _logger;

    public LoginHistoryService(
        ApplicationDbContext context,
        ILogger<LoginHistoryService> logger)
    {
        _context = context;
        _logger = logger;
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
        // 記錄結構化日誌
        if (isSuccess)
        {
            _logger.LogInformation(
                "用戶登入成功 - UserId: {UserId}, AttemptedUserId: {AttemptedUserId}, IP: {IpAddress}",
                userId, attemptedUserId, ipAddress);
        }
        else
        {
            _logger.LogWarning(
                "用戶登入失敗 - AttemptedUserId: {AttemptedUserId}, IP: {IpAddress}, Reason: {FailureReason}",
                attemptedUserId, ipAddress, failureReason);
        }

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
        
        _logger.LogDebug(
            "登入歷史已儲存 - LoginHistoryId: {LoginHistoryId}, Success: {IsSuccess}",
            history.Id, isSuccess);
    }

    /// <summary>
    /// 取得使用者的登入歷史
    /// </summary>
    public async Task<List<LoginHistory>> GetUserLoginHistoryAsync(int userId, int limit = 50)
    {
        _logger.LogDebug("查詢用戶登入歷史 - UserId: {UserId}, Limit: {Limit}", userId, limit);
        
        var histories = await _context.LoginHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.AttemptedAt)
            .Take(limit)
            .ToListAsync();
            
        _logger.LogInformation(
            "查詢完成 - UserId: {UserId}, 找到 {Count} 筆記錄", 
            userId, histories.Count);
            
        return histories;
    }

    /// <summary>
    /// 取得最近的失敗登入嘗試次數（用於防暴力破解）
    /// </summary>
    public async Task<int> GetRecentFailedAttemptsAsync(string attemptedUserId, int minutesWindow = 15)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-minutesWindow);
        
        var failedAttempts = await _context.LoginHistories
            .Where(h => h.AttemptedUserId == attemptedUserId 
                     && !h.IsSuccess 
                     && h.AttemptedAt >= windowStart)
            .CountAsync();
            
        if (failedAttempts > 5)
        {
            _logger.LogError(
                "⚠️ 偵測到異常登入行為！AttemptedUserId: {AttemptedUserId} 在 {MinutesWindow} 分鐘內失敗 {FailureCount} 次",
                attemptedUserId, minutesWindow, failedAttempts);
        }
        else if (failedAttempts > 3)
        {
            _logger.LogWarning(
                "注意：AttemptedUserId: {AttemptedUserId} 在 {MinutesWindow} 分鐘內失敗 {FailureCount} 次",
                attemptedUserId, minutesWindow, failedAttempts);
        }
        
        return failedAttempts;
    }

    /// <summary>
    /// 清理舊的登入歷史記錄
    /// </summary>
    public async Task CleanupOldHistoryAsync(int daysToKeep = 90)
    {
        _logger.LogInformation("開始清理舊的登入歷史記錄 - 保留天數: {DaysToKeep}", daysToKeep);
        
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldRecords = await _context.LoginHistories
            .Where(h => h.AttemptedAt < cutoffDate)
            .ToListAsync();

        if (oldRecords.Any())
        {
            _logger.LogInformation(
                "正在刪除 {RecordCount} 筆舊記錄（早於 {CutoffDate:yyyy-MM-dd}）",
                oldRecords.Count, cutoffDate);
                
            _context.LoginHistories.RemoveRange(oldRecords);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("清理完成 - 已刪除 {RecordCount} 筆記錄", oldRecords.Count);
        }
        else
        {
            _logger.LogInformation("沒有需要清理的舊記錄");
        }
    }
}
