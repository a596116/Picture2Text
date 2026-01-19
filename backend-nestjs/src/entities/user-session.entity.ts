import {
  Entity,
  PrimaryGeneratedColumn,
  Column,
  ManyToOne,
  JoinColumn,
  Index,
} from 'typeorm';
import { User } from './user.entity';
import { RefreshToken } from './refresh-token.entity';

@Entity('UserSession')
@Index(['UserId', 'IsActive'])
export class UserSession {
  @PrimaryGeneratedColumn()
  Id: number;

  @Column()
  UserId: number;

  @Column({ type: 'nvarchar', length: 100 })
  @Index({ unique: true })
  SessionId: string;

  @Column({ nullable: true })
  RefreshTokenId: number | null;

  @Column({ type: 'nvarchar', length: 200, nullable: true })
  DeviceName: string | null;

  @Column({ type: 'nvarchar', length: 50, nullable: true })
  IpAddress: string | null;

  @Column({ type: 'nvarchar', length: 1000, nullable: true })
  UserAgent: string | null;

  @Column({ type: 'datetime' })
  LoginAt: Date;

  @Column({ type: 'datetime' })
  LastActivityAt: Date;

  @Column({ type: 'datetime', nullable: true })
  LogoutAt: Date | null;

  @Column({ type: 'datetime' })
  ExpiresAt: Date;

  @Column({ type: 'bit', default: true })
  IsActive: boolean;

  @ManyToOne(() => User, (user) => user.Sessions, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'UserId' })
  User: User;

  @ManyToOne(() => RefreshToken, { onDelete: 'SET NULL', nullable: true })
  @JoinColumn({ name: 'RefreshTokenId' })
  RefreshToken: RefreshToken | null;
}
