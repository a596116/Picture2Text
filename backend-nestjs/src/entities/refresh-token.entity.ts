import {
  Entity,
  PrimaryGeneratedColumn,
  Column,
  ManyToOne,
  JoinColumn,
  Index,
  CreateDateColumn,
} from 'typeorm';
import { User } from './user.entity';

@Entity('RefreshToken')
@Index(['UserId', 'IsRevoked'])
export class RefreshToken {
  @PrimaryGeneratedColumn()
  Id: number;

  @Column()
  UserId: number;

  @Column({ type: 'nvarchar', length: 500 })
  @Index()
  Token: string;

  @Column({ type: 'nvarchar', length: 100 })
  @Index({ unique: true })
  TokenId: string;

  @CreateDateColumn()
  CreatedAt: Date;

  @Column({ type: 'datetime' })
  ExpiresAt: Date;

  @Column({ type: 'bit', default: false })
  IsRevoked: boolean;

  @Column({ type: 'datetime', nullable: true })
  RevokedAt: Date | null;

  @Column({ type: 'nvarchar', length: 500, nullable: true })
  ReplacedByToken: string | null;

  @Column({ type: 'nvarchar', length: 500, nullable: true })
  DeviceInfo: string | null;

  @Column({ type: 'nvarchar', length: 50, nullable: true })
  IpAddress: string | null;

  @Column({ type: 'nvarchar', length: 1000, nullable: true })
  UserAgent: string | null;

  @Column({ type: 'datetime', nullable: true })
  LastUsedAt: Date | null;

  @ManyToOne(() => User, (user) => user.RefreshTokens, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'UserId' })
  User: User;

  /**
   * 檢查 Token 是否有效（未撤銷且未過期）
   */
  get IsActive(): boolean {
    return !this.IsRevoked && new Date() < this.ExpiresAt;
  }
}
