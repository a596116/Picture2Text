import {
  Controller,
  Get,
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
import { Response } from 'express';
import { AuthService } from '../services';
import { ApiResponse as ApiRes, ProfileResponse, ProfileData } from '../dto/response';
import { CurrentUser, CurrentUserData } from '../decorators/current-user.decorator';
import { JwtAuthGuard } from '../guards';

@ApiTags('Profile')
@Controller('api/profile')
@UseGuards(JwtAuthGuard)
@ApiBearerAuth()
export class ProfileController {
  constructor(private readonly authService: AuthService) {}

  /**
   * 取得使用者個人資料
   */
  @Get()
  @ApiOperation({ summary: '取得個人資料', description: '取得當前登入使用者的個人資料' })
  @ApiResponse({ status: 200, description: '個人資料', type: ProfileResponse })
  @ApiResponse({ status: 401, description: '未授權' })
  @ApiResponse({ status: 404, description: '使用者不存在' })
  async getProfile(
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
}
