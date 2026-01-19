import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';
import { IsBoolean, IsOptional, IsString } from 'class-validator';

export class RevokeTokenRequest {
  @ApiPropertyOptional({
    description: 'Refresh Token（如果不提供且已登入，將從 session 撤銷）',
    example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',
  })
  @IsOptional()
  @IsString({ message: 'Refresh Token 必須是字串' })
  RefreshToken?: string;

  @ApiPropertyOptional({
    description: '是否撤銷所有裝置的 Token',
    example: false,
    default: false,
  })
  @IsOptional()
  @IsBoolean({ message: 'RevokeAllDevices 必須是布林值' })
  RevokeAllDevices?: boolean;
}
