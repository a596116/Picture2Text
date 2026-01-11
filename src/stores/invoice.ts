import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { recognizeInvoice, saveInvoices } from '../api/invoice'
import type { InvoiceData, InvoiceSession } from '../types/invoice'

export const useInvoiceStore = defineStore('invoice', () => {
  // ==================== 狀態 ====================

  /** 所有發票 session 的列表 */
  const sessions = ref<InvoiceSession[]>([])

  /** 當前選中的發票 ID */
  const activeId = ref<string | null>(null)

  /** 是否正在批量保存所有發票 */
  const isSavingAll = ref(false)

  // ==================== 計算屬性 ====================

  /** 當前選中的發票 session */
  const activeSession = computed(() =>
    sessions.value.find((s) => s.id === activeId.value)
  )

  /** 當前選中發票的索引（用於 Swiper） */
  const activeIndex = computed(() => {
    if (!activeId.value) return 0
    const index = sessions.value.findIndex((s) => s.id === activeId.value)
    return index >= 0 ? index : 0
  })

  /** 可保存的發票數量（有資料且狀態為 review 的） */
  const reviewableCount = computed(() => {
    return sessions.value.filter((s) => s.data && s.status === 'review').length
  })

  /** 是否有可保存的發票 */
  const hasReviewableSessions = computed(() => {
    return reviewableCount.value > 0
  })

  // ==================== 內部方法 ====================

  /**
   * 處理發票 session 的辨識流程
   * @param session - 要處理的發票 session
   */
  const processSession = async (session: InvoiceSession) => {
    // 如果已經處理過或正在處理，跳過
    if (session.status !== 'uploading' && session.status !== 'idle') return

    // 更新狀態為處理中
    updateSession(session.id, { status: 'processing' })

    try {
      // 如果還沒有 base64，先讀取檔案
      let base64 = session.base64
      if (!base64) {
        base64 = await new Promise<string>((resolve, reject) => {
          const reader = new FileReader()
          reader.onloadend = () => resolve(reader.result as string)
          reader.onerror = reject
          reader.readAsDataURL(session.file)
        })
        updateSession(session.id, { base64 })
      }

      // 調用 API 辨識發票
      const response = await recognizeInvoice(base64!)

      if (response.success && response.data) {
        // 辨識成功，更新狀態和資料
        updateSession(session.id, {
          status: 'review',
          data: response.data,
        })
        ElMessage.success('發票辨識成功')
      } else {
        throw new Error(response.message || '辨識失敗')
      }
    } catch (err: any) {
      // 辨識失敗，顯示錯誤提示並更新為錯誤狀態
      ElMessage.error(err.message || '發票辨識失敗，請重試')
      updateSession(session.id, {
        status: 'error',
        errorMessage: err.message || '處理圖片時發生錯誤',
      })
    }
  }

  /**
   * 更新指定 session 的資料
   * @param id - session ID
   * @param updates - 要更新的欄位
   */
  const updateSession = (id: string, updates: Partial<InvoiceSession>) => {
    sessions.value = sessions.value.map((s) =>
      s.id === id ? { ...s, ...updates } : s
    )
  }

  /**
   * 監聽 sessions 變化，自動處理上傳中的 session
   */
  watch(
    sessions,
    () => {
      sessions.value.forEach((session) => {
        if (session.status === 'uploading') {
          processSession(session)
        }
      })
    },
    { deep: true }
  )

  // ==================== 公開方法 ====================

  /**
   * 處理檔案列表（通用函數，用於檔案選擇和拖曳上傳）
   * @param files - 檔案列表
   */
  const processFiles = async (files: File[]) => {
    if (files.length === 0) return

    // 過濾出圖片檔案
    const imageFiles = files.filter((file) => file.type.startsWith('image/'))
    if (imageFiles.length === 0) {
      ElMessage.warning('請上傳圖片檔案')
      return
    }

    // 為每個檔案創建 session
    const newSessions: InvoiceSession[] = imageFiles.map((file) => ({
      id: Math.random().toString(36).substr(2, 9),
      file,
      previewUrl: URL.createObjectURL(file),
      status: 'uploading', // 標記為上傳中，觸發處理流程
      data: undefined,
    }))

    sessions.value = [...sessions.value, ...newSessions]

    // 如果沒有選中的發票，自動選擇第一個新上傳的
    if (!activeId.value && newSessions.length > 0) {
      activeId.value = newSessions[0].id
    }
  }

  /**
   * 處理發票資料變更
   * @param newData - 新的發票資料
   */
  const handleDataChange = (newData: InvoiceData) => {
    if (!activeId.value) return
    updateSession(activeId.value, { data: newData })
  }

  /**
   * 儲存當前發票
   */
  const handleSaveCurrent = async () => {
    if (!activeId.value || !activeSession.value?.data) return
    const currentSession = activeSession.value
    updateSession(activeId.value, { status: 'saving' })

    try {
      const response = await saveInvoices([currentSession.data!])
      if (response.success) {
        // 保存成功，顯示提示並移除已保存的發票
        ElMessage.success(response.message || '發票保存成功')
        URL.revokeObjectURL(currentSession.previewUrl)
        const newSessions = sessions.value.filter(
          (s) => s.id !== activeId.value
        )
        sessions.value = newSessions
        // 選擇下一個發票或設為 null
        activeId.value = newSessions.length > 0 ? newSessions[0].id : null
      } else {
        throw new Error(response.message || '儲存失敗')
      }
    } catch (err: any) {
      // 保存失敗，顯示錯誤提示
      ElMessage.error(err.message || '保存失敗，請重試')
      updateSession(activeId.value!, {
        status: 'error',
        errorMessage: err.message || '儲存時發生錯誤',
      })
    }
  }

  /**
   * 刪除指定的發票 session
   * @param id - 要刪除的 session ID
   */
  const handleDeleteSession = (id: string) => {
    const session = sessions.value.find((s) => s.id === id)
    // 釋放預覽圖片的 URL，避免記憶體洩漏
    if (session) {
      URL.revokeObjectURL(session.previewUrl)
    }
    const newSessions = sessions.value.filter((s) => s.id !== id)
    sessions.value = newSessions
    // 如果刪除的是當前選中的，選擇第一個或設為 null
    if (activeId.value === id) {
      activeId.value = newSessions.length > 0 ? newSessions[0].id : null
    }
  }

  /**
   * 刪除當前選中的發票
   */
  const handleDeleteCurrent = () => {
    if (!activeId.value) return
    handleDeleteSession(activeId.value)
  }

  /**
   * 刪除所有發票
   */
  const handleDeleteAll = () => {
    // 釋放所有預覽圖片的 URL，避免記憶體洩漏
    sessions.value.forEach((session) => {
      URL.revokeObjectURL(session.previewUrl)
    })
    // 清空所有發票
    sessions.value = []
    // 清空當前選中的發票
    activeId.value = null
  }

  /**
   * 設置當前選中的發票 ID
   * @param id - 要選中的 session ID
   */
  const setActiveId = (id: string) => {
    activeId.value = id
  }

  /**
   * 處理手動輸入：為當前 session 創建空的資料結構
   */
  const handleManualInput = () => {
    if (!activeSession.value) return
    updateSession(activeSession.value.id, {
      data: {
        id: activeSession.value.id,
        invoiceCode: '',
        invoiceNumber: '',
        date: '',
        amount: '',
        taxAmount: '',
        totalAmount: '',
        seller: '',
        sellerTaxId: '',
        buyer: '',
        buyerTaxId: '',
        remarks: '',
        items: [],
      },
    })
  }

  /**
   * 批量保存所有可保存的發票
   */
  const handleSaveAll = async () => {
    // 找出所有可保存的發票（有資料且狀態為 review）
    const reviewableSessions = sessions.value.filter(
      (s) => s.data && s.status === 'review'
    )

    if (reviewableSessions.length === 0) {
      ElMessage.warning('沒有可保存的發票')
      return
    }

    isSavingAll.value = true

    try {
      // 更新所有要保存的發票狀態為 saving
      reviewableSessions.forEach((session) => {
        updateSession(session.id, { status: 'saving' })
      })

      // 準備要保存的發票資料
      const invoicesToSave = reviewableSessions
        .map((s) => s.data!)
        .filter(Boolean)

      // 調用 API 批量保存
      const response = await saveInvoices(invoicesToSave)

      if (response.success) {
        // 保存成功，顯示提示
        ElMessage.success(
          response.message || `成功保存 ${invoicesToSave.length} 張發票`
        )

        // 移除所有已保存的發票
        const savedIds = reviewableSessions.map((s) => s.id)

        // 釋放預覽圖片的 URL
        reviewableSessions.forEach((session) => {
          URL.revokeObjectURL(session.previewUrl)
        })

        // 從列表中移除已保存的發票
        sessions.value = sessions.value.filter((s) => !savedIds.includes(s.id))

        // 如果刪除的包含當前選中的，選擇第一個或設為 null
        if (activeId.value && savedIds.includes(activeId.value)) {
          activeId.value =
            sessions.value.length > 0 ? sessions.value[0].id : null
        }
      } else {
        // 保存失敗，恢復狀態
        reviewableSessions.forEach((session) => {
          updateSession(session.id, { status: 'review' })
        })
        throw new Error(response.message || '批量保存失敗')
      }
    } catch (err: any) {
      // 保存失敗，顯示錯誤提示並恢復狀態
      ElMessage.error(err.message || '批量保存失敗，請重試')
      reviewableSessions.forEach((session) => {
        updateSession(session.id, {
          status: 'error',
          errorMessage: err.message || '保存時發生錯誤',
        })
      })
    } finally {
      isSavingAll.value = false
    }
  }

  return {
    // 狀態
    sessions,
    activeId,
    isSavingAll,
    // 計算屬性
    activeSession,
    activeIndex,
    reviewableCount,
    hasReviewableSessions,
    // 方法
    processFiles,
    updateSession,
    handleDataChange,
    handleSaveCurrent,
    handleDeleteSession,
    handleDeleteCurrent,
    handleDeleteAll,
    setActiveId,
    handleManualInput,
    handleSaveAll,
  }
})
