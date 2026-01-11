from fastapi import APIRouter, HTTPException, status
from typing import List
from app.schemas.invoice import (
    RecognizeRequest,
    RecognizeResponse,
    SaveInvoicesRequest,
    SaveResponse,
)
from app.utils.openai_service import openai_service

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
    try:
        # 調用 OpenAI 服務識別發票
        invoice_data = openai_service.recognize_invoice(request.image)

        if invoice_data:
            return RecognizeResponse(
                success=True,
                data=invoice_data,
                message="發票識別成功"
            )
        else:
            return RecognizeResponse(
                success=False,
                message="無法識別發票，請確認圖片清晰度或重新上傳"
            )

    except Exception as e:
        return RecognizeResponse(
            success=False,
            message=f"發票識別失敗: {str(e)}"
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
    try:
        # TODO: 實作將發票資料保存到資料庫的邏輯
        # 目前只是簡單地返回成功回應

        invoices = request.invoices
        count = len(invoices)

        # 這裡可以添加資料庫保存邏輯
        # 例如：
        # for invoice in invoices:
        #     db.add(invoice)
        # db.commit()

        # 暫時只打印資料
        print(f"收到 {count} 張發票資料:")
        for invoice in invoices:
            print(f"  - 發票號碼: {invoice.invoiceNumber}, 金額: {invoice.totalAmount}")

        return SaveResponse(
            success=True,
            message=f"成功保存 {count} 張發票"
        )

    except Exception as e:
        return SaveResponse(
            success=False,
            message=f"發票保存失敗: {str(e)}"
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
