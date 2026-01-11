import base64
import json
from typing import Optional
from openai import OpenAI
from app.config import settings
from app.schemas.invoice import InvoiceData


class AIService:
    """AI 服務類，支持 OpenAI 和 Ollama"""

    def __init__(self):
        """初始化 AI 客戶端"""
        self.service_type = settings.AI_SERVICE_TYPE.lower()
        
        if self.service_type == "ollama":
            # 使用 Ollama（本地模型）
            self.client = OpenAI(
                base_url=settings.OLLAMA_BASE_URL,
                api_key="ollama"  # Ollama 不需要真正的 API key，但 OpenAI 客戶端需要一個值
            )
            self.model = settings.OLLAMA_MODEL
        else:
            # 使用 OpenAI（預設）
            self.client = OpenAI(
                api_key=settings.OPENAI_API_KEY,
                base_url=settings.OPENAI_BASE_URL if settings.OPENAI_BASE_URL else None
            )
            self.model = settings.OPENAI_MODEL

    def recognize_invoice(self, base64_image: str) -> Optional[InvoiceData]:
        """
        使用 AI 模型識別發票（支持 OpenAI 和 Ollama）

        Args:
            base64_image: Base64 編碼的圖片

        Returns:
            識別的發票資料，如果識別失敗則返回 None
        """
        try:
            # 構建提示詞
            prompt = """請分析這張發票圖片，並提取以下資訊：

1. 發票號碼 (invoiceNumber)
2. 發票代碼 (invoiceCode)
3. 開票日期 (date) - 格式：YYYY-MM-DD
4. 金額 (amount) - 不含稅金額
5. 稅額 (taxAmount)
6. 價稅合計 (totalAmount)
7. 銷售方名稱 (seller)
8. 銷售方納稅人識別號 (sellerTaxId)
9. 購買方名稱 (buyer)
10. 購買方納稅人識別號 (buyerTaxId)
11. 備註 (remarks)
12. 項目列表 (items) - 包含項目名稱(name)、數量(quantity)、價格(price)

請以 JSON 格式返回結果，格式如下：
{
  "invoiceNumber": "發票號碼",
  "invoiceCode": "發票代碼",
  "date": "YYYY-MM-DD",
  "amount": "金額",
  "taxAmount": "稅額",
  "totalAmount": "價稅合計",
  "seller": "銷售方名稱",
  "sellerTaxId": "銷售方納稅人識別號",
  "buyer": "購買方名稱",
  "buyerTaxId": "購買方納稅人識別號",
  "remarks": "備註",
  "items": [
    {
      "name": "項目名稱",
      "quantity": "數量",
      "price": "價格"
    }
  ]
}

如果某個欄位在圖片中找不到，請使用空字串。
所有數字請以字串形式返回。
只返回 JSON，不要添加任何其他說明文字。"""

            # 準備圖片 URL
            if not base64_image.startswith('data:'):
                image_url = f"data:image/jpeg;base64,{base64_image}"
            else:
                image_url = base64_image

            # 調用 AI API（OpenAI 兼容接口）
            response = self.client.chat.completions.create(
                model=self.model,
                messages=[
                    {
                        "role": "user",
                        "content": [
                            {
                                "type": "text",
                                "text": prompt
                            },
                            {
                                "type": "image_url",
                                "image_url": {
                                    "url": image_url
                                }
                            }
                        ]
                    }
                ],
                max_tokens=1000,
                temperature=0.2,
            )

            # 解析回應
            content = response.choices[0].message.content
            if not content:
                return None

            # 嘗試從回應中提取 JSON
            # 移除可能的 markdown 代碼塊標記
            content = content.strip()
            if content.startswith("```json"):
                content = content[7:]
            if content.startswith("```"):
                content = content[3:]
            if content.endswith("```"):
                content = content[:-3]
            content = content.strip()

            # 解析 JSON
            data = json.loads(content)

            # 生成唯一 ID
            import uuid
            invoice_id = str(uuid.uuid4())

            # 為每個項目生成 ID
            items = []
            for item in data.get("items", []):
                items.append({
                    "id": str(uuid.uuid4()),
                    "name": item.get("name", ""),
                    "quantity": item.get("quantity", ""),
                    "price": item.get("price", "")
                })

            # 構建 InvoiceData
            invoice_data = InvoiceData(
                id=invoice_id,
                invoiceNumber=data.get("invoiceNumber", ""),
                invoiceCode=data.get("invoiceCode", ""),
                date=data.get("date", ""),
                amount=data.get("amount", ""),
                taxAmount=data.get("taxAmount", ""),
                totalAmount=data.get("totalAmount", ""),
                seller=data.get("seller", ""),
                sellerTaxId=data.get("sellerTaxId", ""),
                buyer=data.get("buyer", ""),
                buyerTaxId=data.get("buyerTaxId", ""),
                remarks=data.get("remarks", ""),
                items=items
            )

            return invoice_data

        except Exception as e:
            service_name = "Ollama" if self.service_type == "ollama" else "OpenAI"
            print(f"發票識別錯誤 ({service_name}): {str(e)}")
            return None


# 創建全局實例（保持向後兼容的變數名）
ai_service = AIService()
openai_service = ai_service  # 向後兼容
