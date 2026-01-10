<template>
  <!-- 隱藏的檔案輸入框（放在最外層，確保所有狀態都能訪問） -->
  <input
    ref="fileInputRef"
    type="file"
    class="hidden"
    accept="image/*"
    multiple
    @change="handleFileUpload"
  />

  <!-- 初始載入畫面：首次上傳檔案時顯示 -->
  <InitialLoading v-if="initialLoading" />

  <!-- 空狀態：沒有任何發票時顯示的上傳界面 -->
  <EmptyState
    v-else-if="sessions.length === 0"
    @file-change="handleFileUpload"
  />

  <!-- 工作區：顯示已上傳的發票列表和編輯界面 -->
  <div
    v-else
    class="h-screen bg-[#F5F5F7] flex overflow-hidden relative"
    :class="{ 'drag-over': isDragging }"
    @dragover="handleDragOver"
    @dragleave="handleDragLeave"
    @drop="handleDrop"
  >
    <!-- 拖曳遮罩層 -->
    <div
      v-if="isDragging"
      class="absolute inset-0 z-50 bg-blue-500/10 backdrop-blur-sm flex items-center justify-center pointer-events-none"
    >
      <div
        class="bg-white/95 backdrop-blur-xl rounded-3xl shadow-2xl p-12 border-4 border-dashed border-blue-500 animate-pulse"
      >
        <div class="flex flex-col items-center gap-4">
          <div
            class="w-20 h-20 rounded-full bg-blue-100 flex items-center justify-center text-blue-600"
          >
            <Upload :size="40" :stroke-width="2" class="animate-bounce" />
          </div>
          <div class="text-center space-y-2">
            <p class="font-bold text-xl text-blue-700">放開以上傳檔案</p>
            <p class="text-blue-600 text-sm">支援多個圖片檔案</p>
          </div>
        </div>
      </div>
    </div>
    <!-- 側邊欄：發票列表（桌面版顯示，手機版隱藏） -->
    <SessionList
      :sessions="sessions"
      :active-id="activeId"
      :is-saving-all="isSavingAll"
      @select="setActiveId"
      @add-more="fileInputRef?.click()"
      @delete="handleDeleteSession"
      @save-all="handleSaveAll"
      @delete-all="handleDeleteAll"
      class="hidden md:flex"
    />

    <!-- 主內容區 -->
    <div class="flex-1 flex flex-col h-full overflow-hidden relative">
      <!-- 背景氛圍效果 -->
      <div
        class="absolute top-0 left-0 w-full h-full overflow-hidden -z-10 pointer-events-none"
      >
        <div class="absolute top-0 left-0 w-full h-full bg-slate-50/50" />
      </div>

      <!-- 如果有選中的發票，顯示預覽和編輯界面 -->
      <template v-if="activeSession && sessions.length > 0">
        <!-- 手機版：上方 Swiper 圖片滑動 + 下方編輯器 -->
        <div
          class="flex-1 flex flex-col h-full overflow-hidden md:hidden relative"
        >
          <!-- 手機版頂部操作欄 -->
          <div
            class="flex-shrink-0 bg-white/80 backdrop-blur-xl border-b border-slate-200/60"
          >
            <div class="flex items-center px-4 py-3 gap-4">
              <h2 class="font-semibold text-slate-800 text-base">
                發票 ({{ sessions.length }})
              </h2>

              <button
                v-if="hasReviewableSessions && sessions.length > 0"
                @click="handleSaveAll"
                :disabled="isSavingAll"
                class="px-4 ml-auto py-2.5 bg-blue-600 text-white rounded-lg font-medium text-sm cursor-pointer hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
              >
                <Loader2 v-if="isSavingAll" class="animate-spin" :size="16" />
                <span v-if="isSavingAll">保存中...</span>
                <span v-else>保存所有 ({{ reviewableCount }})</span>
              </button>

              <button
                @click="fileInputRef?.click()"
                class="p-2.5 rounded-full cursor-pointer hover:bg-blue-50 text-blue-600 transition-all shadow-sm hover:shadow-md"
                title="上傳更多"
              >
                <Plus :size="20" />
              </button>
            </div>
          </div>

          <!-- 上方：Swiper 圖片滑動區域 -->
          <InvoiceSwiper
            :sessions="sessions"
            :active-index="activeIndex"
            @slide-change="handleSwiperSlideChange"
          />

          <!-- 下方：發票編輯器 -->
          <div class="flex-1 overflow-y-auto">
            <InvoiceEditor
              :session="activeSession"
              @change="handleDataChange"
              @save="handleSaveCurrent"
              @delete="handleDeleteCurrent"
              @manual-input="handleManualInput"
            />
          </div>
        </div>

        <!-- 桌面版：左側預覽 + 右側編輯器 -->
        <div
          class="hidden md:flex flex-1 flex-col md:flex-row h-full overflow-hidden"
        >
          <!-- 左側：發票預覽 -->
          <InvoicePreview :session="activeSession" />

          <!-- 右側：發票編輯器 -->
          <InvoiceEditor
            :session="activeSession"
            @change="handleDataChange"
            @save="handleSaveCurrent"
            @delete="handleDeleteCurrent"
            @manual-input="handleManualInput"
          />
        </div>
      </template>

      <!-- 沒有選中發票時的提示 -->
      <div
        v-else
        class="flex-1 flex items-center justify-center text-slate-400"
      >
        選擇一張發票以查看詳情
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { recognizeInvoice, saveInvoices } from './api/invoice'
import type { InvoiceData, InvoiceSession } from './types/invoice'
import InitialLoading from './components/InitialLoading.vue'
import EmptyState from './components/EmptyState.vue'
import SessionList from './components/SessionList.vue'
import InvoicePreview from './components/InvoicePreview.vue'
import InvoiceEditor from './components/InvoiceEditor.vue'
import InvoiceSwiper from './components/InvoiceSwiper.vue'
import { Plus, Loader2, Upload } from 'lucide-vue-next'

