<template>
  <teleport to="body">
    <div
      v-if="isOpen"
      class="fixed inset-0 z-50 bg-black flex flex-col"
      @click.self="handleClose"
    >
      <!-- 標題欄（浮動在頂部） -->
      <div
        class="absolute top-0 left-0 right-0 z-10 flex items-center justify-between p-4 bg-gradient-to-b from-black/60 to-transparent"
      >
        <h3 class="text-lg font-semibold text-white">拍攝發票</h3>
        <button
          @click="handleClose"
          class="p-2 rounded-full bg-black/50 hover:bg-black/70 text-white transition-colors"
        >
          <X :size="20" />
        </button>
      </div>

      <!-- 相機預覽區域（全屏） -->
      <div
        class="relative flex-1 flex items-center justify-center overflow-hidden"
      >
        <video
          v-if="stream && !error"
          ref="videoRef"
          autoplay
          playsinline
          class="w-full h-full object-contain"
        />
        <div
          v-else-if="error"
          class="flex flex-col items-center gap-4 text-white p-8"
        >
          <AlertCircle :size="48" class="text-red-400" />
          <p class="text-lg font-medium">{{ error }}</p>
          <button
            @click="startCamera"
            class="px-6 py-3 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-colors"
          >
            重試
          </button>
        </div>
        <div v-else class="flex flex-col items-center gap-4 text-white">
          <Loader2 :size="48" class="animate-spin text-blue-400" />
          <p class="text-lg font-medium">正在啟動相機...</p>
        </div>
      </div>

      <!-- 控制按鈕（浮動在底部） -->
      <div
        v-if="stream && !error"
        class="absolute bottom-0 left-0 right-0 z-10 p-6 bg-gradient-to-t from-black/60 to-transparent flex items-center justify-center gap-4"
      >
        <button
          @click="handleClose"
          class="px-6 py-3 bg-white/20 backdrop-blur-md text-white rounded-lg font-medium hover:bg-white/30 transition-colors"
        >
          取消
        </button>
        <button
          @click="capturePhoto"
          class="w-16 h-16 rounded-full bg-white hover:bg-white/90 transition-colors flex items-center justify-center shadow-lg hover:shadow-xl"
          :disabled="isCapturing"
        >
          <Camera v-if="!isCapturing" :size="32" class="text-slate-800" />
          <Loader2 v-else :size="32" class="text-slate-800 animate-spin" />
        </button>
        <button
          v-if="capturedImage"
          @click="handleConfirm"
          class="px-6 py-3 bg-green-600 text-white rounded-lg font-medium hover:bg-green-700 transition-colors"
        >
          確認使用
        </button>
      </div>

      <!-- 預覽拍攝的照片 -->
      <div
        v-if="capturedImage && stream"
        class="fixed inset-0 z-60 bg-black flex flex-col"
      >
        <!-- 標題欄 -->
        <div
          class="absolute top-0 left-0 right-0 z-10 flex items-center justify-between p-4 bg-gradient-to-b from-black/60 to-transparent"
        >
          <h3 class="text-lg font-semibold text-white">預覽照片</h3>
          <button
            @click="capturedImage = null"
            class="p-2 rounded-full bg-black/50 hover:bg-black/70 text-white transition-colors"
          >
            <X :size="20" />
          </button>
        </div>

        <!-- 圖片預覽 -->
        <div class="flex-1 flex items-center justify-center overflow-hidden">
          <img
            :src="capturedImage"
            alt="預覽"
            class="w-full h-full object-contain"
          />
        </div>

        <!-- 控制按鈕 -->
        <div
          class="absolute bottom-0 left-0 right-0 z-10 p-6 bg-gradient-to-t from-black/60 to-transparent flex items-center justify-center gap-4"
        >
          <button
            @click="capturedImage = null"
            class="px-6 py-3 bg-white/20 backdrop-blur-md text-white rounded-lg font-medium hover:bg-white/30 transition-colors"
          >
            重新拍攝
          </button>
          <button
            @click="handleConfirm"
            class="px-6 py-3 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-colors"
          >
            確認使用
          </button>
        </div>
      </div>
    </div>
  </teleport>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'
