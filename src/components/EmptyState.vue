<template>
  <!-- 空狀態：沒有任何發票時顯示的上傳界面 -->
  <div
    class="min-h-screen bg-[#F5F5F7] flex flex-col items-center justify-center p-6 text-center relative"
    @dragover="handleDragOver"
    @dragleave="handleDragLeave"
    @drop="handleDrop"
  >
    <!-- 拖曳時的頁面遮罩（覆蓋整個頁面） -->
    <transition name="fade">
      <div
        v-if="isDragging"
        class="fixed inset-0 bg-blue-500/10 backdrop-blur-sm z-40 flex items-center justify-center pointer-events-none"
      >
        <div
          class="bg-white rounded-3xl shadow-2xl p-12 border-4 border-dashed border-blue-500"
        >
          <div class="flex flex-col items-center gap-4">
            <div
              class="w-20 h-20 rounded-full bg-blue-100 flex items-center justify-center text-blue-600"
            >
              <Upload :size="40" :stroke-width="2" />
            </div>
            <div class="text-center space-y-2">
              <p class="font-bold text-xl text-blue-700">放開以上傳檔案</p>
              <p class="text-blue-600 text-sm">支援多個圖片檔案</p>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <!-- 標題區域 -->
    <div class="mb-8 relative z-10" :class="{ 'opacity-60': isDragging }">
      <h1 class="text-4xl font-bold text-slate-800 mb-2">發票</h1>
      <p class="text-lg text-slate-600">掃描與整理</p>
    </div>

    <!-- 白色圓角卡片 -->
    <div
      class="w-full max-w-md bg-white rounded-3xl shadow-lg p-8 space-y-6 relative z-10"
      :class="{ 'opacity-60': isDragging }"
    >
      <!-- 中央掃描圖示 -->
      <div class="flex justify-center">
        <div
          class="w-20 h-20 rounded-full bg-slate-100 flex items-center justify-center"
        >
          <Scan class="text-slate-500" :size="40" :stroke-width="1.5" />
        </div>
      </div>

      <!-- 動作按鈕區域 -->
      <div class="space-y-3">
        <!-- 上傳圖片按鈕 -->
        <button
          @click="fileInputRef?.click()"
          class="w-full p-4 bg-blue-600 hover:bg-blue-700 text-white rounded-2xl shadow-md hover:shadow-lg transition-all duration-200 flex items-center justify-center gap-3 font-medium"
        >
          <Upload :size="20" :stroke-width="2" />
          <span>上傳圖片</span>
        </button>

        <!-- Camera 按鈕 -->
        <button
          @click="showCamera = true"
          class="w-full p-4 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-2xl shadow-sm hover:shadow-md transition-all duration-200 flex items-center justify-center gap-3 font-medium"
        >
          <Camera :size="20" :stroke-width="2" />
          <span>相機</span>
        </button>
      </div>

      <!-- 上傳提示文字 -->
      <div class="pt-4 space-y-2 border-t border-slate-100">
        <p class="text-xs text-slate-500">支援拖曳上傳，可一次選擇多張圖片</p>
        <p class="text-sm text-slate-500">支援智慧 AI 提取</p>
      </div>
    </div>

    <!-- 隱藏的檔案輸入框（用於選擇檔案） -->
    <input
      ref="fileInputRef"
      type="file"
      class="hidden"
      accept="image/*"
      multiple
      @change="handleFileChange"
    />

    <!-- 相機組件 -->
    <CameraCapture v-model="showCamera" @capture="handleCameraCapture" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Scan, Upload, Camera } from 'lucide-vue-next'
import CameraCapture from './CameraCapture.vue'

// 檔案輸入框的引用
const fileInputRef = ref<HTMLInputElement | null>(null)
// 相機顯示狀態
const showCamera = ref(false)

// 拖曳狀態
const isDragging = ref(false)

// 定義事件：當用戶選擇檔案時觸發
const emit = defineEmits<{
  fileChange: [event: Event]
}>()

/**
 * 處理檔案選擇變更
 */
const handleFileChange = (event: Event) => {
  emit('fileChange', event)
  // 清空輸入框，允許重複選擇相同檔案
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

/**
 * 處理相機拍攝的照片
 */
const handleCameraCapture = (file: File) => {
  // 創建一個 FileList 來模擬 input 的 files
  const dataTransfer = new DataTransfer()
  dataTransfer.items.add(file)

  // 觸發檔案選擇事件
  if (fileInputRef.value) {
    fileInputRef.value.files = dataTransfer.files
    const changeEvent = new Event('change', { bubbles: true })
    fileInputRef.value.dispatchEvent(changeEvent)
  }
}

/**
 * 處理拖曳進入/懸停
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
 */
const handleDrop = (event: DragEvent) => {
  event.preventDefault()
  event.stopPropagation()
  isDragging.value = false

  const files = Array.from(event.dataTransfer?.files || [])
  if (files.length === 0) return

  // 過濾出圖片檔案
  const imageFiles = files.filter((file) => file.type.startsWith('image/'))
  if (imageFiles.length === 0) {
    return
  }

  // 創建一個 FileList 來模擬 input 的 files
  const dataTransfer = new DataTransfer()
  imageFiles.forEach((file) => dataTransfer.items.add(file))

  // 觸發檔案選擇事件
  if (fileInputRef.value) {
    fileInputRef.value.files = dataTransfer.files
    const changeEvent = new Event('change', { bubbles: true })
    fileInputRef.value.dispatchEvent(changeEvent)
  }
}
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
