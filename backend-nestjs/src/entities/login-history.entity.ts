import {
  Entity,
  PrimaryGeneratedColumn,
  Column,
  ManyToOne,
  JoinColumn,
  Index,
} from 'typeorm';
import { User } from './user.entity';

@Entity('LoginHistory')
@Index(['UserId', 'IsSuccess'])
export class LoginHistory {
  @PrimaryGeneratedColumn()
  Id: number;

  @Column({ nullable: true })
  @Index()
  UserId: number | null;

  @Column({ type: 'nvarchar', length: 50 })
  AttemptedUserId: string;

  @Column({ type: 'bit' })
  IsSuccess: boolean;

  @Column({ type: 'nvarchar', length: 200, nullable: true })
  FailureReason: string | null;

  @Column({ type: 'nvarchar', length: 50, nullable: true })
  IpAddress: string | null;

  @Column({ type: 'nvarchar', length: 1000, nullable: true })
  UserAgent: string | null;

  @Column({ type: 'nvarchar', length: 500, nullable: true })
  DeviceInfo: string | null;

  @Column({ type: 'datetime' })
  @Index()
  AttemptedAt: Date;

  @Column({ type: 'nvarchar', length: 200, nullable: true })
  Location: string | null;

  @ManyToOne(() => User, (user) => user.LoginHistories, {
    onDelete: 'SET NULL',
    nullable: true,
  })
  @JoinColumn({ name: 'UserId' })
  User: User | null;
}
