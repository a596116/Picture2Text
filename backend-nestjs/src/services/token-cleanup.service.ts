import { Injectable, Logger, OnModuleInit } from '@nestjs/common';
import { Cron, CronExpression } from '@nestjs/schedule';
import { RefreshTokenService } from './refresh-token.service';
import { SessionService } from './session.service';
import { LoginHistoryService } from './login-history.service';

@Injectable()
export class TokenCleanupService implements OnModuleInit {
  private readonly logger = new Logger(TokenCleanupService.name);

  constructor(
    private readonly refreshTokenService: RefreshTokenService,
    private readonly sessionService: SessionService,
    private readonly loginHistoryService: LoginHistoryService,
  ) {}

  onModuleInit() {
    this.logger.log('Token cleanup service initialized');
  }

  /**
   * 每小時執行一次清理任務
   */
  @Cron(CronExpression.EVERY_HOUR)
  async handleCleanup() {
    this.logger.log('Starting scheduled cleanup task');

    try {
      // 清理過期的 Refresh Token
      const expiredTokens = await this.refreshTokenService.cleanupExpiredTokensAsync();

      // 清理過期的 Session
      const expiredSessions = await this.sessionService.cleanupExpiredSessionsAsync();

      // 清理舊的登入歷史記錄（保留 90 天）
      const oldHistory = await this.loginHistoryService.cleanupOldHistoryAsync(90);

      this.logger.log(
        `Cleanup completed: ${expiredTokens} tokens, ${expiredSessions} sessions, ${oldHistory} history records removed`,
      );
    } catch (error) {
      this.logger.error('Error during cleanup task', error);
    }
  }

  /**
   * 手動觸發清理（可供管理介面使用）
   */
  async manualCleanup(): Promise<{
    expiredTokens: number;
    expiredSessions: number;
    oldHistory: number;
  }> {
    this.logger.log('Manual cleanup triggered');

    const expiredTokens = await this.refreshTokenService.cleanupExpiredTokensAsync();
    const expiredSessions = await this.sessionService.cleanupExpiredSessionsAsync();
    const oldHistory = await this.loginHistoryService.cleanupOldHistoryAsync(90);

    return {
      expiredTokens,
      expiredSessions,
      oldHistory,
    };
  }
}
