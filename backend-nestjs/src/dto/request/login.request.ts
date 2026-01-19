import { ApiProperty } from '@nestjs/swagger';
import { IsNotEmpty, IsNumber, IsString, MinLength, MaxLength } from 'class-validator';

export class LoginRequest {
  @ApiProperty({
    description: '使用者 ID',
    example: 1,
  })
  @IsNotEmpty({ message: '使用者 ID 為必填欄位' })
  @IsNumber({}, { message: '使用者 ID 必須是數字' })
  UserId: number;

  @ApiProperty({
    description: '密碼',
    example: 'password123',
    minLength: 6,
    maxLength: 255,
  })
  @IsNotEmpty({ message: '密碼為必填欄位' })
  @IsString({ message: '密碼必須是字串' })
  @MinLength(6, { message: '密碼長度至少需要 6 個字元' })
  @MaxLength(255, { message: '密碼長度不能超過 255 個字元' })
  Password: string;
}
