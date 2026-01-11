from typing import List
from pydantic_settings import BaseSettings
from pydantic import field_validator
import json


class Settings(BaseSettings):
    """應用配置類別"""

    # 應用程式設定
    APP_NAME: str = "FastAPI Application"
    APP_VERSION: str = "1.0.0"
    DEBUG: bool = True
    PORT: int = 8000
    API_PREFIX: str = "/api"

    # 資料庫設定
    DB_DRIVER: str = "ODBC Driver 17 for SQL Server"
    DB_SERVER: str = "localhost"
    DB_PORT: int = 1433
    DB_NAME: str = "testdb"
    DB_USER: str = "sa"
    DB_PASSWORD: str = "password"

    # JWT 設定
    SECRET_KEY: str = "your-secret-key-change-this"
    ALGORITHM: str = "HS256"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 30

    # CORS 設定
    BACKEND_CORS_ORIGINS: List[str] = ["http://localhost:3000", "http://localhost:8000"]

    @field_validator("BACKEND_CORS_ORIGINS", mode="before")
    @classmethod
    def assemble_cors_origins(cls, v):
        if isinstance(v, str):
            return json.loads(v)
        return v

    # 分頁設定
    DEFAULT_PAGE_SIZE: int = 10
    MAX_PAGE_SIZE: int = 100

    # 緩存設定
    CACHE_ENABLED: bool = True
    CACHE_EXPIRE_SECONDS: int = 3600  # 緩存過期時間（秒）
    REDIS_ENABLED: bool = False
    REDIS_URL: str = "redis://localhost:6379"

    # 限流設定
    RATE_LIMIT_ENABLED: bool = True
    RATE_LIMIT_REQUESTS_PER_MINUTE: int = 60

    # AI 服務設定
    AI_SERVICE_TYPE: str = "openai"  # "openai" 或 "ollama"
    
    # OpenAI 設定
    OPENAI_API_KEY: str = ""
    OPENAI_MODEL: str = "gpt-4o"  # 支援視覺的模型
    OPENAI_BASE_URL: str = "https://api.openai.com/v1"  # OpenAI API 基礎 URL
    
    # Ollama 設定
    OLLAMA_BASE_URL: str = "http://localhost:11434/v1"  # Ollama API 基礎 URL
    OLLAMA_MODEL: str = "llava"  # Ollama 模型名稱（支援視覺的模型，如 llava）

    @property
    def database_url(self) -> str:
        """建構資料庫連線字串"""
        return (
            f"mssql+pyodbc://{self.DB_USER}:{self.DB_PASSWORD}"
            f"@{self.DB_SERVER}:{self.DB_PORT}/{self.DB_NAME}"
            f"?driver={self.DB_DRIVER.replace(' ', '+')}"
        )

    class Config:
        env_file = ".env"
        case_sensitive = True


settings = Settings()