// ==================== 響應式狀態 ====================

/** 所有發票 session 的列表 */
const sessions = ref<InvoiceSession[]>([])

/** 當前選中的發票 ID */
const activeId = ref<string | null>(null)

/** 檔案輸入框的引用 */
const fileInputRef = ref<HTMLInputElement | null>(null)

/** 初始載入狀態標誌（首次上傳時顯示載入動畫） */
const initialLoading = ref(false)

/** 是否正在批量保存所有發票 */
const isSavingAll = ref(false)

/** 是否正在拖曳檔案 */
const isDragging = ref(false)

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

/** 處理 Swiper 滑動變更事件 */
const handleSwiperSlideChange = (sessionId: string) => {
  activeId.value = sessionId
}

// ==================== 業務邏輯函數 ====================

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

  // 如果是首次上傳（空狀態），顯示載入動畫
  const isFirstUpload = sessions.value.length === 0
  if (isFirstUpload) {
    initialLoading.value = true
    // 等待顯示載入動畫（模擬檔案準備過程）
    await new Promise((resolve) => setTimeout(resolve, 1800))
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

  // 如果是首次上傳，隱藏載入畫面
  if (isFirstUpload) {
    // 短暫延遲確保平滑過渡
    await new Promise((resolve) => setTimeout(resolve, 200))
    initialLoading.value = false
  }
}

/**
 * 處理檔案上傳（從 input 元素）
 * @param event - 檔案選擇事件
 */
const handleFileUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const files = Array.from(target.files || [])
  await processFiles(files)
  // 清空輸入框
  if (target.value) target.value = ''
}

/**
 * 處理拖曳上傳
 * @param event - 拖曳事件
 */
const handleDragOver = (event: DragEvent) => {
  event.preventDefault()
  event.stopPropagation()
  // 檢查是否為檔案拖曳
  if (event.dataTransfer?.types.includes('Files')) {
    isDragging.value = true
  }
}

/**
 * 處理拖曳離開
 * @param event - 拖曳事件
 */
const handleDragLeave = (event: DragEvent) => {
  event.preventDefault()
  event.stopPropagation()
  // 檢查是否真的離開了拖曳區域
  const relatedTarget = event.relatedTarget as HTMLElement | null
  const currentTarget = event.currentTarget as HTMLElement | null
  if (
    !relatedTarget ||
    (currentTarget && !currentTarget.contains(relatedTarget))
  ) {
    isDragging.value = false
  }
}

/**
 * 處理拖曳放下
 * @param event - 拖曳事件
 */
const handleDrop = async (event: DragEvent) => {
  event.preventDefault()
  event.stopPropagation()
  isDragging.value = false

  const files = Array.from(event.dataTransfer?.files || [])
  await processFiles(files)
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
      const newSessions = sessions.value.filter((s) => s.id !== activeId.value)
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
 * 刪除當前選中的發票
 */
const handleDeleteCurrent = () => {
  if (!activeId.value) return
  handleDeleteSession(activeId.value)
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
        activeId.value = sessions.value.length > 0 ? sessions.value[0].id : null
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
</script>
