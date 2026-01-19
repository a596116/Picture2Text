import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class ValidateTokenData {
  @ApiProperty({
    description: 'Token 是否有效',
    example: true,
  })
  IsValid: boolean;

  @ApiPropertyOptional({
    description: '使用者 ID',
    example: 1,
  })
  UserId?: number;

  @ApiPropertyOptional({
    description: '使用者身分證號/員工編號',
    example: 'A123456789',
  })
  IdNo?: string;

  @ApiPropertyOptional({
    description: '使用者姓名',
    example: '張三',
  })
  Name?: string;

  @ApiPropertyOptional({
    description: 'Token 過期時間',
    example: '2024-01-15T10:30:00.000Z',
  })
  ExpiresAt?: Date;

  @ApiPropertyOptional({
    description: 'Token ID (JTI)',
    example: '550e8400-e29b-41d4-a716-446655440000',
  })
  TokenId?: string;

  @ApiPropertyOptional({
    description: '錯誤訊息（當 Token 無效時）',
    example: 'Token 已過期',
  })
  ErrorMessage?: string;
}

export class ValidateTokenResponse {
  @ApiProperty()
  Code: number;

  @ApiProperty()
  Message: string;

  @ApiProperty({ type: ValidateTokenData })
  Data: ValidateTokenData;
}
