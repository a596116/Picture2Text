from typing import Optional
from datetime import datetime
from pydantic import BaseModel, EmailStr, Field


# 共享屬性
class UserBase(BaseModel):
    """使用者基礎模型"""

    username: str = Field(..., min_length=3, max_length=50, description="使用者名稱")
    email: EmailStr = Field(..., description="電子郵件")


# 建立使用者時的屬性
class UserCreate(UserBase):
    """建立使用者模型"""

    password: str = Field(..., min_length=6, max_length=50, description="密碼")


# 更新使用者時的屬性
class UserUpdate(BaseModel):
    """更新使用者模型"""

    username: Optional[str] = Field(None, min_length=3, max_length=50, description="使用者名稱")
    email: Optional[EmailStr] = Field(None, description="電子郵件")
    password: Optional[str] = Field(None, min_length=6, max_length=50, description="密碼")
    is_active: Optional[bool] = Field(None, description="是否啟用")


# 資料庫中的屬性
class UserInDB(UserBase):
    """資料庫使用者模型"""

    id: int = Field(..., description="使用者 ID")
    is_active: bool = Field(..., description="是否啟用")
    is_superuser: bool = Field(..., description="是否為超級使用者")
    created_at: datetime = Field(..., description="建立時間")
    updated_at: datetime = Field(..., description="更新時間")

    class Config:
        from_attributes = True


# 返回給客戶端的屬性
class User(UserInDB):
    """使用者回應模型"""

    pass


# 單一使用者回應
class UserResponse(BaseModel):
    """單一使用者回應模型"""

    code: int = Field(200, description="回應代碼")
    message: str = Field("操作成功", description="回應訊息")
    data: User = Field(..., description="使用者資料")


# 使用者清單回應
class UsersListResponse(BaseModel):
    """使用者清單回應模型"""

    code: int = Field(200, description="回應代碼")
    message: str = Field("查詢成功", description="回應訊息")
    data: list[User] = Field(..., description="使用者清單")
    total: int = Field(..., description="總筆數")
    page: int = Field(..., description="目前頁碼")
    page_size: int = Field(..., description="每頁筆數")
