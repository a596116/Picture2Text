<template>
  <div
    class="flex-shrink-0 h-80 py-2 bg-slate-100/50 border-b border-slate-200/50 relative flex items-center justify-center"
  >
    <swiper
      ref="swiperRef"
      :modules="swiperModules"
      :slides-per-view="1"
      :space-between="0"
      :initial-slide="initialSlide"
      :navigation="true"
      :pagination="{ clickable: true, dynamicBullets: false }"
      @slide-change="handleSlideChange"
      class="h-full w-full swiper-mobile"
    >
      <swiper-slide
        v-for="session in sessions"
        :key="session.id"
        class="flex items-center justify-center !w-full"
      >
        <div class="w-full max-w-sm mx-auto px-4">
          <div
            class="relative rounded-2xl overflow-hidden shadow-lg ring-1 ring-black/5 bg-white mx-auto"
          >
            <img
              :src="session.previewUrl"
              alt="發票預覽"
              class="w-full h-auto object-contain max-h-72 mx-auto"
            />
            <!-- 載入遮罩 -->
            <div
              v-if="
                session.status === 'processing' ||
                session.status === 'uploading'
              "
              class="absolute inset-0 bg-white/80 backdrop-blur-md flex flex-col items-center justify-center"
            >
              <div class="flex flex-col items-center">
                <div class="mb-6">
                  <Loader2
                    class="animate-spin text-blue-600"
                    :size="40"
                    stroke-width="2"
                  />
                </div>
                <span
                  class="font-semibold text-slate-800 tracking-wide bg-white px-6 py-2.5 rounded-full shadow-lg text-base"
                >
                  {{
                    session.status === 'uploading' ? '上傳中...' : 'AI 處理中'
                  }}
                </span>
                <p class="mt-3 text-sm text-slate-600">
                  {{
                    session.status === 'uploading'
                      ? '正在上傳圖片'
                      : '正在提取發票資訊'
                  }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </swiper-slide>
    </swiper>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { Loader2 } from 'lucide-vue-next'
import { Swiper, SwiperSlide } from 'swiper/vue'
import { Navigation, Pagination } from 'swiper/modules'
import 'swiper/css'
import 'swiper/css/navigation'
import 'swiper/css/pagination'
import type { InvoiceSession } from '../types/invoice'

// Swiper modules
const swiperModules = [Navigation, Pagination]

interface Props {
  sessions: InvoiceSession[]
  activeIndex: number
}

const props = defineProps<Props>()

const emit = defineEmits<{
  slideChange: [sessionId: string]
}>()

/** Swiper 實例引用 */
const swiperRef = ref<any>(null)

/** 是否正在由 Swiper 內部更新（防止循環更新） */
const isSwiperUpdating = ref(false)

/** 初始滑動索引 */
const initialSlide = computed(() => props.activeIndex)

/** 處理 Swiper 滑動變更事件 */
const handleSlideChange = (swiper: any) => {
  const index = swiper.activeIndex
  if (props.sessions[index]) {
    const sessionId = props.sessions[index].id
    isSwiperUpdating.value = true
    emit('slideChange', sessionId)
    // 使用 setTimeout 確保狀態更新完成後重置標誌
    setTimeout(() => {
      isSwiperUpdating.value = false
    }, 100)
  }
}

/**
 * 監聽 activeIndex 變化，同步 Swiper 的 activeIndex
 */
watch(
  () => props.activeIndex,
  (newIndex) => {
    // 如果正在由 Swiper 內部更新，則跳過同步，避免循環更新
    if (isSwiperUpdating.value) return

    if (swiperRef.value && swiperRef.value.swiper) {
      const currentIndex = swiperRef.value.swiper.activeIndex
      if (currentIndex !== newIndex) {
        swiperRef.value.swiper.slideTo(newIndex, 300)
      }
    }
  }
)
</script>

<style scoped lang="scss">
/* Swiper 手機版樣式 */
.swiper-mobile {
  display: flex;
  align-items: center;
  justify-content: center;
}

/* Swiper 導航箭頭樣式 - 現代化設計 */
.swiper-mobile :deep(.swiper-button-next),
.swiper-mobile :deep(.swiper-button-prev) {
  width: 30px !important;
  height: 30px !important;
  padding: 6px;
  background: rgba(255, 255, 255, 0.95) !important;
  backdrop-filter: blur(20px) saturate(180%) !important;
  border-radius: 50% !important;
  box-shadow: 0 2px 16px rgba(0, 0, 0, 0.12), 0 0 0 1px rgba(0, 0, 0, 0.04) !important;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1) !important;
  margin-top: 0 !important;
  top: 50% !important;
  transform: translateY(-50%) !important;
  z-index: 10 !important;
}

.swiper-mobile :deep(.swiper-button-next) {
  right: 12px !important;
}

.swiper-mobile :deep(.swiper-button-prev) {
  left: 12px !important;
}

.swiper-mobile :deep(.swiper-button-next:after),
.swiper-mobile :deep(.swiper-button-prev:after) {
  font-size: 20px !important;
  font-weight: 600 !important;
  color: #1d1d1f !important;
  line-height: 1 !important;
}

.swiper-mobile :deep(.swiper-button-next:hover),
.swiper-mobile :deep(.swiper-button-prev:hover) {
  background: rgba(255, 255, 255, 1) !important;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.16), 0 0 0 1px rgba(0, 0, 0, 0.06) !important;
  transform: translateY(-50%) scale(1.05) !important;
}

.swiper-mobile :deep(.swiper-button-next:active),
.swiper-mobile :deep(.swiper-button-prev:active) {
  transform: translateY(-50%) scale(0.98) !important;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.12), 0 0 0 1px rgba(0, 0, 0, 0.04) !important;
}

.swiper-mobile :deep(.swiper-button-next.swiper-button-disabled),
.swiper-mobile :deep(.swiper-button-prev.swiper-button-disabled) {
  opacity: 0.3 !important;
  cursor: not-allowed !important;
  background: rgba(255, 255, 255, 0.6) !important;
}

/* Swiper 分頁指示器樣式 - 更明顯 */
.swiper-mobile :deep(.swiper-pagination) {
  bottom: 16px !important;
  left: 50% !important;
  transform: translateX(-50%) !important;
  width: auto !important;
}

.swiper-mobile :deep(.swiper-pagination-bullet) {
  width: 10px !important;
  height: 10px !important;
  background: rgba(255, 255, 255, 0.5) !important;
  opacity: 1 !important;
  transition: all 0.3s ease !important;
  margin: 0 4px !important;
  border: 2px solid rgba(255, 255, 255, 0.8) !important;
}

.swiper-mobile :deep(.swiper-pagination-bullet-active) {
  width: 28px !important;
  height: 10px !important;
  border-radius: 5px !important;
  background: rgba(255, 255, 255, 0.95) !important;
  border-color: rgba(255, 255, 255, 1) !important;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3) !important;
}
</style>
