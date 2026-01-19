<template>
  <teleport to="body">
    <div
      v-if="isOpen"
      class="fixed inset-0 z-50 bg-black flex flex-col"
      @click.self="handleClose"
    >
      <!-- 相機預覽區域（全屏） -->
      <div
        ref="cameraContainerRef"
        class="relative w-full h-full bg-black overflow-hidden touch-none"
        style="touch-action: none"
      >
        <!-- 視頻容器：用於實現高品質縮放 -->
        <div
          v-if="localStream && !error"
          class="absolute inset-0 w-full h-full overflow-hidden"
          :style="videoContainerStyle"
        >
          <video
            ref="videoRef"
            autoplay
            playsinline
            class="absolute w-full h-full object-cover"
            :class="{
              'scale-x-[-1]':
                currentFacingMode === 'user' && supportsHardwareZoom,
            }"
            :style="videoStyle"
          />
        </div>

        <!-- 錯誤顯示 -->
        <div
          v-else-if="error"
          class="absolute inset-0 flex flex-col items-center justify-center gap-4 text-white p-8 bg-zinc-900"
        >
          <AlertCircle :size="48" class="text-red-400" />
          <p class="text-lg font-medium">{{ error }}</p>
          <button
            @click="startCamera"
            class="px-6 py-3 bg-green-500 text-white rounded-lg font-medium hover:bg-green-600 transition-colors"
          >
            重試
          </button>
          <button
            @click="handleClose"
            class="mt-4 text-zinc-400 hover:text-white transition-colors"
          >
            取消
          </button>
        </div>

        <!-- 加載中 -->
        <div
          v-else
          class="absolute inset-0 flex flex-col items-center justify-center gap-4 text-white bg-zinc-900"
        >
          <Loader2 :size="48" class="animate-spin text-green-500" />
          <p class="text-lg font-medium">正在啟動相機...</p>
        </div>

        <!-- 掃描介面遮罩 (當有畫面時顯示) -->
        <div
          v-if="localStream && !error"
          class="absolute inset-0 z-10 pointer-events-none"
        >
          <!-- 背景遮罩 (上下左右四塊黑色半透明) -->
          <div class="absolute inset-0 bg-black/50 mask-overlay"></div>

          <!-- 掃描框區域 -->
          <div class="absolute inset-0 flex items-center justify-center">
            <!-- 掃描框容器: 寬度 80%, 高度可能固定或比例 -->
            <div class="relative w-[80%] aspect-3/4 max-w-md max-h-[70vh]">
              <!-- 四個角落的綠色框線 -->
              <div
                class="absolute top-0 left-0 w-8 h-8 border-l-4 border-t-4 border-green-400 rounded-tl-lg"
              ></div>
              <div
                class="absolute top-0 right-0 w-8 h-8 border-r-4 border-t-4 border-green-400 rounded-tr-lg"
              ></div>
              <div
                class="absolute bottom-0 left-0 w-8 h-8 border-l-4 border-b-4 border-green-400 rounded-bl-lg"
              ></div>
              <div
                class="absolute bottom-0 right-0 w-8 h-8 border-r-4 border-b-4 border-green-400 rounded-br-lg"
              ></div>

              <!-- 掃描線動畫 -->
              <!-- <div
                class="absolute left-0 right-0 h-0.5 bg-green-400 shadow-[0_0_8px_rgba(74,222,128,0.8)] animate-scan"
              ></div> -->
            </div>
          </div>
        </div>

        <!-- 頂部 UI -->
        <div
          class="absolute top-2 left-0 right-0 z-20 p-4 pt-safe flex justify-between items-start"
        >
          <!-- 左上角縮放指示器 -->
          <div
            v-if="showZoomIndicator"
            class="flex items-center gap-2 px-3 py-1.5 bg-black/60 backdrop-blur-md rounded-full text-white text-sm font-medium border border-white/20"
          >
            <span>{{ displayZoomText }}</span>
          </div>
          <div v-else class="w-4"></div>

          <!-- 右上角功能區 -->
          <div class="flex gap-4">
            <!-- 閃光燈開關 (模擬) -->
            <button
              @click="toggleFlash"
              class="p-3 rounded-full bg-black/40 backdrop-blur-md text-white hover:bg-black/60 transition-colors border border-white/10"
            >
              <ZapOff v-if="!flashEnabled" :size="20" />
              <Zap v-else :size="20" class="text-yellow-400" />
            </button>

            <!-- 關閉按鈕 -->
            <button
              @click="handleClose"
              class="p-3 rounded-full bg-black/40 backdrop-blur-md text-white hover:bg-black/60 transition-colors border border-white/10"
            >
              <X :size="20" />
            </button>
          </div>
        </div>

        <!-- 底部 UI -->
        <div
          class="absolute bottom-0 left-0 right-0 z-20 pb-8 pt-12 bg-linear-to-t from-black/80 to-transparent flex flex-col items-center gap-8"
        >
          <!-- 拍照控制列 -->
          <div class="w-full flex items-center justify-around px-12">
            <!-- 左側按鈕佔位 (保持中間按鈕居中) -->
            <!-- <div class="w-12 h-12"></div> -->

            <!-- 拍攝按鈕 -->
            <button
              @click="capturePhoto"
              class="relative w-20 h-20 rounded-full border-4 border-white flex items-center justify-center bg-transparent hover:bg-white/10 transition-all active:scale-95"
              :disabled="isCapturing"
            >
              <div
                class="w-16 h-16 rounded-full bg-white transition-transform duration-200"
                :class="{ 'scale-90': isCapturing }"
              ></div>
              <Loader2
                v-if="isCapturing"
                :size="32"
                class="absolute text-slate-800 animate-spin"
              />
            </button>

            <!-- 切換鏡頭 / 相簿 -->
            <!-- <button
              @click="switchCamera"
              class="w-12 h-12 rounded-full bg-white/10 backdrop-blur-md flex items-center justify-center text-white hover:bg-white/20 transition-colors border border-white/10"
            >
              <RotateCcw :size="20" />
            </button> -->
          </div>
        </div>
      </div>

      <!-- 預覽拍攝的照片 (保持原有邏輯但優化樣式) -->
      <div
        v-if="capturedImage"
        class="fixed inset-0 z-60 bg-black flex flex-col"
      >
        <!-- 圖片預覽 -->
        <div class="flex-1 relative bg-black">
          <img
            :src="capturedImage"
            alt="預覽"
            class="w-full h-full object-contain"
          />
        </div>

        <!-- 底部 UI -->
        <div
          class="absolute bottom-0 left-0 right-0 z-20 pb-8 pt-12 bg-linear-to-t from-black/80 to-transparent flex flex-col items-center gap-8"
        >
          <!-- 控制按鈕 -->
          <div class="w-full flex items-center justify-center gap-6">
            <!-- 取消/重新拍攝按鈕 -->
            <button
              @click="capturedImage = null"
              class="w-14 h-14 rounded-full bg-black/60 backdrop-blur-md text-white flex items-center justify-center hover:bg-black/80 transition-all active:scale-95 shadow-lg border border-white/20"
              title="重新拍攝"
            >
              <X :size="24" />
            </button>

            <!-- 確認使用按鈕 -->
            <button
              @click="handleConfirm"
              class="w-14 h-14 rounded-full bg-green-500 text-white flex items-center justify-center hover:bg-green-600 transition-all active:scale-95 shadow-lg shadow-green-500/30"
              title="確認使用"
            >
              <Check :size="24" />
            </button>
          </div>
        </div>
      </div>
    </div>
  </teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, onUnmounted, nextTick } from 'vue'
