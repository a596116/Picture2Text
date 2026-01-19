export default () => ({
  port: parseInt(process.env.PORT || '5000', 10),
  nodeEnv: process.env.NODE_ENV || 'development',
  database: {
    host: process.env.DB_HOST || 'localhost',
    port: parseInt(process.env.DB_PORT || '1433', 10),
    username: process.env.DB_USERNAME || 'sa',
    password: process.env.DB_PASSWORD || '',
    database: process.env.DB_DATABASE || 'testdb',
  },
  jwt: {
    secretKey: process.env.JWT_SECRET_KEY || 'your-secret-key-change-this-in-production-must-be-at-least-32-characters-long',
    issuer: process.env.JWT_ISSUER || 'AuthCenter.Api',
    audiences: (process.env.JWT_AUDIENCES || 'HRSystem,FinanceSystem,InventorySystem,ERPSystem,CRMSystem').split(','),
    expirationMinutes: parseInt(process.env.JWT_EXPIRATION_MINUTES || '30', 10),
    refreshTokenExpirationDays: parseInt(process.env.JWT_REFRESH_TOKEN_EXPIRATION_DAYS || '7', 10),
  },
  cors: {
    allowedOrigins: (process.env.CORS_ALLOWED_ORIGINS || 'http://localhost:3000').split(','),
  },
});
