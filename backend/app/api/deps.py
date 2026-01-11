from typing import Generator
from sqlalchemy.orm import Session
from app.database import get_db

# 重新匯出 get_db 方便使用
def get_database() -> Generator:
    """
    資料庫依賴注入
    """
    return get_db()
