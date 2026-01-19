export interface JwtPayload {
  sub: number; // userId
  idNo: string;
  name: string;
  sessionId: string;
  jti: string; // token id
  targetSystem?: string;
  iat?: number;
  exp?: number;
  iss?: string;
  aud?: string | string[];
}

export interface TokenValidationResult {
  isValid: boolean;
  userId?: number;
  idNo?: string;
  name?: string;
  sessionId?: string;
  tokenId?: string;
  expiresAt?: Date;
  errorMessage?: string;
}
