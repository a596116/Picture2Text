import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class ApiResponse<T> {
  @ApiProperty({
    description: 'HTTP 狀態碼',
    example: 200,
  })
  Code: number;

  @ApiProperty({
    description: '回應訊息',
    example: '操作成功',
  })
  Message: string;

  @ApiPropertyOptional({
    description: '回應資料',
  })
  Data?: T;

  constructor(code: number, message: string, data?: T) {
    this.Code = code;
    this.Message = message;
    this.Data = data;
  }

  static success<T>(data?: T, message = '操作成功'): ApiResponse<T> {
    return new ApiResponse(200, message, data);
  }

  static created<T>(data?: T, message = '建立成功'): ApiResponse<T> {
    return new ApiResponse(201, message, data);
  }

  static error(code: number, message: string): ApiResponse<null> {
    return new ApiResponse(code, message);
  }

  static badRequest(message = '請求參數錯誤'): ApiResponse<null> {
    return new ApiResponse(400, message);
  }

  static unauthorized(message = '未授權'): ApiResponse<null> {
    return new ApiResponse(401, message);
  }

  static forbidden(message = '禁止存取'): ApiResponse<null> {
    return new ApiResponse(403, message);
  }

  static notFound(message = '找不到資源'): ApiResponse<null> {
    return new ApiResponse(404, message);
  }

  static tooManyRequests(message = '請求過於頻繁，請稍後再試'): ApiResponse<null> {
    return new ApiResponse(429, message);
  }

  static internalError(message = '伺服器內部錯誤'): ApiResponse<null> {
    return new ApiResponse(500, message);
  }
}