import { useUserMedia } from '@vueuse/core'
import { X, AlertCircle, Loader2, Zap, ZapOff, Check } from 'lucide-vue-next'

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
const cameraContainerRef = ref<HTMLDivElement | null>(null)
const capturedImage = ref<string | null>(null)
const capturedImageUrl = ref<string | null>(null) // 用於追蹤需要釋放的 URL
const isCapturing = ref(false)
const error = ref<string | null>(null)
const currentFacingMode = ref<'user' | 'environment'>('environment')
const flashEnabled = ref(false)
const localStream = ref<MediaStream | null>(null)

// 縮放相關狀態
const displayZoom = ref(1) // 當前顯示的縮放級別
const minZoom = ref(1) // 最小縮放級別
const maxZoom = ref(4) // 最大縮放級別
const initialDistance = ref(0) // 雙指初始距離
const initialZoom = ref(1) // 開始縮放時的初始縮放級別
const showZoomIndicator = ref(false) // 是否顯示縮放指示器
const supportsHardwareZoom = ref(false) // 是否支援硬體縮放
const isZooming = ref(false) // 是否正在進行縮放操作
let zoomIndicatorTimer: ReturnType<typeof setTimeout> | null = null
let hardwareZoomRaf: number | null = null // 用於節流硬體縮放的 RAF ID
let lastHardwareZoomTime = 0 // 上次硬體縮放的時間
const HARDWARE_ZOOM_THROTTLE = 50 // 硬體縮放節流時間（毫秒）

