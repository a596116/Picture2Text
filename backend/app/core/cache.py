"""緩存模組 - 支持內存緩存和 Redis（可選）"""
from typing import Optional, Any
from functools import wraps
import hashlib
import json
from app.config import settings

# 簡單的內存緩存實現
_memory_cache: dict[str, tuple[Any, float]] = {}


class CacheService:
    """緩存服務類"""

    def __init__(self, use_redis: bool = False, redis_url: Optional[str] = None):
        """
        初始化緩存服務

        Args:
            use_redis: 是否使用 Redis（需要安裝 redis 包）
            redis_url: Redis 連接 URL
        """
        self.use_redis = use_redis
        self.redis_client = None

        if use_redis:
            try:
                import redis
                self.redis_client = redis.from_url(redis_url or "redis://localhost:6379")
            except ImportError:
                print("警告: Redis 未安裝，使用內存緩存")
                self.use_redis = False

    def get(self, key: str) -> Optional[Any]:
        """獲取緩存值"""
        if self.use_redis and self.redis_client:
            try:
                value = self.redis_client.get(key)
                return json.loads(value) if value else None
            except Exception:
                return None
        else:
            # 使用內存緩存
            if key in _memory_cache:
                value, expire_time = _memory_cache[key]
                import time
                if expire_time > time.time():
                    return value
                else:
                    del _memory_cache[key]
            return None

    def set(self, key: str, value: Any, expire_seconds: int = 3600) -> bool:
        """設置緩存值"""
        try:
            if self.use_redis and self.redis_client:
                self.redis_client.setex(
                    key,
                    expire_seconds,
                    json.dumps(value, default=str)
                )
            else:
                # 使用內存緩存
                import time
                _memory_cache[key] = (value, time.time() + expire_seconds)
            return True
        except Exception as e:
            print(f"緩存設置失敗: {str(e)}")
            return False

    def delete(self, key: str) -> bool:
        """刪除緩存值"""
        try:
            if self.use_redis and self.redis_client:
                self.redis_client.delete(key)
            else:
                _memory_cache.pop(key, None)
            return True
        except Exception:
            return False

    def clear(self) -> bool:
        """清空所有緩存"""
        try:
            if self.use_redis and self.redis_client:
                self.redis_client.flushdb()
            else:
                _memory_cache.clear()
            return True
        except Exception:
            return False

    @staticmethod
    def generate_key(*args, **kwargs) -> str:
        """生成緩存鍵"""
        key_data = json.dumps({"args": args, "kwargs": kwargs}, sort_keys=True, default=str)
        return hashlib.md5(key_data.encode()).hexdigest()


# 創建全局緩存實例（默認使用內存緩存）
cache_service = CacheService()


def cached(expire_seconds: int = 3600):
    """
    緩存裝飾器

    Args:
        expire_seconds: 緩存過期時間（秒）
    """
    def decorator(func):
        @wraps(func)
        async def wrapper(*args, **kwargs):
            # 生成緩存鍵
            cache_key = f"{func.__module__}.{func.__name__}:{CacheService.generate_key(*args, **kwargs)}"
            
            # 嘗試從緩存獲取
            cached_value = cache_service.get(cache_key)
            if cached_value is not None:
                return cached_value

            # 執行函數並緩存結果
            result = await func(*args, **kwargs)
            cache_service.set(cache_key, result, expire_seconds)
            return result

        return wrapper
    return decorator
