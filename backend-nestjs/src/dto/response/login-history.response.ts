import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class LoginHistoryInfo {
  @ApiProperty({
    description: '記錄 ID',
    example: 1,
  })
  Id: number;

  @ApiProperty({
    description: '是否登入成功',
    example: true,
  })
  IsSuccess: boolean;

  @ApiPropertyOptional({
    description: '失敗原因',
    example: '密碼錯誤',
  })
  FailureReason?: string;

  @ApiPropertyOptional({
    description: 'IP 位址',
    example: '192.168.1.1',
  })
  IpAddress?: string;

  @ApiPropertyOptional({
    description: '裝置資訊',
    example: 'Chrome on Windows',
  })
  DeviceInfo?: string;

  @ApiPropertyOptional({
    description: '登入位置',
    example: '台北市',
  })
  Location?: string;

  @ApiProperty({
    description: '嘗試時間',
    example: '2024-01-15T10:00:00.000Z',
  })
  AttemptedAt: Date;
}

export class LoginHistoryResponse {
  @ApiProperty()
  Code: number;

  @ApiProperty()
  Message: string;

  @ApiProperty({ type: [LoginHistoryInfo] })
  Data: LoginHistoryInfo[];
}