// 計算視頻容器樣式
const videoContainerStyle = computed(() => {
  return {
    overflow: 'hidden',
  }
})

// 計算視頻元素樣式（高品質縮放實現）
const videoStyle = computed(() => {
  // 如果支援硬體縮放，不需要 CSS 處理
  if (supportsHardwareZoom.value) {
    return {}
  }

  const scale = displayZoom.value

  if (scale <= 1) {
    return {}
  }

  // 使用 transform scale 來放大，但保持 video 元素本身是 100% 尺寸
  // 容器會自動裁剪超出部分，實現裁剪式縮放
  // 這樣可以保持原始解析度，只是顯示放大後的區域
  return {
    transform: `scale(${scale})`,
    transformOrigin: 'center center',
    // 啟用硬體加速
    willChange: 'transform',
    // 確保 video 元素保持 100% 尺寸
    width: '100%',
    height: '100%',
  }
})

// 格式化顯示的縮放級別
const displayZoomText = computed(() => displayZoom.value.toFixed(1) + 'x')

// 使用 VueUse 的 useUserMedia (主要用於 stream 管理)
const { stream, stop } = useUserMedia({
  constraints: {
    video: {
      facingMode: currentFacingMode.value,
    },
    audio: false,
  },
})

// 添加觸控事件監聽器（優化版本）
const attachTouchListeners = () => {
  if (!cameraContainerRef.value) return

  const options: AddEventListenerOptions = { passive: false }
  cameraContainerRef.value.addEventListener(
    'touchstart',
    handleTouchStart,
    options
  )
  cameraContainerRef.value.addEventListener(
    'touchmove',
    handleTouchMove,
    options
  )
  cameraContainerRef.value.addEventListener('touchend', handleTouchEnd, options)
  cameraContainerRef.value.addEventListener(
    'touchcancel',
    handleTouchEnd,
    options
  )
}

// 移除觸控事件監聽器
const removeTouchListeners = () => {
  if (!cameraContainerRef.value) return

  cameraContainerRef.value.removeEventListener('touchstart', handleTouchStart)
  cameraContainerRef.value.removeEventListener('touchmove', handleTouchMove)
  cameraContainerRef.value.removeEventListener('touchend', handleTouchEnd)
  cameraContainerRef.value.removeEventListener('touchcancel', handleTouchEnd)
}

// 清理 blob URL
const cleanupBlobUrl = () => {
  if (capturedImageUrl.value) {
    URL.revokeObjectURL(capturedImageUrl.value)
    capturedImageUrl.value = null
  }
}

// 監聽 modelValue 變化
watch(
  () => props.modelValue,
  (newValue) => {
    isOpen.value = newValue
    if (newValue) {
      startCamera()
      nextTick(() => {
        attachTouchListeners()
      })
    } else {
      stopCamera()
      removeTouchListeners()
    }
  }
)

// 監聽 isOpen 變化
watch(isOpen, (newValue) => {
  emit('update:modelValue', newValue)
  if (!newValue) {
    stopCamera()
    cleanupBlobUrl()
    capturedImage.value = null
    error.value = null
    localStream.value = null
    removeTouchListeners()
  }
})

// 監聽 videoRef 變化，確保流正確設置
watch(
  videoRef,
  (newVideo) => {
    if (newVideo && localStream.value) {
      newVideo.srcObject = localStream.value
    }
  },
  { immediate: true }
)

// 監聽 cameraContainerRef 變化，附加事件監聽器
watch(
  cameraContainerRef,
  (newContainer) => {
    if (newContainer && isOpen.value) {
      attachTouchListeners()
    }
  },
  { immediate: true }
)

// 切換前後鏡頭
// const switchCamera = async () => {
//   currentFacingMode.value =
//     currentFacingMode.value === 'environment' ? 'user' : 'environment'
//   stop()
//   await startCamera()
// }

