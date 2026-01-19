import { Injectable, UnauthorizedException } from '@nestjs/common';
import { PassportStrategy } from '@nestjs/passport';
import { ExtractJwt, Strategy } from 'passport-jwt';
import { ConfigService } from '@nestjs/config';
import { JwtPayload } from '../interfaces';

@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
  constructor(private readonly configService: ConfigService) {
    super({
      jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
      ignoreExpiration: false,
      secretOrKey: configService.get<string>('jwt.secretKey'),
      issuer: configService.get<string>('jwt.issuer'),
      audience: configService.get<string[]>('jwt.audiences'),
    });
  }

  async validate(payload: JwtPayload) {
    if (!payload.sub) {
      throw new UnauthorizedException('Token 無效');
    }

    return {
      userId: payload.sub,
      idNo: payload.idNo,
      name: payload.name,
      sessionId: payload.sessionId,
      tokenId: payload.jti,
    };
  }
}
