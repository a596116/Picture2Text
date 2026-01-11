<template>
  <!-- 初始載入畫面：首次上傳檔案時顯示 -->
  <InitialLoading v-if="initialLoading" />

  <!-- 空狀態：沒有任何發票時顯示的上傳界面 -->
  <EmptyState
    v-else
    @file-change="handleFileUpload"
  />
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useInvoiceStore } from '../stores/invoice'
import InitialLoading from '../components/InitialLoading.vue'
import EmptyState from '../components/EmptyState.vue'

const router = useRouter()
const invoiceStore = useInvoiceStore()

/** 初始載入狀態標誌（首次上傳時顯示載入動畫） */
const initialLoading = ref(false)

/**
 * 處理檔案上傳（從 input 元素）
 * @param event - 檔案選擇事件
 */
const handleFileUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const files = Array.from(target.files || [])

  if (files.length === 0) return

  // 如果是首次上傳（空狀態），顯示載入動畫
  const isFirstUpload = invoiceStore.sessions.length === 0
  if (isFirstUpload) {
    initialLoading.value = true
    // 等待顯示載入動畫（模擬檔案準備過程）
    await new Promise((resolve) => setTimeout(resolve, 1800))
  }

  // 處理檔案
  await invoiceStore.processFiles(files)

  // 如果首次上傳，隱藏載入畫面
  if (isFirstUpload) {
    // 短暫延遲確保平滑過渡
    await new Promise((resolve) => setTimeout(resolve, 200))
    initialLoading.value = false
  }

  // 清空輸入框
  if (target.value) target.value = ''

  // 導航到工作區
  if (invoiceStore.sessions.length > 0) {
    router.push('/workspace')
  }
}

// 如果有發票，導航到工作區
watch(
  () => invoiceStore.sessions.length,
  (length) => {
    if (length > 0) {
      router.push('/workspace')
    }
  }
)

// 初始化檢查
onMounted(() => {
  if (invoiceStore.sessions.length > 0) {
    router.push('/workspace')
  }
})
</script>
