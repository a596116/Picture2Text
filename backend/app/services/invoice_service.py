"""發票服務層"""
from typing import List, Optional
from app.schemas.invoice import InvoiceData, SaveInvoicesRequest
from app.services.ai_service import ai_service


class InvoiceService:
    """發票業務邏輯服務層"""

    def __init__(self):
        self.ai_service = ai_service

    async def recognize_invoice(self, base64_image: str) -> tuple[bool, Optional[InvoiceData], str]:
        """
        識別發票圖片

        Args:
            base64_image: Base64 編碼的圖片

        Returns:
            (success, data, message) 元組
        """
        try:
            invoice_data = await self.ai_service.recognize_invoice(base64_image)

            if invoice_data:
                return True, invoice_data, "發票識別成功"
            else:
                return False, None, "無法識別發票，請確認圖片清晰度或重新上傳"

        except Exception as e:
            return False, None, f"發票識別失敗: {str(e)}"

    async def save_invoices(self, request: SaveInvoicesRequest) -> tuple[bool, str]:
        """
        保存發票資料

        Args:
            request: 包含發票資料列表的請求

        Returns:
            (success, message) 元組
        """
        try:
            invoices = request.invoices
            count = len(invoices)

            # TODO: 實作將發票資料保存到資料庫的邏輯
            # 目前只是簡單地記錄資料
            print(f"收到 {count} 張發票資料:")
            for invoice in invoices:
                print(f"  - 發票號碼: {invoice.invoiceNumber}, 金額: {invoice.totalAmount}")

            # 這裡可以添加資料庫保存邏輯
            # 例如：
            # for invoice in invoices:
            #     db.add(invoice)
            # db.commit()

            return True, f"成功保存 {count} 張發票"

        except Exception as e:
            return False, f"發票保存失敗: {str(e)}"


# 創建全局實例
invoice_service = InvoiceService()
