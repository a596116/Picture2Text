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
    
    注意：此方法使用 create_all()，如果表已存在則不會報錯，但也不會更新表結構。
    建議使用 Alembic migration 來管理資料庫變更。
    詳見 MIGRATION.md
    """
    try:
        init_database()
        if engine is None:
            return
        from app.models import user  # noqa: F401
        
        # 檢查表是否已存在
        from sqlalchemy import inspect
        inspector = inspect(engine)
        existing_tables = inspector.get_table_names()
        
        # 只創建不存在的表
        Base.metadata.create_all(bind=engine)
        
        if existing_tables:
            print(f"警告：資料庫中已存在以下表: {', '.join(existing_tables)}")
            print("create_all() 不會更新現有表的結構。")
            print("如需更新表結構，請使用 Alembic migration。")
        else:
            print("所有表已成功創建。")
    except Exception as e:
        print(f"資料庫初始化失敗: {str(e)}")
