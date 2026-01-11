from sqlalchemy import create_engine, event
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import QueuePool
from app.config import settings

# 建立基礎模型類別
Base = declarative_base()

# 資料庫引擎和會話工廠（延遲初始化）
engine = None
SessionLocal = None


def init_database():
    """
    初始化資料庫連接
    優化的連接池配置
    """
    global engine, SessionLocal

    if engine is not None:
        return

    # 建立資料庫引擎（優化配置）
    engine = create_engine(
        settings.database_url,
        echo=settings.DEBUG,
        # 連接池配置
        poolclass=QueuePool,
        pool_pre_ping=True,  # 連線池預檢查，自動重連斷開的連接
        pool_size=10,  # 連線池大小
        max_overflow=20,  # 超過連線池大小外最多建立的連線
        pool_recycle=3600,  # 連接回收時間（秒），避免長時間連接問題
        pool_timeout=30,  # 獲取連接的超時時間（秒）
        # 性能優化
        connect_args={
            "timeout": 10,  # 連接超時
            "autocommit": False,
        },
    )

    # 建立會話工廠
    SessionLocal = sessionmaker(
        autocommit=False,
        autoflush=False,
        bind=engine,
        expire_on_commit=False,  # 提交後不過期對象，提升性能
    )

    # 添加連接池事件監聽
    @event.listens_for(engine, "connect")
    def set_sqlite_pragma(dbapi_conn, connection_record):
        """連接時設置參數（可選）"""
        pass


def get_db():
    """
    獲取資料庫會話
    用於依賴注入
    """
    # 如果資料庫未初始化，嘗試初始化
    if SessionLocal is None:
        try:
            init_database()
        except Exception as e:
            # 如果資料庫連接失敗，返回 None（向後兼容）
            print(f"資料庫連接失敗: {str(e)}")
            yield None
            return

    if SessionLocal is None:
        yield None
        return

    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def init_db():
    """
    初始化資料庫
    建立所有資料表
    """
    try:
        init_database()
        if engine is None:
            return
        from app.models import user  # noqa: F401
        Base.metadata.create_all(bind=engine)
    except Exception as e:
        print(f"資料庫初始化失敗: {str(e)}")
