// 發票資料介面
export interface InvoiceData {
  id: string
  invoiceNumber: string // 發票號碼
  invoiceCode: string // 發票代碼
  date: string // 開票日期
  amount: string // 金額
  taxAmount: string // 稅額
  totalAmount: string // 價稅合計
  seller: string // 銷售方名稱
  sellerTaxId: string // 銷售方納稅人識別號
  buyer: string // 購買方名稱
  buyerTaxId: string // 購買方納稅人識別號
  remarks: string // 備註
}

// 上傳的檔案資訊（保留舊的以兼容）
export interface UploadFileInfo {
  id: string
  file: File
  base64: string
  preview: string
  status: 'pending' | 'recognizing' | 'success' | 'error'
  invoiceData?: InvoiceData
}

// 發票 Session（新設計）
export interface InvoiceSession {
  id: string
  file: File
  previewUrl: string
  status: 'uploading' | 'processing' | 'review' | 'success' | 'error' | 'saving' | 'idle'
  base64?: string
  data?: InvoiceData
  errorMessage?: string
}

// API 回應介面
export interface RecognizeResponse {
  success: boolean
  data?: InvoiceData
  message?: string
}

export interface SaveResponse {
  success: boolean
  message?: string
}
