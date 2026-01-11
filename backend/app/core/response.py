from typing import Any, Optional, Union
from pydantic import BaseModel


class ResponseModel(BaseModel):
    """
    統一回應模型
    """

    code: int = 200
    message: str = "success"
    data: Optional[Any] = None


class PaginatedResponse(BaseModel):
    """
    分頁回應模型
    """

    code: int = 200
    message: str = "success"
    data: Optional[Any] = None
    total: int = 0
    page: int = 1
    page_size: int = 10


def success(data: Any = None, message: str = "操作成功") -> ResponseModel:
    """
    成功回應

    Args:
        data: 回應資料
        message: 回應訊息

    Returns:
        回應模型
    """
    return ResponseModel(code=200, message=message, data=data)


def error(message: str = "操作失敗", code: int = 400, data: Any = None) -> ResponseModel:
    """
    錯誤回應

    Args:
        message: 錯誤訊息
        code: 錯誤代碼
        data: 額外資料

    Returns:
        回應模型
    """
    return ResponseModel(code=code, message=message, data=data)


def paginated_response(
    data: Any, total: int, page: int, page_size: int, message: str = "查詢成功"
) -> PaginatedResponse:
    """
    分頁回應

    Args:
        data: 資料清單
        total: 總數
        page: 目前頁面
        page_size: 每頁數量
        message: 回應訊息

    Returns:
        分頁回應模型
    """
    return PaginatedResponse(
        code=200,
        message=message,
        data=data,
        total=total,
        page=page,
        page_size=page_size,
    )