// 啟動相機 (包含切換鏡頭邏輯)
const startCamera = async () => {
  error.value = null
  capturedImage.value = null

  // 檢查瀏覽器支援
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    error.value = '您的瀏覽器不支援相機功能'
    return
  }

  try {
    // 停止舊的流
    if (localStream.value) {
      localStream.value.getTracks().forEach((track) => track.stop())
      localStream.value = null
    }
    if (stream.value) {
      stream.value.getTracks().forEach((track) => track.stop())
    }
    stop()

    // 清理 video 元素
    if (videoRef.value) {
      videoRef.value.srcObject = null
    }

    // 使用 navigator.mediaDevices.getUserMedia 直接獲取新的流
    // 請求高解析度以支援軟體縮放時的畫質
    const mediaStream = await navigator.mediaDevices.getUserMedia({
      video: {
        facingMode: currentFacingMode.value,
        width: { ideal: 3840, min: 1920 }, // 優先 4K，最低 1080p
        height: { ideal: 2160, min: 1080 },
      },
      audio: false,
    })

    // 保存流到本地 ref
    localStream.value = mediaStream

    // 等待下一個 tick 確保 video 元素已渲染
    await nextTick()

    // 設置到 video 元素
    let video = videoRef.value
    if (!video) {
      // 如果 video 元素還不存在，等待下一個 tick 後再試
      await nextTick()
      video = videoRef.value
    }

    if (video) {
      video.srcObject = mediaStream

      // 等待視頻元素準備好（優化版本）
      await new Promise<void>((resolve, reject) => {
        if (!video) {
          reject(new Error('Video element not found'))
          return
        }

        let timeoutId: ReturnType<typeof setTimeout>

        const cleanup = () => {
          if (video) {
            video.removeEventListener('loadedmetadata', onLoadedMetadata)
            video.removeEventListener('error', onError)
          }
          if (timeoutId) clearTimeout(timeoutId)
        }

        const onLoadedMetadata = () => {
          cleanup()
          resolve()
        }

        const onError = () => {
          cleanup()
          reject(new Error('Video load error'))
        }

        video.addEventListener('loadedmetadata', onLoadedMetadata, {
          once: true,
        })
        video.addEventListener('error', onError, { once: true })

        // 如果已經加載完成，立即 resolve
        if (video.readyState >= 2) {
          cleanup()
          resolve()
          return
        }

        // 設置超時（減少到 3 秒）
        timeoutId = setTimeout(() => {
          cleanup()
          // 即使超時也 resolve，讓視頻繼續嘗試播放
          resolve()
        }, 3000)
      })
    }
  } catch (err: any) {
    console.error('啟動相機失敗:', err)
    localStream.value = null
    if (
      err.name === 'NotAllowedError' ||
      err.name === 'PermissionDeniedError'
    ) {
      error.value = '請允許訪問相機權限'
    } else if (
      err.name === 'NotFoundError' ||
      err.name === 'DevicesNotFoundError'
    ) {
      error.value = '未找到相機設備'
    } else if (
      err.name === 'NotReadableError' ||
      err.name === 'TrackStartError'
    ) {
      error.value = '相機被其他應用程式使用中'
    } else {
      error.value = `啟動相機失敗：${err.message || '請重試'}`
    }
  }
}

// 停止相機（優化版本）
const stopCamera = () => {
  // 取消待處理的 RAF
  if (hardwareZoomRaf !== null) {
    cancelAnimationFrame(hardwareZoomRaf)
    hardwareZoomRaf = null
  }

  // 停止所有媒體軌道
  if (localStream.value) {
    localStream.value.getTracks().forEach((track) => track.stop())
    localStream.value = null
  }
  if (stream.value) {
    stream.value.getTracks().forEach((track) => track.stop())
  }
  stop()

  // 清理 video 元素
  if (videoRef.value) {
    videoRef.value.srcObject = null
  }

  // 重置縮放狀態
  displayZoom.value = 1
  initialDistance.value = 0
  initialZoom.value = 1
  showZoomIndicator.value = false
  supportsHardwareZoom.value = false
  isZooming.value = false
  lastHardwareZoomTime = 0

  // 清理計時器
  if (zoomIndicatorTimer) {
    clearTimeout(zoomIndicatorTimer)
    zoomIndicatorTimer = null
  }
}

