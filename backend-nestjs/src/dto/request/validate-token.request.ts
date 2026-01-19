import { ApiProperty } from '@nestjs/swagger';
import { IsNotEmpty, IsString } from 'class-validator';

export class ValidateTokenRequest {
  @ApiProperty({
    description: 'Access Token (JWT)',
    example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',
  })
  @IsNotEmpty({ message: 'Token 為必填欄位' })
  @IsString({ message: 'Token 必須是字串' })
  Token: string;
}
