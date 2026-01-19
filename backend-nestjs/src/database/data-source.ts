import { DataSource } from 'typeorm';
import { User, RefreshToken, UserSession, LoginHistory } from '../entities';

export const AppDataSource = new DataSource({
  type: 'mssql',
  host: process.env.DB_HOST || 'localhost',
  port: parseInt(process.env.DB_PORT || '1433', 10),
  username: process.env.DB_USERNAME || 'sa',
  password: process.env.DB_PASSWORD || '',
  database: process.env.DB_DATABASE || 'testdb',
  entities: [User, RefreshToken, UserSession, LoginHistory],
  migrations: ['src/database/migrations/*.ts'],
  synchronize: false,
  logging: true,
  options: {
    encrypt: false,
    trustServerCertificate: true,
  },
});