// 切換閃光燈 (僅部分瀏覽器/設備支援)
const toggleFlash = async () => {
  const activeStream = localStream.value || stream.value
  if (!activeStream) return

  const videoTrack = activeStream.getVideoTracks()[0]
  if (!videoTrack) return

  try {
    flashEnabled.value = !flashEnabled.value
    await videoTrack.applyConstraints({
      advanced: [{ torch: flashEnabled.value }] as any,
    })
  } catch (err) {
    console.error('切換閃光燈失敗:', err)
    // 不重置 flashEnabled，有些設備可能不支持 getCapabilities 但支援 applyConstraints
    // 或者可以提示用戶
  }
}

// 拍照（優化版本）
const capturePhoto = () => {
  if (!videoRef.value || !localStream.value) return

  isCapturing.value = true

  try {
    const canvas = document.createElement('canvas')
    const video = videoRef.value
    canvas.width = video.videoWidth
    canvas.height = video.videoHeight

    const ctx = canvas.getContext('2d', {
      alpha: false, // 不需要透明通道，提升性能
      willReadFrequently: false,
    })
    if (!ctx) {
      throw new Error('無法創建畫布上下文')
    }

    // 如果是前置鏡頭，可能需要鏡像翻轉繪製
    if (currentFacingMode.value === 'user') {
      ctx.translate(canvas.width, 0)
      ctx.scale(-1, 1)
    }

    ctx.drawImage(video, 0, 0)

    // 清理舊的 blob URL
    cleanupBlobUrl()

    // 將畫布轉換為 blob，然後轉為 URL 用於預覽
    canvas.toBlob(
      (blob) => {
        if (blob) {
          const url = URL.createObjectURL(blob)
          capturedImage.value = url
          capturedImageUrl.value = url
          isCapturing.value = false
        } else {
          throw new Error('無法生成圖片')
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

// 確認使用照片（優化版本）
const handleConfirm = async () => {
  if (!capturedImage.value) return

  try {
    // 將 blob URL 轉換為 File 對象
    const response = await fetch(capturedImage.value)
    const blob = await response.blob()
    const file = new File([blob], `camera-${Date.now()}.jpg`, {
      type: 'image/jpeg',
    })

    emit('capture', file)

    // 清理並關閉
    cleanupBlobUrl()
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

// 計算兩個觸摸點之間的距離
const getDistance = (touch1: Touch, touch2: Touch): number => {
  const dx = touch1.clientX - touch2.clientX
  const dy = touch1.clientY - touch2.clientY
  return Math.sqrt(dx * dx + dy * dy)
}

// 處理觸摸開始
const handleTouchStart = (e: TouchEvent) => {
  // 只有當確實是雙指觸控時才處理
  if (e.touches.length === 2) {
    // 阻止瀏覽器預設的雙指縮放行為
    e.preventDefault()
    e.stopPropagation()

    isZooming.value = true
    initialDistance.value = getDistance(e.touches[0], e.touches[1])
    initialZoom.value = displayZoom.value
    showZoomIndicator.value = true

    // 清除之前的計時器
    if (zoomIndicatorTimer) {
      clearTimeout(zoomIndicatorTimer)
      zoomIndicatorTimer = null
    }
  }
}

// 處理觸摸移動（優化版本）
const handleTouchMove = (e: TouchEvent) => {
  // 如果是雙指且正在縮放，則處理並阻止預設行為
  if (e.touches.length === 2) {
    e.preventDefault()
    e.stopPropagation()

    if (initialDistance.value > 0) {
      const currentDistance = getDistance(e.touches[0], e.touches[1])
      const scale = currentDistance / initialDistance.value

      // 計算新的縮放級別
      let newZoom = initialZoom.value * scale

      // 限制縮放範圍
      newZoom = Math.max(minZoom.value, Math.min(maxZoom.value, newZoom))

      displayZoom.value = newZoom

      // 使用 RAF 節流硬體縮放調用
      if (supportsHardwareZoom.value) {
        const now = Date.now()
        if (now - lastHardwareZoomTime >= HARDWARE_ZOOM_THROTTLE) {
          if (hardwareZoomRaf !== null) {
            cancelAnimationFrame(hardwareZoomRaf)
          }
          hardwareZoomRaf = requestAnimationFrame(() => {
            applyHardwareZoom(newZoom)
            lastHardwareZoomTime = Date.now()
            hardwareZoomRaf = null
          })
        }
      }
    }
  }
}

// 處理觸摸結束
const handleTouchEnd = (e: TouchEvent) => {
  // 當手指數量小於 2 時，結束縮放
  if (e.touches.length < 2 && isZooming.value) {
    isZooming.value = false
    initialDistance.value = 0

    // 確保最後的縮放值被應用
    if (supportsHardwareZoom.value && hardwareZoomRaf === null) {
      applyHardwareZoom(displayZoom.value)
    }

    // 延遲隱藏縮放指示器
    if (zoomIndicatorTimer) {
      clearTimeout(zoomIndicatorTimer)
    }
    zoomIndicatorTimer = setTimeout(() => {
      showZoomIndicator.value = false
    }, 1000)
  }
}

// 應用硬體縮放（如果相機支援）- 優化版本
const applyHardwareZoom = async (zoom: number) => {
  if (!supportsHardwareZoom.value) return

  const activeStream = localStream.value || stream.value
  if (!activeStream) return

  const videoTrack = activeStream.getVideoTracks()[0]
  if (!videoTrack) return

  try {
    const capabilities = videoTrack.getCapabilities?.() as any
    if (capabilities && 'zoom' in capabilities) {
      const { min, max } = capabilities.zoom
      const actualZoom = Math.max(min, Math.min(max, zoom))

      await videoTrack.applyConstraints({
        advanced: [{ zoom: actualZoom }] as any,
      })
    }
  } catch (err) {
    console.debug('硬體縮放調用失敗:', err)
    // 如果失敗，標記為不支援硬體縮放
    supportsHardwareZoom.value = false
  }
}

// 初始化縮放能力（優化版本）
const initializeZoomCapabilities = () => {
  const activeStream = localStream.value || stream.value
  if (!activeStream) return

  const videoTrack = activeStream.getVideoTracks()[0]
  if (!videoTrack) return

  try {
    const capabilities = videoTrack.getCapabilities?.() as any
    if (capabilities && 'zoom' in capabilities) {
      supportsHardwareZoom.value = true
      minZoom.value = capabilities.zoom.min || 1
      maxZoom.value = capabilities.zoom.max || 4
      console.log('相機支援硬體縮放:', minZoom.value, '-', maxZoom.value)
    } else {
      supportsHardwareZoom.value = false
      minZoom.value = 1
      maxZoom.value = 4
      console.log('相機不支援硬體縮放，使用 CSS 縮放')
    }
  } catch (err) {
    console.debug('無法獲取縮放能力:', err)
    supportsHardwareZoom.value = false
    minZoom.value = 1
    maxZoom.value = 4
  }
}

// 監聽流變化以初始化縮放能力（優化版本）
watch(localStream, async (newStream) => {
  if (newStream && videoRef.value) {
    videoRef.value.srcObject = newStream
    // 等待 video 元素準備好後再初始化縮放能力
    await nextTick()
    // 使用 RAF 延遲初始化，確保軌道完全準備好
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        initializeZoomCapabilities()
      })
    })
  }
})

// 組件卸載時停止相機並清理所有資源
onUnmounted(() => {
  // 移除事件監聽器
  removeTouchListeners()

  // 停止相機
  stopCamera()

  // 清理 blob URL
  cleanupBlobUrl()

  // 清理計時器
  if (zoomIndicatorTimer) {
    clearTimeout(zoomIndicatorTimer)
    zoomIndicatorTimer = null
  }

  // 取消待處理的 RAF
  if (hardwareZoomRaf !== null) {
    cancelAnimationFrame(hardwareZoomRaf)
    hardwareZoomRaf = null
  }
})
</script>

<style scoped>
/* 掃描線動畫 */
@keyframes scan {
  0% {
    top: 5%;
    opacity: 0.8;
  }
  50% {
    top: 95%;
    opacity: 0.8;
  }
  100% {
    top: 5%;
    opacity: 0.8;
  }
}

.animate-scan {
  animation: scan 2s linear infinite;
}

/* 確保全面屏適配 */
.pt-safe {
  padding-top: env(safe-area-inset-top, 20px);
}

.pb-safe {
  padding-bottom: env(safe-area-inset-bottom, 20px);
}

/* 模擬相機遮罩：中間鏤空 */
.mask-overlay {
  background: transparent;
}

/* 前置鏡頭鏡像由 class 控制 */
</style>
