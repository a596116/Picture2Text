from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from fastapi.exceptions import RequestValidationError
from app.config import settings
from app.api.router import api_router
from app.middleware.logging import LoggingMiddleware
from app.middleware.cors import setup_cors
from app.middleware.rate_limit import RateLimitMiddleware
from app.core.exceptions import CustomException

# 建立 FastAPI 應用程式實例
app = FastAPI(
    title=settings.APP_NAME,
    version=settings.APP_VERSION,
    description="基於 FastAPI + SQLAlchemy + MSSQL 的後端應用",
    docs_url=f"{settings.API_PREFIX}/docs",
    redoc_url=f"{settings.API_PREFIX}/redoc",
    openapi_url=f"{settings.API_PREFIX}/openapi.json",
)

# 配置 CORS 中介軟體
setup_cors(app)

# 新增日誌中介軟體（最外層，最後執行）
app.add_middleware(LoggingMiddleware)

# 新增請求限流中介軟體（在日誌中間件之前）
app.add_middleware(RateLimitMiddleware, requests_per_minute=60)


# 自訂異常處理
@app.exception_handler(CustomException)
async def custom_exception_handler(request: Request, exc: CustomException):
    """處理自訂異常"""
    return JSONResponse(
        status_code=exc.status_code,
        content={
            "code": exc.status_code,
            "message": exc.detail,
            "data": None,
        },
    )


@app.exception_handler(RequestValidationError)
async def validation_exception_handler(request: Request, exc: RequestValidationError):
    """處理請求驗證異常"""
    return JSONResponse(
        status_code=422,
        content={
            "code": 422,
            "message": "請求參數驗證失敗",
            "data": exc.errors(),
        },
    )


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    """處理全域異常"""
    return JSONResponse(
        status_code=500,
        content={
            "code": 500,
            "message": f"伺服器內部錯誤: {str(exc)}",
            "data": None,
        },
    )


# 註冊 API 路由
app.include_router(api_router, prefix=settings.API_PREFIX)


# # 根路徑
# @app.get("/", tags=["根路徑"])
# async def root():
#     """根路徑，返回應用資訊"""
#     return {
#         "app_name": settings.APP_NAME,
#         "version": settings.APP_VERSION,
#         "docs_url": f"{settings.API_PREFIX}/docs",
#     }


# # 健康檢查
# @app.get("/health", tags=["健康檢查"])
# async def health_check():
#     """健康檢查介面"""
#     return {"status": "healthy"}


# 啟動事件
@app.on_event("startup")
async def startup_event():
    """應用啟動時執行"""
    print(f"{settings.APP_NAME} v{settings.APP_VERSION} 啟動成功！")
    print(f"Swagger 文件地址: http://localhost:{settings.PORT}{settings.API_PREFIX}/docs")


# 關閉事件
@app.on_event("shutdown")
async def shutdown_event():
    """應用關閉時執行"""
    print(f"{settings.APP_NAME} 已關閉")


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        reload=settings.DEBUG,
    )
