from fastapi import APIRouter
from app.api.endpoints import users, invoices

# 建立 API 路由
api_router = APIRouter()

# 註冊各個模組的路由
api_router.include_router(users.router, prefix="/users", tags=["使用者管理"])
api_router.include_router(invoices.router, prefix="/invoice", tags=["發票管理"])

# 可以在這裡新增更多的路由
# api_router.include_router(auth.router, prefix="/auth", tags=["認證"])
# api_router.include_router(items.router, prefix="/items", tags=["物品管理"])
