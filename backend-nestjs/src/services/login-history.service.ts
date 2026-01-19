import { Injectable, Logger } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository, LessThan, MoreThan, And } from 'typeorm';
import { LoginHistory } from '../entities';
import { LoginHistoryInfo } from '../dto/response';

@Injectable()
export class LoginHistoryService {
  private readonly logger = new Logger(LoginHistoryService.name);

  constructor(
    @InjectRepository(LoginHistory)
    private readonly loginHistoryRepository: Repository<LoginHistory>,
  ) {}

  /**
   * 記錄登入嘗試
   */
  async recordLoginAttemptAsync(
    attemptedUserId: string,
    isSuccess: boolean,
    failureReason?: string,
    userId?: number,
    ipAddress?: string,
    userAgent?: string,
    deviceInfo?: string,
    location?: string,
  ): Promise<LoginHistory> {
    const history = this.loginHistoryRepository.create({
      UserId: userId ?? null,
      AttemptedUserId: attemptedUserId,
      IsSuccess: isSuccess,
      FailureReason: failureReason ?? null,
      IpAddress: ipAddress ?? null,
      UserAgent: userAgent ?? null,
      DeviceInfo: deviceInfo ?? null,
      AttemptedAt: new Date(),
      Location: location ?? null,
    });

    await this.loginHistoryRepository.save(history);

    if (isSuccess) {
      this.logger.log(`Successful login attempt for user ${attemptedUserId} from ${ipAddress}`);
    } else {
      this.logger.warn(`Failed login attempt for user ${attemptedUserId} from ${ipAddress}: ${failureReason}`);
    }

    return history;
  }

  /**
   * 取得使用者的登入歷史記錄
   */
  async getUserLoginHistoryAsync(userId: number, limit = 20): Promise<LoginHistoryInfo[]> {
    const histories = await this.loginHistoryRepository.find({
      where: { UserId: userId },
      order: { AttemptedAt: 'DESC' },
      take: limit,
    });

    return histories.map((history) => ({
      Id: history.Id,
      IsSuccess: history.IsSuccess,
      FailureReason: history.FailureReason || undefined,
      IpAddress: history.IpAddress || undefined,
      DeviceInfo: history.DeviceInfo || undefined,
      Location: history.Location || undefined,
      AttemptedAt: history.AttemptedAt,
    }));
  }

  /**
   * 取得最近的失敗登入嘗試次數（用於暴力破解防護）
   */
  async getRecentFailedAttemptsAsync(
    attemptedUserId: string,
    windowMinutes = 15,
  ): Promise<number> {
    const windowStart = new Date();
    windowStart.setMinutes(windowStart.getMinutes() - windowMinutes);

    const count = await this.loginHistoryRepository.count({
      where: {
        AttemptedUserId: attemptedUserId,
        IsSuccess: false,
        AttemptedAt: MoreThan(windowStart),
      },
    });

    return count;
  }

  /**
   * 檢查帳戶是否被鎖定
   */
  async isAccountLockedAsync(
    attemptedUserId: string,
    maxFailedAttempts = 5,
    windowMinutes = 15,
  ): Promise<boolean> {
    const failedAttempts = await this.getRecentFailedAttemptsAsync(attemptedUserId, windowMinutes);
    return failedAttempts >= maxFailedAttempts;
  }

  /**
   * 清理舊的登入歷史記錄
   */
  async cleanupOldHistoryAsync(retentionDays = 90): Promise<number> {
    const cutoffDate = new Date();
    cutoffDate.setDate(cutoffDate.getDate() - retentionDays);

    const result = await this.loginHistoryRepository.delete({
      AttemptedAt: LessThan(cutoffDate),
    });

    const count = result.affected || 0;
    if (count > 0) {
      this.logger.log(`Cleaned up ${count} login history records older than ${retentionDays} days`);
    }

    return count;
  }
}
