import time
import logging
from fastapi import Request
from starlette.middleware.base import BaseHTTPMiddleware

# 配置日誌
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)

logger = logging.getLogger(__name__)


class LoggingMiddleware(BaseHTTPMiddleware):
    """
    日誌中介軟體
    記錄每個請求的詳細資訊
    """

    async def dispatch(self, request: Request, call_next):
        # 記錄請求開始時間
        start_time = time.time()

        # 記錄請求資訊
        logger.info(f"請求開始: {request.method} {request.url.path}")
        logger.debug(f"請求標頭: {request.headers}")

        # 處理請求
        response = await call_next(request)

        # 計算處理時間
        process_time = time.time() - start_time

        # 記錄回應資訊
        logger.info(
            f"請求完成: {request.method} {request.url.path} - "
            f"狀態碼: {response.status_code} - 耗時: {process_time:.3f}s"
        )

        # 新增處理時間到回應標頭
        response.headers["X-Process-Time"] = str(process_time)

        return response
