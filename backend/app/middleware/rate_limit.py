"""請求限流中間件"""
import time
from collections import defaultdict
from typing import Dict, Tuple
from fastapi import Request, HTTPException, status
from starlette.middleware.base import BaseHTTPMiddleware


class RateLimitMiddleware(BaseHTTPMiddleware):
    """
    請求限流中間件
    基於 IP 地址的簡單限流實現
    """

    def __init__(self, app, requests_per_minute: int = 60):
        """
        初始化限流中間件

        Args:
            app: FastAPI 應用
            requests_per_minute: 每分鐘允許的請求數
        """
        super().__init__(app)
        self.requests_per_minute = requests_per_minute
        self.requests: Dict[str, list[float]] = defaultdict(list)

    async def dispatch(self, request: Request, call_next):
        """處理請求"""
        # 獲取客戶端 IP
        client_ip = request.client.host if request.client else "unknown"

        # 清理過期的請求記錄
        current_time = time.time()
        self.requests[client_ip] = [
            req_time
            for req_time in self.requests[client_ip]
            if current_time - req_time < 60
        ]

        # 檢查是否超過限制
        if len(self.requests[client_ip]) >= self.requests_per_minute:
            raise HTTPException(
                status_code=status.HTTP_429_TOO_MANY_REQUESTS,
                detail=f"請求過於頻繁，每分鐘最多 {self.requests_per_minute} 次請求"
            )

        # 記錄本次請求
        self.requests[client_ip].append(current_time)

        # 處理請求
        response = await call_next(request)
        return response
