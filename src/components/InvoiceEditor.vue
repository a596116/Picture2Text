<template>
  <!-- 發票編輯器：右側的編輯區域 -->
  <div
    class="w-full md:w-1/2 p-4 md:p-8 overflow-y-auto bg-white/40 backdrop-blur-xl"
  >
    <div class="max-w-xl mx-auto space-y-6">
      <!-- 標題欄和操作按鈕 -->
      <div
        class="flex items-center justify-between pb-4 border-b border-slate-200/60"
      >
        <h2 class="text-xl font-bold text-slate-900">
          {{ session.data?.seller || '發票詳情' }}
        </h2>
        <div class="flex items-center gap-2">
          <!-- 刪除按鈕 -->
          <button
            @click="$emit('delete')"
            class="p-2.5 rounded-full cursor-pointer hover:bg-red-50 text-red-600 transition-all shadow-sm hover:shadow-md"
            title="刪除此發票"
          >
            <Trash2 :size="20" />
          </button>
        </div>
      </div>

      <!-- 錯誤訊息提示 -->
      <div
        v-if="session.status === 'error'"
        class="p-4 bg-red-50 border border-red-100 rounded-xl flex items-center gap-3 text-red-600"
      >
        <AlertCircle :size="20" />
        <p class="text-sm font-medium">
          {{ session.errorMessage || '處理圖片時發生錯誤' }}
        </p>
      </div>

      <!-- 成功訊息提示 -->
      <div
        v-if="session.status === 'success'"
        class="p-4 bg-green-50 border border-green-100 rounded-xl flex items-center gap-3 text-green-700"
      >
        <CheckCircle2 :size="20" />
        <p class="text-sm font-medium">發票已成功儲存</p>
      </div>

      <!-- 表單編輯區域 -->
      <div
        v-if="session.data"
        :class="{
          'opacity-50 pointer-events-none': session.status === 'success',
        }"
      >
        <!-- 發票表單 -->
        <InvoiceForm
          ref="formRef"
          :data="session.data"
          @change="$emit('change', $event)"
        />

        <!-- 儲存按鈕 -->
        <div class="pt-8">
          <Button
            :full-width="true"
            :disabled="
              session.status === 'saving' || session.status === 'success'
            "
            @click="handleSave"
          >
            <span
              v-if="session.status === 'saving'"
              class="flex items-center gap-2"
            >
              <Loader2 class="animate-spin" :size="16" /> 儲存中...
            </span>
            <span
              v-else-if="session.status === 'success'"
              class="flex items-center gap-2"
            >
              <CheckCircle2 :size="16" /> 已儲存
            </span>
            <span v-else>儲存發票</span>
          </Button>
        </div>
      </div>

      <!-- 無法自動提取資料時的提示 -->
      <div
        v-else-if="
          session.status !== 'processing' &&
          session.status !== 'uploading' &&
          session.status !== 'error'
        "
        class="text-center py-12 text-slate-400"
      >
        <p>無法自動提取資料</p>
        <Button variant="secondary" class="mt-4" @click="$emit('manualInput')">
          手動輸入
        </Button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Trash2, AlertCircle, CheckCircle2, Loader2 } from 'lucide-vue-next'
import type { InvoiceSession, InvoiceData } from '../types/invoice'
import InvoiceForm from './InvoiceForm.vue'
import Button from './Button.vue'

// 組件屬性：接收當前的發票 session
defineProps<{
  session: InvoiceSession
}>()

// 定義事件
const emit = defineEmits<{
  // 資料變更事件
  change: [data: InvoiceData]
  // 儲存事件
  save: []
  // 刪除事件
  delete: []
  // 手動輸入事件
  manualInput: []
}>()

// 表單引用
const formRef = ref<InstanceType<typeof InvoiceForm> | null>(null)

// 處理保存，先驗證表單
const handleSave = async () => {
  if (!formRef.value) {
    emit('save')
    return
  }

  try {
    // 驗證表單
    await formRef.value.validate()
    // 驗證通過，發出保存事件
    emit('save')
  } catch (error) {
    // 驗證失敗，表單會自動在欄位上顯示錯誤訊息，不需要額外提示
  }
}
</script>
