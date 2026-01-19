import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { JwtModule } from '@nestjs/jwt';
import { PassportModule } from '@nestjs/passport';
import { ScheduleModule } from '@nestjs/schedule';
import { APP_GUARD, APP_FILTER } from '@nestjs/core';

// Config
import { configuration } from './config';

// Entities
import { User, RefreshToken, UserSession, LoginHistory } from './entities';

// Services
import {
  JwtTokenService,
  RefreshTokenService,
  SessionService,
  LoginHistoryService,
  AuthService,
  TokenCleanupService,
} from './services';

// Controllers
import { AuthController, SessionController, ProfileController } from './controllers';

// Guards & Strategy
import { JwtAuthGuard, JwtStrategy } from './guards';

// Filters
import { HttpExceptionFilter, ValidationExceptionFilter } from './filters';

@Module({
  imports: [
    // 設定模組
    ConfigModule.forRoot({
      isGlobal: true,
      load: [configuration],
      envFilePath: ['.env', '.env.local'],
    }),

    // 資料庫模組
    TypeOrmModule.forRootAsync({
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: (configService: ConfigService) => ({
        type: 'mssql',
        host: configService.get<string>('database.host'),
        port: configService.get<number>('database.port'),
        username: configService.get<string>('database.username'),
        password: configService.get<string>('database.password'),
        database: configService.get<string>('database.database'),
        entities: [User, RefreshToken, UserSession, LoginHistory],
        synchronize: false, // 生產環境請設為 false
        logging: configService.get<string>('nodeEnv') === 'development',
        options: {
          encrypt: false,
          trustServerCertificate: true,
        },
      }),
    }),

    // Entity Repository
    TypeOrmModule.forFeature([User, RefreshToken, UserSession, LoginHistory]),

    // Passport 模組
    PassportModule.register({ defaultStrategy: 'jwt' }),

    // JWT 模組
    JwtModule.registerAsync({
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: (configService: ConfigService) => ({
        secret: configService.get<string>('jwt.secretKey'),
        signOptions: {
          expiresIn: `${configService.get<number>('jwt.expirationMinutes')}m`,
          issuer: configService.get<string>('jwt.issuer'),
          audience: configService.get<string[]>('jwt.audiences'),
        },
      }),
    }),

    // 排程模組
    ScheduleModule.forRoot(),
  ],
  controllers: [AuthController, SessionController, ProfileController],
  providers: [
    // Services
    JwtTokenService,
    RefreshTokenService,
    SessionService,
    LoginHistoryService,
    AuthService,
    TokenCleanupService,

    // Strategy
    JwtStrategy,

    // Global Guard
    {
      provide: APP_GUARD,
      useClass: JwtAuthGuard,
    },

    // Global Filters
    {
      provide: APP_FILTER,
      useClass: HttpExceptionFilter,
    },
    {
      provide: APP_FILTER,
      useClass: ValidationExceptionFilter,
    },
  ],
})
export class AppModule {}
