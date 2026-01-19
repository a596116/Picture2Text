import { Injectable, Logger, UnauthorizedException, HttpException, HttpStatus } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import * as bcrypt from 'bcrypt';
import { User, RefreshToken } from '../entities';
import { JwtTokenService } from './jwt.service';
import { RefreshTokenService } from './refresh-token.service';
import { SessionService } from './session.service';
import { LoginHistoryService } from './login-history.service';
import { TokenData, ValidateTokenData } from '../dto/response';

interface LoginResult {
  success: boolean;
  data?: TokenData;
  errorMessage?: string;
  statusCode?: number;
}

interface RefreshResult {
  success: boolean;
  data?: TokenData;
  errorMessage?: string;
}

@Injectable()
export class AuthService {
  private readonly logger = new Logger(AuthService.name);
  private readonly maxFailedAttempts = 5;
  private readonly lockoutWindowMinutes = 15;

  constructor(
    @InjectRepository(User)
    private readonly userRepository: Repository<User>,
    private readonly jwtService: JwtTokenService,
    private readonly refreshTokenService: RefreshTokenService,
    private readonly sessionService: SessionService,
    private readonly loginHistoryService: LoginHistoryService,
  ) {}

  /**
   * 使用者登入
   */
  async loginAsync(
    userId: number,
    password: string,
    ipAddress?: string,
    userAgent?: string,
    targetSystem?: string,
  ): Promise<LoginResult> {
    const attemptedUserId = userId.toString();
    const deviceInfo = this.extractDeviceInfo(userAgent);

    // 檢查帳戶是否被鎖定
    const isLocked = await this.loginHistoryService.isAccountLockedAsync(
      attemptedUserId,
      this.maxFailedAttempts,
      this.lockoutWindowMinutes,
    );

    if (isLocked) {
      await this.loginHistoryService.recordLoginAttemptAsync(
        attemptedUserId,
        false,
        '帳戶已鎖定，請稍後再試',
        undefined,
        ipAddress,
        userAgent,
        deviceInfo,
      );

      return {
        success: false,
        errorMessage: `帳戶已鎖定，請在 ${this.lockoutWindowMinutes} 分鐘後再試`,
        statusCode: HttpStatus.TOO_MANY_REQUESTS,
      };
    }

    // 查詢使用者
    const user = await this.userRepository.findOne({ where: { Id: userId } });

    if (!user) {
      await this.loginHistoryService.recordLoginAttemptAsync(
        attemptedUserId,
        false,
        '使用者不存在',
        undefined,
        ipAddress,
        userAgent,
        deviceInfo,
      );

      return {
        success: false,
        errorMessage: '使用者名稱或密碼錯誤',
        statusCode: HttpStatus.UNAUTHORIZED,
      };
    }

    // 驗證密碼
    const isPasswordValid = await bcrypt.compare(password, user.Password);

    if (!isPasswordValid) {
      await this.loginHistoryService.recordLoginAttemptAsync(
        attemptedUserId,
        false,
        '密碼錯誤',
        user.Id,
        ipAddress,
        userAgent,
        deviceInfo,
      );

      // 檢查剩餘嘗試次數
      const failedAttempts = await this.loginHistoryService.getRecentFailedAttemptsAsync(
        attemptedUserId,
        this.lockoutWindowMinutes,
      );
      const remainingAttempts = this.maxFailedAttempts - failedAttempts;

      let message = '使用者名稱或密碼錯誤';
      if (remainingAttempts <= 2 && remainingAttempts > 0) {
        message += `（剩餘 ${remainingAttempts} 次嘗試機會）`;
      }

      return {
        success: false,
        errorMessage: message,
        statusCode: HttpStatus.UNAUTHORIZED,
      };
    }

    // 產生 Token
    const { token: accessToken, tokenId, expiresAt } = this.jwtService.generateToken(
      user.Id,
      user.IdNo,
      user.Name,
      '', // sessionId 會在後面填入
      targetSystem,
    );

    // 產生 Refresh Token
    const { token: refreshToken, expiresAt: refreshTokenExpiresAt } =
      await this.refreshTokenService.generateRefreshTokenAsync(
        user.Id,
        tokenId,
        ipAddress,
        userAgent,
        deviceInfo,
      );

    // 取得剛建立的 RefreshToken 實體
    const refreshTokenEntity = await this.refreshTokenService.getByTokenIdAsync(tokenId);

    // 建立 Session
    const session = await this.sessionService.createSessionAsync(
      user.Id,
      refreshTokenEntity!.Id,
      ipAddress,
      userAgent,
    );

    // 重新產生包含 SessionId 的 Token
    const { token: finalAccessToken, expiresAt: finalExpiresAt } = this.jwtService.generateToken(
      user.Id,
      user.IdNo,
      user.Name,
      session.SessionId,
      targetSystem,
    );

    // 記錄成功登入
    await this.loginHistoryService.recordLoginAttemptAsync(
      attemptedUserId,
      true,
      undefined,
      user.Id,
      ipAddress,
      userAgent,
      deviceInfo,
    );

    this.logger.log(`User ${user.Id} logged in successfully from ${ipAddress}`);

    return {
      success: true,
      data: {
        AccessToken: finalAccessToken,
        RefreshToken: refreshToken,
        TokenType: 'Bearer',
        ExpiresAt: finalExpiresAt,
        RefreshTokenExpiresAt: refreshTokenExpiresAt,
        SessionId: session.SessionId,
      },
    };
  }

