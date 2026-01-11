<template>
  <div
    class="h-screen bg-[#F5F5F7] flex overflow-hidden relative"
    :class="{ 'drag-over': isDragging }"
    @dragover="handleDragOver"
    @dragleave="handleDragLeave"
    @drop="handleDrop"
  >
    <!-- 隱藏的檔案輸入框（用於點擊上傳） -->
    <input
      ref="fileInputRef"
      type="file"
      class="hidden"
      accept="image/*"
      multiple
      @change="handleFileUpload"
    />

    <!-- 相機組件 -->
    <CameraCapture v-model="showCamera" @capture="handleCameraCapture" />

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
      @select="invoiceStore.setActiveId"
      @add-more="fileInputRef?.click()"
      @take-photo="showCamera = true"
      @delete="invoiceStore.handleDeleteSession"
      @save-all="invoiceStore.handleSaveAll"
      @delete-all="invoiceStore.handleDeleteAll"
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
                @click="invoiceStore.handleSaveAll"
                :disabled="isSavingAll"
                class="px-4 ml-auto py-2.5 bg-blue-600 text-white rounded-lg font-medium text-sm cursor-pointer hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
              >
                <Loader2 v-if="isSavingAll" class="animate-spin" :size="16" />
                <span v-if="isSavingAll">保存中...</span>
                <span v-else>保存所有 ({{ reviewableCount }})</span>
              </button>

              <button
                @click="showCamera = true"
                class="p-2.5 rounded-full cursor-pointer hover:bg-green-50 text-green-600 transition-all shadow-sm hover:shadow-md"
                title="拍照上傳"
              >
                <Camera :size="20" />
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
              @change="invoiceStore.handleDataChange"
              @save="invoiceStore.handleSaveCurrent"
              @delete="invoiceStore.handleDeleteCurrent"
              @manual-input="invoiceStore.handleManualInput"
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
            @change="invoiceStore.handleDataChange"
            @save="invoiceStore.handleSaveCurrent"
            @delete="invoiceStore.handleDeleteCurrent"
            @manual-input="invoiceStore.handleManualInput"
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
import { ref, watch, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import { useInvoiceStore } from '../stores/invoice'
import SessionList from '../components/SessionList.vue'
import InvoicePreview from '../components/InvoicePreview.vue'
import InvoiceEditor from '../components/InvoiceEditor.vue'
import InvoiceSwiper from '../components/InvoiceSwiper.vue'
import CameraCapture from '../components/CameraCapture.vue'
import { Plus, Loader2, Upload, Camera } from 'lucide-vue-next'

const router = useRouter()
const invoiceStore = useInvoiceStore()

/** 檔案輸入框的引用 */
const fileInputRef = ref<HTMLInputElement | null>(null)
/** 相機顯示狀態 */
const showCamera = ref(false)

/** 是否正在拖曳檔案 */
const isDragging = ref(false)

// 從 store 獲取響應式狀態
const {
  sessions,
  activeId,
  activeSession,
  activeIndex,
  hasReviewableSessions,
  reviewableCount,
  isSavingAll,
} = storeToRefs(invoiceStore)

/** 處理 Swiper 滑動變更事件 */
const handleSwiperSlideChange = (sessionId: string) => {
  invoiceStore.setActiveId(sessionId)
}

/**
 * 處理檔案上傳（從 input 元素）
 * @param event - 檔案選擇事件
 */
const handleFileUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const files = Array.from(target.files || [])
  await invoiceStore.processFiles(files)
  // 清空輸入框
  if (target.value) target.value = ''
}

/**
 * 處理相機拍攝的照片
 */
const handleCameraCapture = async (file: File) => {
  await invoiceStore.processFiles([file])
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
  await invoiceStore.processFiles(files)
}

// 如果沒有發票，導航回首頁
watch(
  () => invoiceStore.sessions.length,
  (length) => {
    if (length === 0) {
      router.push('/')
    }
  }
)

// 初始化檢查
onMounted(() => {
  if (invoiceStore.sessions.length === 0) {
    router.push('/')
  }
})
</script>
