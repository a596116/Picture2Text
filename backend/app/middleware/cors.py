from fastapi.middleware.cors import CORSMiddleware
from app.config import settings


def setup_cors(app):
    """
    配置 CORS 中介軟體

    Args:
        app: FastAPI 應用程式實例
    """
    app.add_middleware(
        CORSMiddleware,
        allow_origins=settings.BACKEND_CORS_ORIGINS,
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )
