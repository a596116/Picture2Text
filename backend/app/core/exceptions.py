from fastapi import HTTPException, status


class CustomException(HTTPException):
    """自訂異常基礎類別"""

    def __init__(self, status_code: int, detail: str):
        super().__init__(status_code=status_code, detail=detail)


class NotFoundException(CustomException):
    """資源未找到異常"""

    def __init__(self, detail: str = "資源未找到"):
        super().__init__(status_code=status.HTTP_404_NOT_FOUND, detail=detail)


class BadRequestException(CustomException):
    """錯誤請求異常"""

    def __init__(self, detail: str = "請求參數錯誤"):
        super().__init__(status_code=status.HTTP_400_BAD_REQUEST, detail=detail)


class UnauthorizedException(CustomException):
    """未授權異常"""

    def __init__(self, detail: str = "未授權存取"):
        super().__init__(status_code=status.HTTP_401_UNAUTHORIZED, detail=detail)


class ForbiddenException(CustomException):
    """禁止存取異常"""

    def __init__(self, detail: str = "禁止存取"):
        super().__init__(status_code=status.HTTP_403_FORBIDDEN, detail=detail)


class ConflictException(CustomException):
    """衝突異常"""

    def __init__(self, detail: str = "資源衝突"):
        super().__init__(status_code=status.HTTP_409_CONFLICT, detail=detail)


class InternalServerException(CustomException):
    """伺服器內部錯誤異常"""

    def __init__(self, detail: str = "伺服器內部錯誤"):
        super().__init__(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=detail)