  /**
   * 刷新 Token
   */
  async refreshTokenAsync(
    refreshToken: string,
    ipAddress?: string,
    userAgent?: string,
    targetSystem?: string,
  ): Promise<RefreshResult> {
    // 驗證 Refresh Token
    const existingToken = await this.refreshTokenService.validateRefreshTokenAsync(refreshToken);

    if (!existingToken) {
      return {
        success: false,
        errorMessage: 'Refresh Token 無效或已過期',
      };
    }

    const user = existingToken.User;
    const deviceInfo = this.extractDeviceInfo(userAgent);

    // 取得現有 Session
    const session = await this.sessionService.getSessionByIdAsync(
      (await this.getSessionIdByRefreshTokenId(existingToken.Id)) || '',
    );

    // 產生新的 Access Token
    const { token: newAccessToken, tokenId: newTokenId, expiresAt } = this.jwtService.generateToken(
      user.Id,
      user.IdNo,
      user.Name,
      session?.SessionId || '',
      targetSystem,
    );

    // 產生新的 Refresh Token
    const { token: newRefreshToken, expiresAt: newRefreshTokenExpiresAt } =
      await this.refreshTokenService.generateRefreshTokenAsync(
        user.Id,
        newTokenId,
        ipAddress,
        userAgent,
        deviceInfo,
      );

    // 撤銷舊的 Refresh Token（Token 輪換）
    await this.refreshTokenService.revokeRefreshTokenAsync(refreshToken, newRefreshToken);

    // 更新 Session
    if (session) {
      const newRefreshTokenEntity = await this.refreshTokenService.getByTokenIdAsync(newTokenId);
      if (newRefreshTokenEntity) {
        await this.sessionService.updateSessionRefreshTokenAsync(
          session.SessionId,
          newRefreshTokenEntity.Id,
        );
      }
    }

    this.logger.log(`Token refreshed for user ${user.Id}`);

    return {
      success: true,
      data: {
        AccessToken: newAccessToken,
        RefreshToken: newRefreshToken,
        TokenType: 'Bearer',
        ExpiresAt: expiresAt,
        RefreshTokenExpiresAt: newRefreshTokenExpiresAt,
        SessionId: session?.SessionId || '',
      },
    };
  }

  /**
   * 撤銷 Token（登出）
   */
  async revokeTokenAsync(
    userId: number,
    sessionId?: string,
    refreshToken?: string,
    revokeAllDevices = false,
  ): Promise<boolean> {
    if (revokeAllDevices) {
      // 撤銷所有裝置
      await this.refreshTokenService.revokeAllUserTokensAsync(userId);
      await this.sessionService.endAllUserSessionsAsync(userId);
      this.logger.log(`Revoked all tokens and sessions for user ${userId}`);
      return true;
    }

    if (refreshToken) {
      // 撤銷指定的 Refresh Token
      await this.refreshTokenService.revokeRefreshTokenAsync(refreshToken);
    }

    if (sessionId) {
      // 結束指定的 Session
      await this.sessionService.endSessionAsync(sessionId, userId);
    }

    return true;
  }

  /**
   * 驗證 Token（供其他微服務使用）
   */
  async validateTokenAsync(token: string): Promise<ValidateTokenData> {
    const result = await this.jwtService.validateTokenAsync(token);

    if (!result.isValid) {
      return {
        IsValid: false,
        ErrorMessage: result.errorMessage,
      };
    }

    // 取得使用者詳細資訊
    const user = await this.userRepository.findOne({ where: { Id: result.userId } });

    if (!user) {
      return {
        IsValid: false,
        ErrorMessage: '使用者不存在',
      };
    }

    return {
      IsValid: true,
      UserId: user.Id,
      IdNo: user.IdNo,
      Name: user.Name,
      ExpiresAt: result.expiresAt,
      TokenId: result.tokenId,
    };
  }

  /**
   * 取得當前使用者資訊
   */
  async getCurrentUserAsync(userId: number): Promise<User | null> {
    return this.userRepository.findOne({ where: { Id: userId } });
  }

  /**
   * 提取裝置資訊
   */
  private extractDeviceInfo(userAgent?: string): string {
    if (!userAgent) {
      return 'Unknown Device';
    }

    const ua = userAgent.toLowerCase();

    // 偵測裝置類型
    if (ua.includes('mobile') || ua.includes('android') || ua.includes('iphone')) {
      return 'Mobile Device';
    } else if (ua.includes('tablet') || ua.includes('ipad')) {
      return 'Tablet';
    } else {
      return 'Desktop';
    }
  }

  /**
   * 根據 RefreshToken ID 取得 Session ID
   */
  private async getSessionIdByRefreshTokenId(refreshTokenId: number): Promise<string | null> {
    const session = await this.sessionService['sessionRepository'].findOne({
      where: { RefreshTokenId: refreshTokenId, IsActive: true },
    });
    return session?.SessionId || null;
  }
}
