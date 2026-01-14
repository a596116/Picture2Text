using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Picture2Text.Api.Services;

/// <summary>
/// 背景服務 - 定期清理過期的 Token 和會話
/// </summary>
public class TokenCleanupService : BackgroundService
{
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // 每小時執行一次

    public TokenCleanupService(
        ILogger<TokenCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token 清理服務已啟動");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                _logger.LogInformation("開始清理過期的 Token 和會話...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var refreshTokenService = scope.ServiceProvider.GetRequiredService<RefreshTokenService>();
                    var sessionService = scope.ServiceProvider.GetRequiredService<SessionService>();
                    var loginHistoryService = scope.ServiceProvider.GetRequiredService<LoginHistoryService>();

                    // 清理過期的 Refresh Token
                    await refreshTokenService.CleanupExpiredTokensAsync();

                    // 清理過期的會話
                    await sessionService.CleanupExpiredSessionsAsync();

                    // 清理 90 天前的登入歷史
                    await loginHistoryService.CleanupOldHistoryAsync(90);

                    _logger.LogInformation("Token 和會話清理完成");
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理 Token 和會話時發生錯誤");
            }
        }

        _logger.LogInformation("Token 清理服務已停止");
    }
}
