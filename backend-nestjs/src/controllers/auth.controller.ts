import {
  Controller,
  Post,
  Get,
  Body,
  Req,
  Res,
  HttpStatus,
  UseGuards,
} from '@nestjs/common';
import {
  ApiTags,
  ApiOperation,
  ApiResponse,
  ApiBearerAuth,
} from '@nestjs/swagger';
import { Request, Response } from 'express';
import { AuthService } from '../services';
import {
  LoginRequest,
  RefreshTokenRequest,
  ValidateTokenRequest,
  RevokeTokenRequest,
} from '../dto/request';
import {
  ApiResponse as ApiRes,
  TokenResponse,
  ValidateTokenResponse,
  ProfileData,
} from '../dto/response';
import { Public } from '../decorators/public.decorator';
import { CurrentUser, CurrentUserData } from '../decorators/current-user.decorator';
import { JwtAuthGuard } from '../guards';

@ApiTags('Auth')
@Controller('api/auth')
@UseGuards(JwtAuthGuard)
export class AuthController {
  constructor(private readonly authService: AuthService) {}

  /**
   * 使用者登入
   */
  @Post('login')
  @Public()
  @ApiOperation({ summary: '使用者登入', description: '驗證使用者憑證並回傳 JWT Token' })
  @ApiResponse({ status: 200, description: '登入成功', type: TokenResponse })
  @ApiResponse({ status: 401, description: '帳號或密碼錯誤' })
  @ApiResponse({ status: 429, description: '帳戶已鎖定' })
  async login(
    @Body() request: LoginRequest,
    @Req() req: Request,
    @Res() res: Response,
  ) {
    const ipAddress = this.getClientIp(req);
    const userAgent = req.headers['user-agent'];

    const result = await this.authService.loginAsync(
      request.UserId,
      request.Password,
      ipAddress,
      userAgent,
    );

    if (!result.success) {
      return res
        .status(result.statusCode || HttpStatus.UNAUTHORIZED)
        .json(ApiRes.error(result.statusCode || 401, result.errorMessage || '登入失敗'));
    }

    // 設定回應標頭（供 API Gateway 使用）
    res.setHeader('X-User-Id', result.data!.SessionId);

    return res.status(HttpStatus.OK).json(ApiRes.success(result.data, '登入成功'));
  }

  /**
   * 刷新 Token
   */
  @Post('refresh')
  @Public()
  @ApiOperation({ summary: '刷新 Token', description: '使用 Refresh Token 取得新的 Access Token' })
  @ApiResponse({ status: 200, description: '刷新成功', type: TokenResponse })
  @ApiResponse({ status: 401, description: 'Refresh Token 無效或已過期' })
  async refresh(
    @Body() request: RefreshTokenRequest,
    @Req() req: Request,
    @Res() res: Response,
  ) {
    const ipAddress = this.getClientIp(req);
    const userAgent = req.headers['user-agent'];

    const result = await this.authService.refreshTokenAsync(
      request.RefreshToken,
      ipAddress,
      userAgent,
    );

    if (!result.success) {
      return res
        .status(HttpStatus.UNAUTHORIZED)
        .json(ApiRes.unauthorized(result.errorMessage || 'Refresh Token 無效'));
    }

    return res.status(HttpStatus.OK).json(ApiRes.success(result.data, '刷新成功'));
  }

  /**
   * 撤銷 Token（登出）
   */
  @Post('revoke')
  @ApiBearerAuth()
  @ApiOperation({ summary: '撤銷 Token', description: '登出當前裝置或所有裝置' })
  @ApiResponse({ status: 200, description: '登出成功' })
  @ApiResponse({ status: 401, description: '未授權' })
  async revoke(
    @Body() request: RevokeTokenRequest,
    @CurrentUser() user: CurrentUserData,
    @Res() res: Response,
  ) {
    await this.authService.revokeTokenAsync(
      user.userId,
      user.sessionId,
      request.RefreshToken,
      request.RevokeAllDevices,
    );

    return res.status(HttpStatus.OK).json(ApiRes.success(null, '登出成功'));
  }

  /**
   * 驗證 Token（供其他微服務使用）
   */
  @Post('validate')
  @Public()
  @ApiOperation({
    summary: '驗證 Token',
    description: '驗證 Access Token 是否有效（供內部微服務使用）',
  })
  @ApiResponse({ status: 200, description: '驗證結果', type: ValidateTokenResponse })
  async validate(
    @Body() request: ValidateTokenRequest,
    @Res() res: Response,
  ) {
    const result = await this.authService.validateTokenAsync(request.Token);

    // 設定回應標頭（供 API Gateway 使用）
    if (result.IsValid) {
      res.setHeader('X-User-Id', result.UserId?.toString() || '');
      res.setHeader('X-User-IdNo', result.IdNo || '');
      res.setHeader('X-User-Name', encodeURIComponent(result.Name || ''));
    }

    return res.status(HttpStatus.OK).json(ApiRes.success(result, result.IsValid ? '驗證成功' : '驗證失敗'));
  }

  /**
   * 取得當前使用者資訊
   */
  @Get('me')
  @ApiBearerAuth()
  @ApiOperation({ summary: '取得當前使用者', description: '取得當前登入使用者的資訊' })
  @ApiResponse({ status: 200, description: '使用者資訊' })
  @ApiResponse({ status: 401, description: '未授權' })
  async me(
    @CurrentUser() user: CurrentUserData,
    @Res() res: Response,
  ) {
    const currentUser = await this.authService.getCurrentUserAsync(user.userId);

    if (!currentUser) {
      return res.status(HttpStatus.NOT_FOUND).json(ApiRes.notFound('使用者不存在'));
    }

    const profileData: ProfileData = {
      Id: currentUser.Id,
      IdNo: currentUser.IdNo,
      Name: currentUser.Name,
    };

    return res.status(HttpStatus.OK).json(ApiRes.success(profileData));
  }

  /**
   * 取得客戶端 IP
   */
  private getClientIp(req: Request): string {
    const forwarded = req.headers['x-forwarded-for'];
    if (forwarded) {
      const ips = typeof forwarded === 'string' ? forwarded : forwarded[0];
      return ips.split(',')[0].trim();
    }
    return req.ip || req.socket.remoteAddress || 'unknown';
  }
}
