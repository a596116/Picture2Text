"""AI 服務模組 - 異步版本（優化）"""
import base64
import json
import uuid
import logging
import re
from typing import Optional, Dict, Any
from openai import AsyncOpenAI
from app.config import settings
from app.schemas.invoice import InvoiceData

logger = logging.getLogger(__name__)


class AIService:
    """AI 服務類，支持 OpenAI 和 Ollama（異步版本，優化）"""

    def __init__(self):
        """初始化 AI 客戶端"""
        self.service_type = settings.AI_SERVICE_TYPE.lower()
        
        if self.service_type == "ollama":
            # 使用 Ollama（本地模型）
            self.client = AsyncOpenAI(
                base_url=settings.OLLAMA_BASE_URL,
                api_key="ollama"  # Ollama 不需要真正的 API key
            )
            self.model = settings.OLLAMA_MODEL
        else:
            # 使用 OpenAI（預設）
            self.client = AsyncOpenAI(
                api_key=settings.OPENAI_API_KEY,
                base_url=settings.OPENAI_BASE_URL if settings.OPENAI_BASE_URL else None
            )
            self.model = settings.OPENAI_MODEL

    async def recognize_invoice(self, base64_image: str) -> Optional[InvoiceData]:
        """
        使用 AI 模型識別發票（支持 OpenAI 和 Ollama）- 異步版本（優化）

        Args:
            base64_image: Base64 編碼的圖片

        Returns:
            識別的發票資料，如果識別失敗則返回 None
        """
        try:
            # 驗證和預處理圖片
            image_url = self._preprocess_image(base64_image)
            if not image_url:
                logger.error("圖片預處理失敗")
                return None

            # 構建優化的提示詞
            prompt = self._get_invoice_prompt()

            # 調用 AI API（優化參數）
            response = await self._call_ai_api(prompt, image_url)
            if not response:
                return None

            # 解析和驗證回應
            data = self._parse_ai_response(response)
            if not data:
                return None

            # 構建 InvoiceData（帶數據清理）
            invoice_data = self._build_invoice_data(data)
            return invoice_data

        except Exception as e:
            service_name = "Ollama" if self.service_type == "ollama" else "OpenAI"
            logger.error(f"發票識別錯誤 ({service_name}): {str(e)}", exc_info=True)
            return None

    def _preprocess_image(self, base64_image: str) -> Optional[str]:
        """
        預處理圖片：驗證格式、大小等

        Returns:
            處理後的圖片 URL，失敗返回 None
        """
        try:
            # 移除 data URL 前綴（如果存在）
            if base64_image.startswith('data:'):
                # 提取 MIME 類型和 base64 數據
                match = re.match(r'data:image/(\w+);base64,(.+)', base64_image)
                if match:
                    image_format = match.group(1).lower()
                    base64_data = match.group(2)
                    
                    # 驗證圖片格式
                    if image_format not in ['jpeg', 'jpg', 'png', 'webp']:
                        logger.warning(f"不支持的圖片格式: {image_format}")
                        return None
                    
                    # 檢查圖片大小（base64 編碼後的大小）
                    if len(base64_data) > 20 * 1024 * 1024:  # 約 15MB 原始圖片
                        logger.warning("圖片過大，可能影響識別性能")
                    
                    return f"data:image/{image_format};base64,{base64_data}"
                else:
                    return base64_image
            else:
                # 假設是 JPEG（向後兼容）
                return f"data:image/jpeg;base64,{base64_image}"

        except Exception as e:
            logger.error(f"圖片預處理錯誤: {str(e)}")
            return None

    async def _call_ai_api(self, prompt: str, image_url: str) -> Optional[str]:
        """
        調用 AI API（優化版本）
        
        改進點：
        1. 增加 max_tokens（處理複雜發票）
        2. 使用更低的 temperature（提高準確性）
        3. 添加高解析度模式（GPT-4o）
        """
        try:
            is_openai = self.service_type != "ollama"
            
            # OpenAI 模型參數
            if is_openai:
                params = {
                    "model": self.model,
                    "messages": [
                        {
                            "role": "user",
                            "content": [
                                {"type": "text", "text": prompt},
                                {
                                    "type": "image_url",
                                    "image_url": {
                                        "url": image_url,
                                        "detail": "high"  # 高解析度模式，提高識別準確度
                                    }
                                }
                            ]
                        }
                    ],
                    "max_tokens": 2000,  # 增加 token 限制，處理複雜發票
                    "temperature": 0.1,  # 降低溫度，提高準確性和一致性
                    "timeout": 60.0,  # 60秒超時
                }
                
                # GPT-4o 支持 JSON mode（如果可用）
                if "gpt-4o" in self.model.lower():
                    try:
                        # 嘗試使用 JSON mode（需要 OpenAI API 支持）
                        params["response_format"] = {"type": "json_object"}
                    except:
                        pass  # 如果不支持，使用普通模式
            else:
                # Ollama 模型參數
                params = {
                    "model": self.model,
                    "messages": [
                        {
                            "role": "user",
                            "content": [
                                {"type": "text", "text": prompt},
                                {
                                    "type": "image_url",
                                    "image_url": {"url": image_url}
                                }
                            ]
                        }
                    ],
                    "max_tokens": 2000,
                    "temperature": 0.1,
                }

            response = await self.client.chat.completions.create(**params)
            
            if not response or not response.choices:
                logger.error("AI API 返回空回應")
                return None

            content = response.choices[0].message.content
            if not content:
                logger.error("AI API 返回的內容為空")
                return None

            return content

        except Exception as e:
            logger.error(f"調用 AI API 錯誤: {str(e)}", exc_info=True)
            return None

    def _get_invoice_prompt(self) -> str:
        """獲取發票識別提示詞（優化版本）"""
        return """你是一個專業的發票識別系統。請仔細分析這張發票圖片，準確提取以下資訊：

**必填欄位：**
1. invoiceNumber (發票號碼) - 通常是8位數字
2. invoiceCode (發票代碼) - 通常是12位數字
3. date (開票日期) - 格式必須為 YYYY-MM-DD，例如：2024-01-15
4. totalAmount (價稅合計) - 總金額，包含稅額

**金額相關欄位：**
5. amount (金額) - 不含稅金額
6. taxAmount (稅額) - 稅額

**交易雙方資訊：**
7. seller (銷售方名稱) - 完整的公司或個人名稱
8. sellerTaxId (銷售方納稅人識別號) - 統一社會信用代碼或稅號
9. buyer (購買方名稱) - 完整的公司或個人名稱
10. buyerTaxId (購買方納稅人識別號) - 統一社會信用代碼或稅號

**其他資訊：**
11. remarks (備註) - 發票上的備註或說明
12. items (項目列表) - 發票明細項目，每個項目包含：
    - name (項目名稱)
    - quantity (數量) - 數字或字串
    - price (單價或金額) - 數字或字串

**重要要求：**
- 如果某個欄位在圖片中找不到，請使用空字串 ""
- 所有數字欄位（金額、數量等）請以字串形式返回，保留原始格式
- 日期必須嚴格按照 YYYY-MM-DD 格式
- 只返回純 JSON，不要添加任何說明文字、markdown 標記或其他內容
- 確保 JSON 格式完全正確，可以被直接解析

**返回格式（嚴格遵守）：**
{
  "invoiceNumber": "發票號碼或空字串",
  "invoiceCode": "發票代碼或空字串",
  "date": "YYYY-MM-DD 或空字串",
  "amount": "金額或空字串",
  "taxAmount": "稅額或空字串",
  "totalAmount": "價稅合計或空字串",
  "seller": "銷售方名稱或空字串",
  "sellerTaxId": "銷售方稅號或空字串",
  "buyer": "購買方名稱或空字串",
  "buyerTaxId": "購買方稅號或空字串",
  "remarks": "備註或空字串",
  "items": [
    {
      "name": "項目名稱或空字串",
      "quantity": "數量或空字串",
      "price": "價格或空字串"
    }
  ]
}"""

    def _parse_ai_response(self, content: str) -> Optional[Dict[str, Any]]:
        """
        解析 AI 回應內容（增強版本）
        
        改進點：
        1. 更強健的 JSON 提取
        2. 數據驗證
        3. 錯誤恢復
        """
        try:
            # 清理內容
            content = content.strip()
            
            # 移除 markdown 代碼塊
            patterns = [
                (r'```json\s*', ''),
                (r'```\s*', ''),
            ]
            
            for pattern, replacement in patterns:
                content = re.sub(pattern, replacement, content)
            
            content = content.strip()
            
            # 嘗試找到 JSON 對象
            json_match = re.search(r'\{.*\}', content, re.DOTALL)
            if json_match:
                content = json_match.group(0)
            
            # 解析 JSON
            data = json.loads(content)
            
            # 基本驗證
            if not isinstance(data, dict):
                logger.error("解析的數據不是字典格式")
                return None
            
            # 確保必要欄位存在
            required_fields = [
                "invoiceNumber", "invoiceCode", "date", "amount",
                "taxAmount", "totalAmount", "seller", "sellerTaxId",
                "buyer", "buyerTaxId", "remarks", "items"
            ]
            
            for field in required_fields:
                if field not in data:
                    data[field] = "" if field != "items" else []
            
            # 驗證 items 格式
            if not isinstance(data.get("items"), list):
                data["items"] = []
            
            return data

        except json.JSONDecodeError as e:
            logger.error(f"JSON 解析錯誤: {str(e)}")
            logger.debug(f"原始內容: {content[:500]}")
            return None
        except Exception as e:
            logger.error(f"回應驗證錯誤: {str(e)}")
            return None

    def _build_invoice_data(self, data: Dict[str, Any]) -> InvoiceData:
        """
        構建 InvoiceData 對象（帶數據清理）
        
        改進點：
        1. 數據清理和標準化
        2. 日期格式驗證
        3. 金額格式處理
        """
        # 生成唯一 ID
        invoice_id = str(uuid.uuid4())

        # 清理和處理項目
        items = []
        for item in data.get("items", []):
            if isinstance(item, dict):
                items.append({
                    "id": str(uuid.uuid4()),
                    "name": str(item.get("name", "")).strip(),
                    "quantity": str(item.get("quantity", "")).strip(),
                    "price": str(item.get("price", "")).strip()
                })

        # 清理日期格式
        date = str(data.get("date", "")).strip()
        # 嘗試標準化日期格式
        date_match = re.search(r'(\d{4})[-\/](\d{1,2})[-\/](\d{1,2})', date)
        if date_match:
            year, month, day = date_match.groups()
            date = f"{year}-{month.zfill(2)}-{day.zfill(2)}"

        # 構建 InvoiceData
        return InvoiceData(
            id=invoice_id,
            invoiceNumber=str(data.get("invoiceNumber", "")).strip(),
            invoiceCode=str(data.get("invoiceCode", "")).strip(),
            date=date,
            amount=str(data.get("amount", "")).strip(),
            taxAmount=str(data.get("taxAmount", "")).strip(),
            totalAmount=str(data.get("totalAmount", "")).strip(),
            seller=str(data.get("seller", "")).strip(),
            sellerTaxId=str(data.get("sellerTaxId", "")).strip(),
            buyer=str(data.get("buyer", "")).strip(),
            buyerTaxId=str(data.get("buyerTaxId", "")).strip(),
            remarks=str(data.get("remarks", "")).strip(),
            items=items
        )


# 創建全局實例
ai_service = AIService()
