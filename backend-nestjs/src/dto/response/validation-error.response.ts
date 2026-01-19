import { ApiProperty } from '@nestjs/swagger';

export class ValidationErrorResponse {
  @ApiProperty({
    description: 'HTTP 狀態碼',
    example: 422,
  })
  Code: number;

  @ApiProperty({
    description: '錯誤訊息',
    example: '驗證失敗',
  })
  Message: string;

  @ApiProperty({
    description: '欄位錯誤詳情',
    example: {
      UserId: ['使用者 ID 為必填欄位'],
      Password: ['密碼長度至少需要 6 個字元'],
    },
  })
  Errors: Record<string, string[]>;

  constructor(errors: Record<string, string[]>) {
    this.Code = 422;
    this.Message = '驗證失敗';
    this.Errors = errors;
  }
}
