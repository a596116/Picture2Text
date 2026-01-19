import {
  ExceptionFilter,
  Catch,
  ArgumentsHost,
  BadRequestException,
  HttpStatus,
} from '@nestjs/common';
import { Response } from 'express';
import { ValidationErrorResponse } from '../dto/response';

@Catch(BadRequestException)
export class ValidationExceptionFilter implements ExceptionFilter {
  catch(exception: BadRequestException, host: ArgumentsHost) {
    const ctx = host.switchToHttp();
    const response = ctx.getResponse<Response>();
    const exceptionResponse = exception.getResponse() as any;

    // 檢查是否為驗證錯誤
    if (exceptionResponse.message && Array.isArray(exceptionResponse.message)) {
      const errors: Record<string, string[]> = {};

      // 將錯誤訊息分組到對應的欄位
      for (const message of exceptionResponse.message) {
        // class-validator 的錯誤格式通常是 "property message"
        // 嘗試提取屬性名稱
        const parts = message.split(' ');
        const property = parts[0] || 'general';

        if (!errors[property]) {
          errors[property] = [];
        }
        errors[property].push(message);
      }

      const validationError = new ValidationErrorResponse(errors);

      return response.status(HttpStatus.UNPROCESSABLE_ENTITY).json(validationError);
    }

    // 非驗證錯誤，回傳原始錯誤
    return response.status(exception.getStatus()).json({
      Code: exception.getStatus(),
      Message: exceptionResponse.message || '請求錯誤',
    });
  }
}
