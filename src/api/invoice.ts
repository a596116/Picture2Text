import type { InvoiceData, RecognizeResponse, SaveResponse } from '../types/invoice'

// 生成隨機發票資料
const generateMockInvoiceData = (): InvoiceData => {
  const randomNumber = Math.floor(Math.random() * 100000000)
  const randomCode = Math.floor(Math.random() * 10000000000)

  return {
    id: `invoice_${Date.now()}_${Math.random()}`,
    invoiceNumber: `${randomNumber.toString().padStart(8, '0')}`,
    invoiceCode: `${randomCode.toString().padStart(10, '0')}`,
    date: '2025-01-09',
    amount: (Math.random() * 10000 + 100).toFixed(2),
    taxAmount: (Math.random() * 1000 + 10).toFixed(2),
    totalAmount: (Math.random() * 11000 + 110).toFixed(2),
    seller: '某某科技有限公司',
    sellerTaxId: '91110000MA01234567',
    buyer: '某某貿易有限公司',
    buyerTaxId: '91110000MA09876543',
    remarks: '備註資訊',
    items: []
  }
}

/**
 * 辨識發票圖片（模擬 API 呼叫）
 * @param base64 圖片的 base64 編碼
 * @returns 辨識結果
 */
export const recognizeInvoice = async (base64: string): Promise<RecognizeResponse> => {
  // 模擬網路延遲
  await new Promise(resolve => setTimeout(resolve, 1000 + Math.random() * 1000))

  // 模擬成功回應
  return {
    success: true,
    data: generateMockInvoiceData()
  }

  // 如果需要模擬失敗，可以取消下面的註解
  // if (Math.random() > 0.8) {
  //   return {
  //     success: false,
  //     message: '辨識失敗，請重試'
  //   }
  // }
}

/**
 * 儲存發票資料（模擬 API 呼叫）
 * @param invoices 發票資料陣列
 * @returns 儲存結果
 */
export const saveInvoices = async (invoices: InvoiceData[]): Promise<SaveResponse> => {
  // 模擬網路延遲
  await new Promise(resolve => setTimeout(resolve, 500))

  console.log('儲存的發票資料:', invoices)

  // 模擬成功回應
  return {
    success: true,
    message: `成功儲存 ${invoices.length} 張發票`
  }

  // 如果需要模擬失敗，可以取消下面的註解
  // if (Math.random() > 0.9) {
  //   return {
  //     success: false,
  //     message: '儲存失敗，請重試'
  //   }
  // }
}

/**
 * 實際 API 呼叫範例（已註解，需要時取消註解並配置）
 */
/*
export const recognizeInvoice = async (base64: string): Promise<RecognizeResponse> => {
  try {
    const response = await fetch('/api/invoice/recognize', {
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

export const saveInvoices = async (invoices: InvoiceData[]): Promise<SaveResponse> => {
  try {
    const response = await fetch('/api/invoice/save', {
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
*/
