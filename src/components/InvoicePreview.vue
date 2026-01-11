<template>
  <!-- 發票預覽區域：顯示上傳的發票圖片 -->
  <div
    class="w-full md:w-1/2 p-4 md:p-8 flex flex-col justify-center items-center bg-slate-100/50 border-r border-slate-200/50 overflow-y-auto"
  >
    <div class="w-full max-w-lg">
      <div
        class="relative rounded-2xl overflow-hidden shadow-lg ring-1 ring-black/5 bg-white"
      >
        <!-- 發票圖片 -->
        <el-image
          :src="session.previewUrl"
          alt="發票預覽"
          class="w-full block"
          :preview-src-list="[session.previewUrl]"
          fit="contain"
          hide-on-click-modal
          style="display: block"
        >
          <template #error>
            <div class="flex items-center justify-center h-full text-slate-300">
              <FileText :size="20" />
            </div>
          </template>
        </el-image>

        <!-- 載入遮罩：當正在上傳或處理時顯示 -->
        <div
          v-if="
            session.status === 'processing' || session.status === 'uploading'
          "
          class="absolute inset-0 bg-white/80 backdrop-blur-md flex flex-col items-center justify-center"
        >
          <div class="flex flex-col items-center">
            <!-- 載入圖標：簡單的旋轉圖標 -->
            <div class="mb-6">
              <Loader2
                class="animate-spin text-blue-600"
                :size="40"
                stroke-width="2"
              />
            </div>

            <!-- 狀態文字 -->
            <!-- <span
              class="font-semibold text-slate-800 tracking-wide bg-white px-6 py-2.5 rounded-full shadow-lg text-base"
            >
              {{ session.status === 'uploading' ? '上傳中...' : 'AI 處理中' }}
            </span> -->
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
  </div>
</template>

<script setup lang="ts">
import { FileText, Loader2 } from 'lucide-vue-next'
import type { InvoiceSession } from '../types/invoice'

// 組件屬性：接收當前的發票 session
defineProps<{
  session: InvoiceSession
}>()
</script>

<style scoped>
/* 修復 el-image 底部白邊問題 */
:deep(.el-image) {
  display: block;
  width: 100%;
  line-height: 0;
  vertical-align: top;
}

:deep(.el-image__inner) {
  display: block;
  width: 100%;
  height: auto;
  vertical-align: top;
}

:deep(.el-image__wrapper) {
  display: block;
  width: 100%;
  padding-bottom: 0 !important;
  line-height: 0;
}

:deep(.el-image__error) {
  line-height: normal;
}
</style>
