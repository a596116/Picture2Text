import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class SessionInfo {
  @ApiProperty({
    description: 'Session ID',
    example: '550e8400-e29b-41d4-a716-446655440000',
  })
  SessionId: string;

  @ApiPropertyOptional({
    description: '裝置名稱',
    example: 'Chrome on Windows',
  })
  DeviceName?: string;

  @ApiPropertyOptional({
    description: 'IP 位址',
    example: '192.168.1.1',
  })
  IpAddress?: string;

  @ApiProperty({
    description: '登入時間',
    example: '2024-01-15T10:00:00.000Z',
  })
  LoginAt: Date;

  @ApiProperty({
    description: '最後活動時間',
    example: '2024-01-15T10:30:00.000Z',
  })
  LastActivityAt: Date;

  @ApiProperty({
    description: '過期時間',
    example: '2024-01-22T10:00:00.000Z',
  })
  ExpiresAt: Date;

  @ApiProperty({
    description: '是否為當前 Session',
    example: true,
  })
  IsCurrent: boolean;
}

export class SessionListResponse {
  @ApiProperty()
  Code: number;

  @ApiProperty()
  Message: string;

  @ApiProperty({ type: [SessionInfo] })
  Data: SessionInfo[];
}
