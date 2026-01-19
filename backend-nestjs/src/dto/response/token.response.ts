import { ApiProperty } from '@nestjs/swagger';

export class TokenData {
  @ApiProperty({
    description: 'Access Token (JWT)',
    example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',
  })
  AccessToken: string;

  @ApiProperty({
    description: 'Refresh Token',
    example: 'dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...',
  })
  RefreshToken: string;

  @ApiProperty({
    description: 'Token 類型',
    example: 'Bearer',
  })
  TokenType: string;

  @ApiProperty({
    description: 'Access Token 過期時間',
    example: '2024-01-15T10:30:00.000Z',
  })
  ExpiresAt: Date;

  @ApiProperty({
    description: 'Refresh Token 過期時間',
    example: '2024-01-22T10:00:00.000Z',
  })
  RefreshTokenExpiresAt: Date;

  @ApiProperty({
    description: 'Session ID',
    example: '550e8400-e29b-41d4-a716-446655440000',
  })
  SessionId: string;
}

export class TokenResponse {
  @ApiProperty()
  Code: number;

  @ApiProperty()
  Message: string;

  @ApiProperty({ type: TokenData })
  Data: TokenData;
}
