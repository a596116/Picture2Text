import type { InvoiceData, RecognizeResponse, SaveResponse } from '../types/invoice'

// API 基礎 URL
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8000/api'

/**
 * 辨識發票圖片
 * @param base64 圖片的 base64 編碼
 * @returns 辨識結果
 */
export const recognizeInvoice = async (base64: string): Promise<RecognizeResponse> => {
  try {
    const response = await fetch(`${API_BASE_URL}/invoice/recognize`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ image: base64 })
    })

    const data = await response.json()
    return data
  } catch (error) {
    return {
      success: false,
      message: '網路錯誤，請重試'
    }
  }
}

/**
 * 儲存發票資料
 * @param invoices 發票資料陣列
 * @returns 儲存結果
 */
export const saveInvoices = async (invoices: InvoiceData[]): Promise<SaveResponse> => {
  try {
    const response = await fetch(`${API_BASE_URL}/invoice/save`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ invoices })
    })

    const data = await response.json()
    return data
  } catch (error) {
    return {
      success: false,
      message: '網路錯誤，請重試'
    }
  }
}
