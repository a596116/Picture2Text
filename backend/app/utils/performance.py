"""性能優化工具"""
import time
from functools import wraps
from typing import Callable, Any
import logging

logger = logging.getLogger(__name__)


def measure_time(func: Callable) -> Callable:
    """
    測量函數執行時間的裝飾器

    Usage:
        @measure_time
        async def my_function():
            ...
    """
    @wraps(func)
    async def async_wrapper(*args, **kwargs):
        start_time = time.time()
        try:
            result = await func(*args, **kwargs)
            return result
        finally:
            elapsed = time.time() - start_time
            logger.info(f"{func.__name__} 執行時間: {elapsed:.3f}s")

    @wraps(func)
    def sync_wrapper(*args, **kwargs):
        start_time = time.time()
        try:
            result = func(*args, **kwargs)
            return result
        finally:
            elapsed = time.time() - start_time
            logger.info(f"{func.__name__} 執行時間: {elapsed:.3f}s")

    # 根據函數是否為協程返回對應的包裝器
    import asyncio
    if asyncio.iscoroutinefunction(func):
        return async_wrapper
    return sync_wrapper


def retry(max_attempts: int = 3, delay: float = 1.0):
    """
    重試裝飾器

    Args:
        max_attempts: 最大重試次數
        delay: 重試延遲（秒）

    Usage:
        @retry(max_attempts=3, delay=1.0)
        async def my_function():
            ...
    """
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        async def async_wrapper(*args, **kwargs):
            import asyncio
            last_exception = None
            for attempt in range(max_attempts):
                try:
                    return await func(*args, **kwargs)
                except Exception as e:
                    last_exception = e
                    if attempt < max_attempts - 1:
                        logger.warning(
                            f"{func.__name__} 第 {attempt + 1} 次嘗試失敗: {str(e)}，"
                            f"{delay} 秒後重試..."
                        )
                        await asyncio.sleep(delay)
                    else:
                        logger.error(f"{func.__name__} 所有重試均失敗")
            raise last_exception

        @wraps(func)
        def sync_wrapper(*args, **kwargs):
            import time
            last_exception = None
            for attempt in range(max_attempts):
                try:
                    return func(*args, **kwargs)
                except Exception as e:
                    last_exception = e
                    if attempt < max_attempts - 1:
                        logger.warning(
                            f"{func.__name__} 第 {attempt + 1} 次嘗試失敗: {str(e)}，"
                            f"{delay} 秒後重試..."
                        )
                        time.sleep(delay)
                    else:
                        logger.error(f"{func.__name__} 所有重試均失敗")
            raise last_exception

        import asyncio
        if asyncio.iscoroutinefunction(func):
            return async_wrapper
        return sync_wrapper

    return decorator
