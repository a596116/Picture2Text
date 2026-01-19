import {
  Entity,
  PrimaryGeneratedColumn,
  Column,
  OneToMany,
  Index,
} from 'typeorm';
import { RefreshToken } from './refresh-token.entity';
import { UserSession } from './user-session.entity';
import { LoginHistory } from './login-history.entity';

@Entity('User')
export class User {
  @PrimaryGeneratedColumn()
  Id: number;

  @Column({ type: 'nvarchar', length: 50 })
  @Index({ unique: true })
  IdNo: string;

  @Column({ type: 'nvarchar', length: 100 })
  Name: string;

  @Column({ type: 'nvarchar', length: 255 })
  Password: string;

  @OneToMany(() => RefreshToken, (refreshToken) => refreshToken.User)
  RefreshTokens: RefreshToken[];

  @OneToMany(() => UserSession, (session) => session.User)
  Sessions: UserSession[];

  @OneToMany(() => LoginHistory, (history) => history.User)
  LoginHistories: LoginHistory[];
}
