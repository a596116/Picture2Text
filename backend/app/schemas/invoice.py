from typing import List, Optional
from pydantic import BaseModel, Field


# 發票項目模型
class InvoiceItem(BaseModel):
    """發票項目"""
    id: str
    name: str = Field(..., description="項目名稱")
    quantity: str = Field(..., description="數量")
    price: str = Field(..., description="價格")


# 發票資料模型
class InvoiceData(BaseModel):
    """發票資料"""
    id: str
    invoiceNumber: str = Field(..., description="發票號碼")
    invoiceCode: str = Field(..., description="發票代碼")
    date: str = Field(..., description="開票日期")
    amount: str = Field(..., description="金額")
    taxAmount: str = Field(..., description="稅額")
    totalAmount: str = Field(..., description="價稅合計")
    seller: str = Field(..., description="銷售方名稱")
    sellerTaxId: str = Field(..., description="銷售方納稅人識別號")
    buyer: str = Field(..., description="購買方名稱")
    buyerTaxId: str = Field(..., description="購買方納稅人識別號")
    remarks: str = Field(..., description="備註")
    items: List[InvoiceItem] = Field(default_factory=list, description="項目列表")


# 請求模型
class RecognizeRequest(BaseModel):
    """識別發票請求"""
    image: str = Field(..., description="Base64 編碼的圖片")


class SaveInvoicesRequest(BaseModel):
    """保存發票請求"""
    invoices: List[InvoiceData] = Field(..., description="發票資料列表")


# 響應模型
class RecognizeResponse(BaseModel):
    """識別發票響應"""
    success: bool = Field(..., description="是否成功")
    data: Optional[InvoiceData] = Field(None, description="識別的發票資料")
    message: Optional[str] = Field(None, description="提示訊息")


class SaveResponse(BaseModel):
    """保存發票響應"""
    success: bool = Field(..., description="是否成功")
    message: Optional[str] = Field(None, description="提示訊息")
