import { Injectable, Logger } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository, LessThan } from 'typeorm';
import { ConfigService } from '@nestjs/config';
import { v4 as uuidv4 } from 'uuid';
import { UserSession } from '../entities';
import { SessionInfo } from '../dto/response';

@Injectable()
export class SessionService {
  private readonly logger = new Logger(SessionService.name);
  private readonly refreshTokenExpirationDays: number;

  constructor(
    @InjectRepository(UserSession)
    private readonly sessionRepository: Repository<UserSession>,
    private readonly configService: ConfigService,
  ) {
    this.refreshTokenExpirationDays = this.configService.get<number>('jwt.refreshTokenExpirationDays')!;
  }

  /**
   * 建立新 Session
   */
  async createSessionAsync(
    userId: number,
    refreshTokenId: number,
    ipAddress?: string,
    userAgent?: string,
  ): Promise<UserSession> {
    const sessionId = uuidv4();
    const now = new Date();
    const expiresAt = new Date();
    expiresAt.setDate(expiresAt.getDate() + this.refreshTokenExpirationDays);

    const session = this.sessionRepository.create({
      UserId: userId,
      SessionId: sessionId,
      RefreshTokenId: refreshTokenId,
      DeviceName: this.parseDeviceName(userAgent),
      IpAddress: ipAddress,
      UserAgent: userAgent,
      LoginAt: now,
      LastActivityAt: now,
      ExpiresAt: expiresAt,
      IsActive: true,
    });

    await this.sessionRepository.save(session);

    this.logger.log(`Created session ${sessionId} for user ${userId}`);

    return session;
  }

  /**
   * 更新 Session 活動時間
   */
  async updateSessionActivityAsync(sessionId: string): Promise<void> {
    await this.sessionRepository.update(
      { SessionId: sessionId, IsActive: true },
      { LastActivityAt: new Date() },
    );
  }

  /**
   * 結束 Session
   */
  async endSessionAsync(sessionId: string, userId: number): Promise<boolean> {
    const result = await this.sessionRepository.update(
      { SessionId: sessionId, UserId: userId, IsActive: true },
      { IsActive: false, LogoutAt: new Date() },
    );

    if (result.affected && result.affected > 0) {
      this.logger.log(`Ended session ${sessionId} for user ${userId}`);
      return true;
    }

    return false;
  }

  /**
   * 結束使用者的所有 Session
   */
  async endAllUserSessionsAsync(userId: number): Promise<number> {
    const result = await this.sessionRepository.update(
      { UserId: userId, IsActive: true },
      { IsActive: false, LogoutAt: new Date() },
    );

    const count = result.affected || 0;
    this.logger.log(`Ended ${count} sessions for user ${userId}`);

    return count;
  }

  /**
   * 取得使用者的活躍 Session 列表
   */
  async getUserActiveSessionsAsync(userId: number, currentSessionId?: string): Promise<SessionInfo[]> {
    const sessions = await this.sessionRepository.find({
      where: { UserId: userId, IsActive: true },
      order: { LastActivityAt: 'DESC' },
    });

    return sessions.map((session) => ({
      SessionId: session.SessionId,
      DeviceName: session.DeviceName || undefined,
      IpAddress: session.IpAddress || undefined,
      LoginAt: session.LoginAt,
      LastActivityAt: session.LastActivityAt,
      ExpiresAt: session.ExpiresAt,
      IsCurrent: session.SessionId === currentSessionId,
    }));
  }

  /**
   * 根據 Session ID 取得 Session
   */
  async getSessionByIdAsync(sessionId: string): Promise<UserSession | null> {
    return this.sessionRepository.findOne({
      where: { SessionId: sessionId },
      relations: ['User'],
    });
  }

  /**
   * 更新 Session 的 Refresh Token ID
   */
  async updateSessionRefreshTokenAsync(sessionId: string, refreshTokenId: number): Promise<void> {
    const expiresAt = new Date();
    expiresAt.setDate(expiresAt.getDate() + this.refreshTokenExpirationDays);

    await this.sessionRepository.update(
      { SessionId: sessionId },
      {
        RefreshTokenId: refreshTokenId,
        LastActivityAt: new Date(),
        ExpiresAt: expiresAt,
      },
    );
  }

  /**
   * 清理過期的 Session
   */
  async cleanupExpiredSessionsAsync(): Promise<number> {
    const result = await this.sessionRepository.update(
      { IsActive: true, ExpiresAt: LessThan(new Date()) },
      { IsActive: false },
    );

    const count = result.affected || 0;
    if (count > 0) {
      this.logger.log(`Marked ${count} expired sessions as inactive`);
    }

    return count;
  }

  /**
   * 解析裝置名稱
   */
  private parseDeviceName(userAgent?: string): string {
    if (!userAgent) {
      return 'Unknown Device';
    }

    const ua = userAgent.toLowerCase();

    // 偵測瀏覽器
    let browser = 'Unknown Browser';
    if (ua.includes('edg/')) {
      browser = 'Edge';
    } else if (ua.includes('chrome/')) {
      browser = 'Chrome';
    } else if (ua.includes('firefox/')) {
      browser = 'Firefox';
    } else if (ua.includes('safari/') && !ua.includes('chrome')) {
      browser = 'Safari';
    } else if (ua.includes('opera') || ua.includes('opr/')) {
      browser = 'Opera';
    }

    // 偵測作業系統
    let os = 'Unknown OS';
    if (ua.includes('windows')) {
      os = 'Windows';
    } else if (ua.includes('macintosh') || ua.includes('mac os')) {
      os = 'macOS';
    } else if (ua.includes('linux') && !ua.includes('android')) {
      os = 'Linux';
    } else if (ua.includes('android')) {
      os = 'Android';
    } else if (ua.includes('iphone') || ua.includes('ipad')) {
      os = 'iOS';
    }

    return `${browser} on ${os}`;
  }
}
