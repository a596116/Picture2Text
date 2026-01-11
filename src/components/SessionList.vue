<template>
  <div
    class="flex flex-col h-full bg-white/50 backdrop-blur-xl border-r border-slate-200/60 w-full md:w-72 flex-shrink-0 transition-all"
  >
    <div class="p-4 border-b border-slate-200/60 space-y-3">
      <!-- 標題和操作按鈕 -->
      <div class="flex items-center justify-between">
        <h2 class="font-semibold text-slate-800">
          發票 ({{ sessions.length }})
        </h2>
        <div class="flex items-center gap-2">
          <button
            @click="$emit('takePhoto')"
            class="p-2 rounded-full cursor-pointer hover:bg-white hover:shadow-sm text-green-600 transition-all"
            title="拍照上傳"
          >
            <Camera :size="20" />
          </button>
          <button
            @click="$emit('addMore')"
            class="p-2 rounded-full cursor-pointer hover:bg-white hover:shadow-sm text-blue-600 transition-all"
            title="上傳更多"
          >
            <Plus :size="20" />
          </button>
        </div>
      </div>

      <!-- 批量操作按鈕 -->
      <div class="grid grid-cols-2 gap-2">
        <button
          v-if="hasReviewableSessions"
          @click="$emit('saveAll')"
          :disabled="isSavingAll"
          class="flex-1 py-2.5 bg-blue-600 text-white rounded-lg font-medium text-sm cursor-pointer hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
        >
          <Loader2 v-if="isSavingAll" class="animate-spin" :size="16" />
          <span v-if="isSavingAll">保存中...</span>
          <span v-else>保存所有 ({{ reviewableCount }})</span>
        </button>

        <button
          v-if="sessions.length > 0"
          @click="$emit('deleteAll')"
          class="py-2.5 bg-red-600 text-white rounded-lg font-medium text-sm cursor-pointer hover:bg-red-700 transition-colors flex items-center justify-center gap-2"
          title="刪除所有發票"
        >
          <Trash2 :size="16" />
          <span>刪除所有</span>
        </button>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto p-3 space-y-2 no-scrollbar">
      <div
        v-for="session in sessions"
        :key="session.id"
        @click="$emit('select', session.id)"
        :class="[
          'relative flex items-center gap-3 p-3 rounded-xl cursor-pointer transition-all duration-200 group',
          activeId === session.id
            ? 'bg-white shadow-sm ring-1 ring-slate-200'
            : 'hover:bg-white/40 hover:shadow-sm text-slate-600',
        ]"
      >
        <!-- Thumbnail -->
        <div
          class="w-12 h-12 rounded-lg bg-slate-100 overflow-hidden flex-shrink-0 border border-slate-100 relative"
        >
          <img
            v-if="session.previewUrl"
            :src="session.previewUrl"
            alt="Thumbnail"
            class="w-full h-full object-cover"
          />
          <div
            v-else
            class="flex items-center justify-center h-full text-slate-300"
          >
            <FileText :size="20" />
          </div>

          <!-- Status Overlay on Thumbnail -->
          <div
            v-if="
              session.status === 'processing' || session.status === 'uploading'
            "
            class="absolute inset-0 bg-black/30 backdrop-blur-[2px] flex items-center justify-center rounded-lg"
          >
            <div class="relative">
              <Loader2
                :size="16"
                class="text-white animate-spin relative z-10"
              />
            </div>
          </div>
        </div>

        <!-- Info -->
        <div class="flex-1 min-w-0">
          <p
            :class="[
              'text-sm font-medium truncate',
              activeId === session.id ? 'text-slate-900' : 'text-slate-700',
            ]"
          >
            {{
              session.data?.seller ||
              (session.status === 'success' ? '已處理' : '新發票')
            }}
          </p>
          <p class="text-xs text-slate-500 truncate">
            {{
              session.status === 'processing'
                ? '分析中...'
                : session.status === 'uploading'
                ? '上傳中...'
                : session.status === 'error'
                ? '失敗'
                : session.status === 'success'
                ? '已儲存'
                : session.data?.date || '等待中...'
            }}
          </p>
        </div>

        <!-- Status Icon -->
        <div class="flex-shrink-0 flex items-center gap-2">
          <CheckCircle2
            v-if="session.status === 'success'"
            :size="16"
            class="text-green-500"
          />
          <AlertCircle
            v-if="session.status === 'error'"
            :size="16"
            class="text-red-500"
          />
          <!-- Delete Button (shown on hover) -->
          <button
            @click.stop="$emit('delete', session.id)"
            class="p-1.5 text-slate-400 hover:text-red-500 cursor-pointer hover:bg-red-50 rounded-lg transition-colors opacity-0 group-hover:opacity-100"
            title="刪除"
          >
            <Trash2 :size="16" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { InvoiceSession } from '../types/invoice'
import {
  Plus,
  Loader2,
  CheckCircle2,
  AlertCircle,
  FileText,
  Trash2,
  Camera,
} from 'lucide-vue-next'

interface SessionListProps {
  sessions: InvoiceSession[]
  activeId: string | null
  isSavingAll?: boolean
}

const props = withDefaults(defineProps<SessionListProps>(), {
  isSavingAll: false,
})

// 計算可保存的發票數量（有資料且狀態為 review 的）
const reviewableCount = computed(() => {
  return props.sessions.filter((s) => s.data && s.status === 'review').length
})

// 是否有可保存的發票
const hasReviewableSessions = computed(() => {
  return reviewableCount.value > 0
})

defineEmits<{
  select: [id: string]
  addMore: []
  takePhoto: []
  delete: [id: string]
  saveAll: []
  deleteAll: []
}>()
</script>

<style scoped>
.no-scrollbar::-webkit-scrollbar {
  display: none;
}

.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}

@keyframes pulse-ring {
  0% {
    transform: scale(0.8);
    opacity: 1;
  }
  50% {
    transform: scale(1.2);
    opacity: 0.5;
  }
  100% {
    transform: scale(1.5);
    opacity: 0;
  }
}

.pulse-ring {
  animation: pulse-ring 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}
</style>
