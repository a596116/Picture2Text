import { NestFactory } from '@nestjs/core';
import { ValidationPipe, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { SwaggerModule, DocumentBuilder } from '@nestjs/swagger';
import { AppModule } from './app.module';

async function bootstrap() {
  const logger = new Logger('Bootstrap');
  const app = await NestFactory.create(AppModule);

  const configService = app.get(ConfigService);
  const port = configService.get<number>('port') || 5000;
  const nodeEnv = configService.get<string>('nodeEnv') || 'development';
  const allowedOrigins = configService.get<string[]>('cors.allowedOrigins') || [];

  // 全域驗證 Pipe
  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      forbidNonWhitelisted: true,
      transform: true,
      transformOptions: {
        enableImplicitConversion: true,
      },
    }),
  );

  // CORS 設定
  if (nodeEnv === 'development') {
    app.enableCors({
      origin: true,
      credentials: true,
    });
    logger.log('CORS enabled for all origins (development mode)');
  } else {
    app.enableCors({
      origin: allowedOrigins,
      credentials: true,
    });
    logger.log(`CORS enabled for: ${allowedOrigins.join(', ')}`);
  }

  // Swagger 設定
  const swaggerConfig = new DocumentBuilder()
    .setTitle('Auth Center API')
    .setDescription('微服務認證中心 API 文件')
    .setVersion('1.0')
    .addBearerAuth(
      {
        type: 'http',
        scheme: 'bearer',
        bearerFormat: 'JWT',
        name: 'JWT',
        description: '輸入 JWT Token',
        in: 'header',
      },
      'JWT-auth',
    )
    .addTag('Auth', '認證相關 API')
    .addTag('Session', 'Session 管理 API')
    .addTag('Profile', '使用者資料 API')
    .build();

  const document = SwaggerModule.createDocument(app, swaggerConfig);
  SwaggerModule.setup('swagger', app, document, {
    swaggerOptions: {
      persistAuthorization: true,
      docExpansion: 'none',
      filter: true,
      showRequestDuration: true,
    },
  });

  await app.listen(port);

  logger.log(`Application is running on: http://localhost:${port}`);
  logger.log(`Swagger documentation: http://localhost:${port}/swagger`);
  logger.log(`Environment: ${nodeEnv}`);
}

bootstrap();
