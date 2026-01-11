from fastapi import APIRouter
from app.schemas.invoice import (
    RecognizeRequest,
    RecognizeResponse,
    SaveInvoicesRequest,
    SaveResponse,
)
from app.services.invoice_service import invoice_service

router = APIRouter()


@router.post("/recognize", response_model=RecognizeResponse)
async def recognize_invoice(request: RecognizeRequest):
    """
    識別發票圖片

    Args:
        request: 包含 base64 編碼圖片的請求

    Returns:
        識別結果
    """
    success, data, message = await invoice_service.recognize_invoice(request.image)
    return RecognizeResponse(
        success=success,
        data=data,
        message=message
    )


@router.post("/save", response_model=SaveResponse)
async def save_invoices(request: SaveInvoicesRequest):
    """
    保存發票資料

    Args:
        request: 包含發票資料列表的請求

    Returns:
        保存結果
    """
    success, message = await invoice_service.save_invoices(request)
    return SaveResponse(
        success=success,
        message=message
    )


@router.get("/health")
async def health_check():
    """
    健康檢查

    Returns:
        服務狀態
    """
    return {
        "status": "healthy",
        "service": "invoice-recognition"
    }
