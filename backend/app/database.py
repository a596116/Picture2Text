from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker
from app.config import settings

# 暫時停用資料庫連線
# # 建立資料庫引擎
# engine = create_engine(
#     settings.database_url,
#     echo=settings.DEBUG,
#     pool_pre_ping=True,  # 連線池預檢查
#     pool_size=10,  # 連線池大小
#     max_overflow=20,  # 超過連線池大小外最多建立的連線
# )

# # 建立會話工廠
# SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# 建立基礎模型類別
Base = declarative_base()


def get_db():
    """
    獲取資料庫會話
    用於依賴注入
    暫時停用資料庫連線
    """
    # db = SessionLocal()
    # try:
    #     yield db
    # finally:
    #     db.close()
    yield None


def init_db():
    """
    初始化資料庫
    建立所有資料表
    暫時停用
    """
    # from app.models import user  # noqa: F401
    # Base.metadata.create_all(bind=engine)
    pass
