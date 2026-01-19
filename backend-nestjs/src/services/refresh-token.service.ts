import { Injectable, Logger } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository, LessThan } from 'typeorm';
import { ConfigService } from '@nestjs/config';
import * as crypto from 'crypto';
import { RefreshToken } from '../entities';

@Injectable()
export class RefreshTokenService {
  private readonly logger = new Logger(RefreshTokenService.name);
  private readonly refreshTokenExpirationDays: number;

  constructor(
    @InjectRepository(RefreshToken)
    private readonly refreshTokenRepository: Repository<RefreshToken>,
    private readonly configService: ConfigService,
  ) {
    this.refreshTokenExpirationDays = this.configService.get<number>('jwt.refreshTokenExpirationDays')!;
  }

  /**
   * 產生 Refresh Token
   */
  async generateRefreshTokenAsync(
    userId: number,
    tokenId: string,
    ipAddress?: string,
    userAgent?: string,
    deviceInfo?: string,
  ): Promise<{ token: string; expiresAt: Date }> {
    // 產生隨機 Token
    const rawToken = crypto.randomBytes(64).toString('base64url');

    // 計算 Token 的 SHA-256 雜湊（存入資料庫）
    const hashedToken = this.hashToken(rawToken);

    const expiresAt = new Date();
    expiresAt.setDate(expiresAt.getDate() + this.refreshTokenExpirationDays);

    const refreshToken = this.refreshTokenRepository.create({
      UserId: userId,
      Token: hashedToken,
      TokenId: tokenId,
      ExpiresAt: expiresAt,
      IsRevoked: false,
      IpAddress: ipAddress,
      UserAgent: userAgent,
      DeviceInfo: deviceInfo,
    });

    await this.refreshTokenRepository.save(refreshToken);

    this.logger.log(`Generated refresh token for user ${userId}`);

    // 回傳原始 Token（不是雜湊後的）
    return { token: rawToken, expiresAt };
  }

  /**
   * 驗證 Refresh Token
   */
  async validateRefreshTokenAsync(token: string): Promise<RefreshToken | null> {
    const hashedToken = this.hashToken(token);

    const refreshToken = await this.refreshTokenRepository.findOne({
      where: { Token: hashedToken },
      relations: ['User'],
    });

    if (!refreshToken) {
      this.logger.warn('Refresh token not found');
      return null;
    }

    if (refreshToken.IsRevoked) {
      this.logger.warn(`Refresh token ${refreshToken.Id} has been revoked`);
      return null;
    }

    if (new Date() >= refreshToken.ExpiresAt) {
      this.logger.warn(`Refresh token ${refreshToken.Id} has expired`);
      return null;
    }

    // 更新最後使用時間
    refreshToken.LastUsedAt = new Date();
    await this.refreshTokenRepository.save(refreshToken);

    return refreshToken;
  }

  /**
   * 撤銷 Refresh Token
   */
  async revokeRefreshTokenAsync(token: string, replacedByToken?: string): Promise<boolean> {
    const hashedToken = this.hashToken(token);

    const refreshToken = await this.refreshTokenRepository.findOne({
      where: { Token: hashedToken },
    });

    if (!refreshToken) {
      return false;
    }

    refreshToken.IsRevoked = true;
    refreshToken.RevokedAt = new Date();
    if (replacedByToken) {
      refreshToken.ReplacedByToken = this.hashToken(replacedByToken);
    }

    await this.refreshTokenRepository.save(refreshToken);

    this.logger.log(`Revoked refresh token ${refreshToken.Id}`);

    return true;
  }

  /**
   * 撤銷使用者的所有 Refresh Token
   */
  async revokeAllUserTokensAsync(userId: number): Promise<number> {
    const result = await this.refreshTokenRepository.update(
      { UserId: userId, IsRevoked: false },
      { IsRevoked: true, RevokedAt: new Date() },
    );

    const count = result.affected || 0;
    this.logger.log(`Revoked ${count} refresh tokens for user ${userId}`);

    return count;
  }

  /**
   * 根據 Token ID 取得 Refresh Token
   */
  async getByTokenIdAsync(tokenId: string): Promise<RefreshToken | null> {
    return this.refreshTokenRepository.findOne({
      where: { TokenId: tokenId },
      relations: ['User'],
    });
  }

  /**
   * 清理過期的 Refresh Token
   */
  async cleanupExpiredTokensAsync(): Promise<number> {
    const result = await this.refreshTokenRepository.delete({
      ExpiresAt: LessThan(new Date()),
    });

    const count = result.affected || 0;
    if (count > 0) {
      this.logger.log(`Cleaned up ${count} expired refresh tokens`);
    }

    return count;
  }

  /**
   * 計算 Token 的 SHA-256 雜湊
   */
  private hashToken(token: string): string {
    return crypto.createHash('sha256').update(token).digest('hex');
  }
}
