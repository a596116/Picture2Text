from datetime import datetime, timedelta
from typing import Any, Optional, Union
from jose import jwt
from passlib.context import CryptContext
from app.config import settings

# 密碼加密上下文
pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")


def create_access_token(subject: Union[str, Any], expires_delta: Optional[timedelta] = None) -> str:
    """
    建立存取權杖

    Args:
        subject: 權杖主題（通常是使用者 ID 或使用者名稱）
        expires_delta: 過期時間增量

    Returns:
        編碼後的 JWT 權杖
    """
    if expires_delta:
        expire = datetime.utcnow() + expires_delta
    else:
        expire = datetime.utcnow() + timedelta(
            minutes=settings.ACCESS_TOKEN_EXPIRE_MINUTES
        )

    to_encode = {"exp": expire, "sub": str(subject)}
    encoded_jwt = jwt.encode(to_encode, settings.SECRET_KEY, algorithm=settings.ALGORITHM)
    return encoded_jwt


def verify_password(plain_password: str, hashed_password: str) -> bool:
    """
    驗證密碼

    Args:
        plain_password: 明文密碼
        hashed_password: 雜湊密碼

    Returns:
        是否匹配
    """
    return pwd_context.verify(plain_password, hashed_password)


def get_password_hash(password: str) -> str:
    """
    獲取密碼雜湊值

    Args:
        password: 明文密碼

    Returns:
        雜湊後的密碼
    """
    return pwd_context.hash(password)
