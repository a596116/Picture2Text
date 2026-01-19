import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { JwtService as NestJwtService } from '@nestjs/jwt';
import { v4 as uuidv4 } from 'uuid';
import { JwtPayload, TokenValidationResult } from '../interfaces';

@Injectable()
export class JwtTokenService {
  private readonly logger = new Logger(JwtTokenService.name);
  private readonly secretKey: string;
  private readonly issuer: string;
  private readonly audiences: string[];
  private readonly expirationMinutes: number;

  constructor(
    private readonly jwtService: NestJwtService,
    private readonly configService: ConfigService,
  ) {
    this.secretKey = this.configService.get<string>('jwt.secretKey')!;
    this.issuer = this.configService.get<string>('jwt.issuer')!;
    this.audiences = this.configService.get<string[]>('jwt.audiences')!;
    this.expirationMinutes = this.configService.get<number>('jwt.expirationMinutes')!;
  }

  /**
   * 產生 Access Token
   */
  generateToken(
    userId: number,
    idNo: string,
    name: string,
    sessionId: string,
    targetSystem?: string,
  ): { token: string; tokenId: string; expiresAt: Date } {
    const tokenId = uuidv4();
    const expiresAt = new Date();
    expiresAt.setMinutes(expiresAt.getMinutes() + this.expirationMinutes);

    const payload: JwtPayload = {
      sub: userId,
      idNo,
      name,
      sessionId,
      jti: tokenId,
      targetSystem,
    };

    const token = this.jwtService.sign(payload, {
      secret: this.secretKey,
      expiresIn: `${this.expirationMinutes}m`,
      issuer: this.issuer,
      audience: this.audiences,
    });

    return { token, tokenId, expiresAt };
  }

  /**
   * 驗證 Token
   */
  async validateTokenAsync(token: string): Promise<TokenValidationResult> {
    try {
      const payload = this.jwtService.verify<JwtPayload>(token, {
        secret: this.secretKey,
        issuer: this.issuer,
        audience: this.audiences,
      });

      const expiresAt = payload.exp ? new Date(payload.exp * 1000) : undefined;

      return {
        isValid: true,
        userId: payload.sub,
        idNo: payload.idNo,
        name: payload.name,
        sessionId: payload.sessionId,
        tokenId: payload.jti,
        expiresAt,
      };
    } catch (error) {
      const errorMessage = this.getTokenErrorMessage(error);
      this.logger.warn(`Token validation failed: ${errorMessage}`);
      return {
        isValid: false,
        errorMessage,
      };
    }
  }

  /**
   * 從 Token 中取得使用者 ID
   */
  getUserIdFromToken(token: string): number | null {
    try {
      const payload = this.jwtService.decode(token) as JwtPayload;
      return payload?.sub ?? null;
    } catch {
      return null;
    }
  }

  /**
   * 從 Token 中取得 Session ID
   */
  getSessionIdFromToken(token: string): string | null {
    try {
      const payload = this.jwtService.decode(token) as JwtPayload;
      return payload?.sessionId ?? null;
    } catch {
      return null;
    }
  }

  /**
   * 從 Token 中取得 Token ID (JTI)
   */
  getTokenIdFromToken(token: string): string | null {
    try {
      const payload = this.jwtService.decode(token) as JwtPayload;
      return payload?.jti ?? null;
    } catch {
      return null;
    }
  }

  /**
   * 從 Token 中取得過期時間
   */
  getTokenExpiration(token: string): Date | null {
    try {
      const payload = this.jwtService.decode(token) as JwtPayload;
      return payload?.exp ? new Date(payload.exp * 1000) : null;
    } catch {
      return null;
    }
  }

  /**
   * 解析 Token (不驗證)
   */
  decodeToken(token: string): JwtPayload | null {
    try {
      return this.jwtService.decode(token) as JwtPayload;
    } catch {
      return null;
    }
  }

  private getTokenErrorMessage(error: unknown): string {
    if (error instanceof Error) {
      if (error.name === 'TokenExpiredError') {
        return 'Token 已過期';
      }
      if (error.name === 'JsonWebTokenError') {
        return 'Token 無效';
      }
      if (error.name === 'NotBeforeError') {
        return 'Token 尚未生效';
      }
      return error.message;
    }
    return '未知錯誤';
  }
}
