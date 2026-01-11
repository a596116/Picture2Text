from typing import Any
from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session
from app.api.deps import get_database
from app.crud.crud_user import crud_user
from app.schemas.user import User, UserCreate, UserUpdate, UserResponse, UsersListResponse
from app.core.response import success, paginated_response
from app.core.exceptions import NotFoundException, ConflictException
from app.config import settings

router = APIRouter()


@router.get("/", summary="獲取使用者清單", response_model=UsersListResponse)
def get_users(
    page: int = Query(1, ge=1, description="頁碼"),
    page_size: int = Query(
        settings.DEFAULT_PAGE_SIZE,
        ge=1,
        le=settings.MAX_PAGE_SIZE,
        description="每頁數量",
    ),
    db: Session = Depends(get_database),
) -> Any:
    """
    獲取使用者清單（分頁）

    - **page**: 頁碼，從 1 開始
    - **page_size**: 每頁數量
    """
    from datetime import datetime

    # 返回假資料
    fake_users = [
        {
            "id": 1,
            "username": "張小三",
            "email": "zhangsan@example.com",
            "is_active": True,
            "is_superuser": False,
            "created_at": datetime.now().isoformat(),
            "updated_at": datetime.now().isoformat(),
        },
        {
            "id": 2,
            "username": "李小四",
            "email": "lisi@example.com",
            "is_active": True,
            "is_superuser": False,
            "created_at": datetime.now().isoformat(),
            "updated_at": datetime.now().isoformat(),
        },
        {
            "id": 3,
            "username": "王小五",
            "email": "wangwu@example.com",
            "is_active": False,
            "is_superuser": False,
            "created_at": datetime.now().isoformat(),
            "updated_at": datetime.now().isoformat(),
        },
    ]

    # 模擬分頁
    start = (page - 1) * page_size
    end = start + page_size
    paginated_users = fake_users[start:end]

    return {
        "code": 200,
        "message": "查詢成功",
        "data": paginated_users,
        "total": len(fake_users),
        "page": page,
        "page_size": page_size,
    }


@router.get("/{user_id}", summary="獲取使用者詳情", response_model=UserResponse)
def get_user(user_id: int, db: Session = Depends(get_database)) -> Any:
    """
    根據 ID 獲取使用者詳情

    - **user_id**: 使用者 ID
    """
    from datetime import datetime

    # 假資料字典
    fake_users_dict = {
        1: {
            "id": 1,
            "username": "張小三",
            "email": "zhangsan@example.com",
            "is_active": True,
            "is_superuser": False,
            "created_at": datetime.now().isoformat(),
            "updated_at": datetime.now().isoformat(),
        },
        2: {
            "id": 2,
            "username": "李小四",
            "email": "lisi@example.com",
            "is_active": True,
            "is_superuser": False,
            "created_at": datetime.now().isoformat(),
            "updated_at": datetime.now().isoformat(),
        },
        3: {
            "id": 3,
            "username": "王小五",
            "email": "wangwu@example.com",
            "is_active": False,
            "is_superuser": False,
            "created_at": datetime.now().isoformat(),
            "updated_at": datetime.now().isoformat(),
        },
    }

    # 檢查使用者是否存在
    if user_id not in fake_users_dict:
        raise NotFoundException(detail=f"使用者 ID {user_id} 不存在")

    return {
        "code": 200,
        "message": "操作成功",
        "data": fake_users_dict[user_id],
    }


@router.post("/", summary="建立使用者")
def create_user(user_in: UserCreate, db: Session = Depends(get_database)) -> Any:
    """
    建立新使用者

    - **username**: 使用者名稱（3-50 字元）
    - **email**: 電子郵件地址
    - **password**: 密碼（6-50 字元）
    """
    # 檢查使用者名稱是否已存在
    if crud_user.get_by_username(db, username=user_in.username):
        raise ConflictException(detail=f"使用者名稱 {user_in.username} 已存在")

    # 檢查電子郵件是否已存在
    if crud_user.get_by_email(db, email=user_in.email):
        raise ConflictException(detail=f"電子郵件 {user_in.email} 已存在")

    user = crud_user.create(db, obj_in=user_in)
    return success(data=User.model_validate(user), message="使用者建立成功")


@router.put("/{user_id}", summary="更新使用者")
def update_user(
    user_id: int, user_in: UserUpdate, db: Session = Depends(get_database)
) -> Any:
    """
    更新使用者資訊

    - **user_id**: 使用者 ID
    - **username**: 使用者名稱（選填）
    - **email**: 電子郵件地址（選填）
    - **password**: 密碼（選填）
    - **is_active**: 是否啟用（選填）
    """
    user = crud_user.get(db, id=user_id)
    if not user:
        raise NotFoundException(detail=f"使用者 ID {user_id} 不存在")

    # 檢查使用者名稱是否被其他使用者使用
    if user_in.username and user_in.username != user.username:
        existing_user = crud_user.get_by_username(db, username=user_in.username)
        if existing_user and existing_user.id != user_id:
            raise ConflictException(detail=f"使用者名稱 {user_in.username} 已被使用")

    # 檢查電子郵件是否被其他使用者使用
    if user_in.email and user_in.email != user.email:
        existing_user = crud_user.get_by_email(db, email=user_in.email)
        if existing_user and existing_user.id != user_id:
            raise ConflictException(detail=f"電子郵件 {user_in.email} 已被使用")

    user = crud_user.update(db, db_obj=user, obj_in=user_in)
    return success(data=User.model_validate(user), message="使用者更新成功")


@router.delete("/{user_id}", summary="刪除使用者")
def delete_user(user_id: int, db: Session = Depends(get_database)) -> Any:
    """
    刪除使用者

    - **user_id**: 使用者 ID
    """
    user = crud_user.get(db, id=user_id)
    if not user:
        raise NotFoundException(detail=f"使用者 ID {user_id} 不存在")

    crud_user.delete(db, id=user_id)
    return success(message="使用者刪除成功")
