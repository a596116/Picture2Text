<template>
  <!-- 空狀態：沒有任何發票時顯示的上傳界面 -->
  <div
    class="min-h-screen bg-[#F5F5F7] flex flex-col items-center justify-center p-6 text-center relative"
    @dragover="handleDragOver"
    @dragleave="handleDragLeave"
    @drop="handleDrop"
  >
    <!-- 背景氛圍效果 -->
    <div
      class="fixed top-0 left-0 w-full h-full overflow-hidden -z-10 pointer-events-none"
    >
      <div
        class="absolute -top-[20%] -left-[10%] w-[50%] h-[50%] bg-blue-200/30 rounded-full blur-[100px]"
      />
      <div
        class="absolute top-[40%] -right-[10%] w-[40%] h-[60%] bg-purple-200/30 rounded-full blur-[100px]"
      />
    </div>

    <!-- 拖曳時的頁面遮罩（覆蓋整個頁面） -->
    <transition name="fade">
      <div
        v-if="isDragging"
        class="fixed inset-0 bg-blue-500/10 backdrop-blur-sm z-40 flex items-center justify-center pointer-events-none"
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
    </transition>

    <div
      class="max-w-md w-full space-y-8 animate-in fade-in slide-in-from-bottom-8 duration-700 relative z-10"
      :class="{ 'opacity-60': isDragging }"
    >
      <!-- 應用圖標 -->
      <div
        class="inline-flex items-center justify-center w-16 h-16 rounded-3xl bg-white shadow-xl mb-4"
      >
        <Scan class="text-slate-900" :size="32" />
      </div>

      <!-- 標題和描述 -->
      <h1 class="text-4xl font-bold tracking-tight text-slate-900">發票辨識</h1>
      <p class="text-lg text-slate-500 leading-relaxed">
        使用 AI 快速辨識發票資訊<br />
        上傳多個檔案開始處理
      </p>

      <!-- 上傳區域 -->
      <div
        class="mt-8 p-12 bg-white/60 backdrop-blur-xl rounded-3xl shadow-xl hover:shadow-2xl transition-all duration-300 group cursor-pointer border-dashed border-2 border-slate-300 hover:border-blue-400 relative overflow-hidden"
        @click="fileInputRef?.click()"
      >
        <div class="flex flex-col items-center gap-4 relative z-10">
          <div
            class="w-16 h-16 rounded-full bg-blue-50 flex items-center justify-center text-blue-600 group-hover:scale-110 transition-transform duration-300"
          >
            <Upload :size="32" :stroke-width="1.5" />
          </div>
          <div class="space-y-1 text-center">
            <p class="font-semibold text-lg text-slate-700">點擊上傳發票</p>
            <p class="text-slate-400 text-sm">
              支援 JPG、PNG（可多選）或拖曳檔案到頁面任意處
            </p>
          </div>
        </div>
      </div>

      <!-- 隱藏的檔案輸入框 -->
      <input
        ref="fileInputRef"
        type="file"
        class="hidden"
        accept="image/*"
        multiple
        @change="handleFileChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Scan, Upload } from 'lucide-vue-next'

// 檔案輸入框的引用
const fileInputRef = ref<HTMLInputElement | null>(null)

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
