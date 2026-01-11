from sqlalchemy import Column, Integer, String, Boolean, DateTime
from sqlalchemy.sql import func
from app.database import Base


class User(Base):
    """使用者模型"""

    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    username = Column(String(50), unique=True, index=True, nullable=False, comment="使用者名稱")
    email = Column(String(100), unique=True, index=True, nullable=False, comment="電子郵件")
    hashed_password = Column(String(255), nullable=False, comment="加密密碼")
    is_active = Column(Boolean, default=True, comment="是否啟用")
    is_superuser = Column(Boolean, default=False, comment="是否為超級使用者")
    created_at = Column(DateTime(timezone=True), server_default=func.now(), comment="建立時間")
    updated_at = Column(DateTime(timezone=True), server_default=func.now(), onupdate=func.now(), comment="更新時間")

    def __repr__(self):
        return f"<User(id={self.id}, username={self.username})>"
