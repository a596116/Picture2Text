import { ApiProperty } from '@nestjs/swagger';

export class ProfileData {
  @ApiProperty({
    description: '使用者 ID',
    example: 1,
  })
  Id: number;

  @ApiProperty({
    description: '使用者身分證號/員工編號',
    example: 'A123456789',
  })
  IdNo: string;

  @ApiProperty({
    description: '使用者姓名',
    example: '張三',
  })
  Name: string;
}

export class ProfileResponse {
  @ApiProperty()
  Code: number;

  @ApiProperty()
  Message: string;

  @ApiProperty({ type: ProfileData })
  Data: ProfileData;
}
