import {
  Controller,
  Get,
  Delete,
  Param,
  Query,
  Res,
  HttpStatus,
  UseGuards,
} from '@nestjs/common';
import {
  ApiTags,
  ApiOperation,
  ApiResponse,
  ApiBearerAuth,
  ApiParam,
  ApiQuery,
} from '@nestjs/swagger';
import { Response } from 'express';
import { SessionService, LoginHistoryService, RefreshTokenService } from '../services';
import { ApiResponse as ApiRes, SessionListResponse, LoginHistoryResponse } from '../dto/response';
import { CurrentUser, CurrentUserData } from '../decorators/current-user.decorator';
import { JwtAuthGuard } from '../guards';

@ApiTags('Session')
@Controller('api/session')
@UseGuards(JwtAuthGuard)
@ApiBearerAuth()
export class SessionController {
  constructor(
    private readonly sessionService: SessionService,
    private readonly loginHistoryService: LoginHistoryService,
    private readonly refreshTokenService: RefreshTokenService,
  ) {}

  /**
   * 取得使用者的活躍 Session 列表
   */
  @Get('active')
  @ApiOperation({ summary: '取得活躍 Session', description: '取得當前使用者的所有活躍 Session' })
  @ApiResponse({ status: 200, description: 'Session 列表', type: SessionListResponse })
  @ApiResponse({ status: 401, description: '未授權' })
  async getActiveSessions(
    @CurrentUser() user: CurrentUserData,
    @Res() res: Response,
  ) {
    const sessions = await this.sessionService.getUserActiveSessionsAsync(
      user.userId,
      user.sessionId,
    );

    return res.status(HttpStatus.OK).json(ApiRes.success(sessions));
  }

  /**
   * 結束指定的 Session
   */
  @Delete(':sessionId')
  @ApiOperation({ summary: '結束 Session', description: '結束指定的 Session（登出該裝置）' })
  @ApiParam({ name: 'sessionId', description: 'Session ID' })
  @ApiResponse({ status: 200, description: '結束成功' })
  @ApiResponse({ status: 401, description: '未授權' })
  @ApiResponse({ status: 404, description: 'Session 不存在' })
  async endSession(
    @Param('sessionId') sessionId: string,
    @CurrentUser() user: CurrentUserData,
    @Res() res: Response,
  ) {
    // 取得 Session 以獲取 RefreshTokenId
    const session = await this.sessionService.getSessionByIdAsync(sessionId);

    if (!session || session.UserId !== user.userId) {
      return res.status(HttpStatus.NOT_FOUND).json(ApiRes.notFound('Session 不存在或無權限'));
    }

    // 結束 Session
    const success = await this.sessionService.endSessionAsync(sessionId, user.userId);

    if (!success) {
      return res.status(HttpStatus.NOT_FOUND).json(ApiRes.notFound('Session 不存在'));
    }

    // 撤銷對應的 Refresh Token
    if (session.RefreshTokenId) {
      const refreshToken = await this.refreshTokenService.getByTokenIdAsync(
        session.RefreshToken?.TokenId || '',
      );
      if (refreshToken) {
        await this.refreshTokenService.revokeRefreshTokenAsync(refreshToken.Token);
      }
    }

    return res.status(HttpStatus.OK).json(ApiRes.success(null, 'Session 已結束'));
  }

  /**
   * 取得登入歷史記錄
   */
  @Get('history')
  @ApiOperation({ summary: '取得登入歷史', description: '取得當前使用者的登入歷史記錄' })
  @ApiQuery({ name: 'limit', required: false, description: '回傳筆數（預設 20）' })
  @ApiResponse({ status: 200, description: '登入歷史', type: LoginHistoryResponse })
  @ApiResponse({ status: 401, description: '未授權' })
  async getLoginHistory(
    @Query('limit') limit: string | undefined,
    @CurrentUser() user: CurrentUserData,
    @Res() res: Response,
  ) {
    const historyLimit = limit ? parseInt(limit, 10) : 20;
    const history = await this.loginHistoryService.getUserLoginHistoryAsync(
      user.userId,
      historyLimit,
    );

    return res.status(HttpStatus.OK).json(ApiRes.success(history));
  }
}
