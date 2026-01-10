<template>
  <div class="min-h-screen bg-gray-50 py-16 px-4">
    <div class="max-w-5xl mx-auto">
      <!-- 標題區域 -->
      <div class="text-center mb-16">
        <h1 class="text-5xl font-semibold text-gray-900 mb-4 tracking-tight">
          發票辨識
        </h1>
        <p class="text-lg text-gray-500">使用 AI 快速辨識發票資訊</p>
      </div>

      <!-- 上傳區域 -->
      <UploadArea @file-change="handleFileChange" />

      <!-- 已上傳檔案列表 -->
      <InvoiceList
        v-if="uploadFiles.length > 0"
        ref="invoiceListRef"
        :upload-files="uploadFiles"
        @remove="removeFile"
        @retry="retryRecognize"
      />

      <!-- 底部操作按鈕 -->
      <InvoiceActions
        :upload-files="uploadFiles"
        :saving="saving"
        @clear="clearAll"
        @save="handleSave"
      />

      <!-- 統計資訊 -->
      <InvoiceStats :upload-files="uploadFiles" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { UploadFile } from 'element-plus'
import type { UploadFileInfo } from '../types/invoice'
import { recognizeInvoice, saveInvoices } from '../api/invoice'
import UploadArea from './UploadArea.vue'
import InvoiceList from './InvoiceList.vue'
import InvoiceStats from './InvoiceStats.vue'
import InvoiceActions from './InvoiceActions.vue'

const uploadFiles = ref<UploadFileInfo[]>([])
const saving = ref(false)
const invoiceListRef = ref<InstanceType<typeof InvoiceList>>()

// 檔案轉 base64
const fileToBase64 = (file: File): Promise<string> => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.readAsDataURL(file)
    reader.onload = () => resolve(reader.result as string)
    reader.onerror = (error) => reject(error)
  })
}

// 處理檔案變化
const handleFileChange = async (uploadFile: UploadFile) => {
  if (!uploadFile.raw) return

  try {
    const base64 = await fileToBase64(uploadFile.raw)
    const preview = URL.createObjectURL(uploadFile.raw)

    const fileInfo: UploadFileInfo = {
      id: `${Date.now()}_${Math.random()}`,
      file: uploadFile.raw,
      base64,
      preview,
      status: 'recognizing',
    }

    uploadFiles.value.push(fileInfo)

    // 開始辨識
    recognizeFile(fileInfo)
  } catch (error) {
    ElMessage.error('檔案讀取失敗')
  }
}

// 辨識檔案
const recognizeFile = async (fileInfo: UploadFileInfo) => {
  try {
    const index = uploadFiles.value.findIndex((f) => f.id === fileInfo.id)
    if (index === -1) return

    uploadFiles.value[index].status = 'recognizing'

    console.log('開始辨識...', fileInfo.id)
    const response = await recognizeInvoice(fileInfo.base64)
    console.log('辨識回應:', response)

    if (response.success && response.data) {
      uploadFiles.value[index].status = 'success'
      uploadFiles.value[index].invoiceData = response.data
      ElMessage.success('辨識成功')
    } else {
      uploadFiles.value[index].status = 'error'
      ElMessage.error(response.message || '辨識失敗')
    }
  } catch (error) {
    console.error('辨識錯誤:', error)
    const index = uploadFiles.value.findIndex((f) => f.id === fileInfo.id)
    if (index !== -1) {
      uploadFiles.value[index].status = 'error'
    }
    ElMessage.error('辨識失敗，請重試')
  }
}

// 重新辨識
const retryRecognize = (fileInfo: UploadFileInfo) => {
  recognizeFile(fileInfo)
}

// 刪除檔案
const removeFile = (id: string) => {
  const index = uploadFiles.value.findIndex((f) => f.id === id)
  if (index > -1) {
    URL.revokeObjectURL(uploadFiles.value[index].preview)
    uploadFiles.value.splice(index, 1)
    // 清除驗證狀態
    invoiceListRef.value?.clearValidate()
  }
}

// 清空全部
const clearAll = () => {
  uploadFiles.value.forEach((f) => URL.revokeObjectURL(f.preview))
  uploadFiles.value = []
  invoiceListRef.value?.resetFields()
  ElMessage.info('已清空')
}

// 儲存
const handleSave = async () => {
  const successFiles = uploadFiles.value.filter(
    (f) => f.status === 'success' && f.invoiceData
  )

  if (successFiles.length === 0) {
    ElMessage.warning('沒有可儲存的發票')
    return
  }

  // 驗證表單
  if (!invoiceListRef.value) {
    ElMessage.error('表單未初始化')
    return
  }

  try {
    await invoiceListRef.value.validate()
  } catch (error) {
    ElMessage.error('請檢查並填寫完整的發票資訊')
    return
  }

  try {
    saving.value = true
    const invoices = successFiles.map((f) => f.invoiceData!)
    const response = await saveInvoices(invoices)

    if (response.success) {
      ElMessage.success(response.message || '儲存成功')
      // 儲存成功後清空列表
      clearAll()
    } else {
      ElMessage.error(response.message || '儲存失敗')
    }
  } catch (error) {
    ElMessage.error('儲存失敗，請重試')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
/* 全局字體設置 */
* {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto,
    'Helvetica Neue', Arial, sans-serif;
}

/* 響應式調整 */
@media (max-width: 768px) {
  h1 {
    font-size: 2.5rem;
  }
}

@media (max-width: 640px) {
  h1 {
    font-size: 2rem;
  }
}
</style>