import { useUserMedia } from '@vueuse/core'
import { Camera, X, AlertCircle, Loader2 } from 'lucide-vue-next'

interface Props {
  modelValue: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  capture: [file: File]
}>()

const isOpen = ref(props.modelValue)
const videoRef = ref<HTMLVideoElement | null>(null)
const capturedImage = ref<string | null>(null)
const isCapturing = ref(false)
const error = ref<string | null>(null)

// 使用 VueUse 的 useUserMedia
const { stream, start, stop, isSupported } = useUserMedia({
  constraints: {
    video: {
      facingMode: 'environment', // 使用後置相機
    },
    audio: false,
  },
})

// 監聽 modelValue 變化
watch(
  () => props.modelValue,
  (newValue) => {
    isOpen.value = newValue
    if (newValue) {
      startCamera()
    } else {
      stopCamera()
    }
  }
)

// 監聽 isOpen 變化
watch(isOpen, (newValue) => {
  emit('update:modelValue', newValue)
  if (!newValue) {
    stopCamera()
    capturedImage.value = null
    error.value = null
  }
})

// 監聽 stream 變化，自動設置到 video 元素
watch(stream, (newStream) => {
  if (newStream && videoRef.value) {
    videoRef.value.srcObject = newStream
  }
})

// 啟動相機
const startCamera = async () => {
  error.value = null
  capturedImage.value = null

  if (!isSupported.value) {
    error.value = '您的瀏覽器不支援相機功能'
    return
  }

  try {
    const mediaStream = await start()
    // 等待視頻元素準備好並設置 stream
    if (videoRef.value && mediaStream) {
      videoRef.value.srcObject = mediaStream
      await new Promise((resolve) => {
        if (videoRef.value) {
          videoRef.value.onloadedmetadata = () => {
            resolve(void 0)
          }
        } else {
          resolve(void 0)
        }
      })
    }
  } catch (err: any) {
    console.error('啟動相機失敗:', err)
    if (err.name === 'NotAllowedError') {
      error.value = '請允許訪問相機權限'
    } else if (err.name === 'NotFoundError') {
      error.value = '未找到相機設備'
    } else {
      error.value = '啟動相機失敗，請重試'
    }
  }
}

// 停止相機
const stopCamera = () => {
  if (stream.value) {
    stream.value.getTracks().forEach((track) => track.stop())
  }
  stop()
  if (videoRef.value) {
    videoRef.value.srcObject = null
  }
}

// 拍照
const capturePhoto = () => {
  if (!videoRef.value || !stream.value) return

  isCapturing.value = true

  try {
    const canvas = document.createElement('canvas')
    const video = videoRef.value
    canvas.width = video.videoWidth
    canvas.height = video.videoHeight

    const ctx = canvas.getContext('2d')
    if (!ctx) {
      throw new Error('無法創建畫布上下文')
    }

    ctx.drawImage(video, 0, 0)

    // 將畫布轉換為 blob，然後轉為 data URL 用於預覽
    canvas.toBlob(
      (blob) => {
        if (blob) {
          capturedImage.value = URL.createObjectURL(blob)
          isCapturing.value = false
        }
      },
      'image/jpeg',
      0.9
    )
  } catch (err) {
    console.error('拍照失敗:', err)
    error.value = '拍照失敗，請重試'
    isCapturing.value = false
  }
}

// 確認使用照片
const handleConfirm = async () => {
  if (!capturedImage.value) return

  try {
    // 將 data URL 轉換為 File 對象
    const response = await fetch(capturedImage.value)
    const blob = await response.blob()
    const file = new File([blob], `camera-${Date.now()}.jpg`, {
      type: 'image/jpeg',
    })

    emit('capture', file)
    handleClose()
  } catch (err) {
    console.error('處理照片失敗:', err)
    error.value = '處理照片失敗，請重試'
  }
}

// 關閉相機
const handleClose = () => {
  isOpen.value = false
}

// 組件卸載時停止相機
onUnmounted(() => {
  stopCamera()
})
</script>

<style scoped>
video {
  /* 後置相機保持正常方向，不鏡像 */
  transform: none;
}
</style>
