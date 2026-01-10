<template>
  <div class="invoice-card">
    <!-- 辨識結果表單 -->
    <div v-if="fileInfo.invoiceData" class="invoice-form-container">
      <!-- 頂部：標題、縮略圖、刪除按鈕 -->
      <div class="invoice-header">
        <div class="flex items-center gap-4 flex-1 min-w-0">
          <!-- 縮略圖 -->
          <div class="invoice-thumbnail">
            <el-image
              :src="fileInfo.preview"
              :alt="fileInfo.file.name"
              fit="cover"
              class="thumbnail-image"
              :preview-src-list="[fileInfo.preview]"
              :initial-index="0"
              hide-on-click-modal
            >
              <template #error>
                <div class="image-error">
                  <svg
                    class="w-6 h-6"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
                    />
                  </svg>
                </div>
              </template>
            </el-image>
            <div
              v-if="fileInfo.status === 'recognizing'"
              class="thumbnail-overlay"
            >
              <div class="spinner-small"></div>
            </div>
          </div>

          <!-- 文件信息 -->
          <div class="flex-1 min-w-0">
            <h3 class="invoice-title" style="margin-bottom: 0.25rem">
              發票資訊
            </h3>
            <div class="flex items-center gap-2 flex-wrap">
              <span class="file-name">{{ fileInfo.file.name }}</span>
              <div class="status-badge" :class="statusClass(fileInfo.status)">
                {{ statusText(fileInfo.status) }}
              </div>
            </div>
          </div>
        </div>

        <!-- 刪除按鈕 -->
        <button @click="$emit('remove')" class="delete-button">
          <svg
            class="w-5 h-5"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
            />
          </svg>
        </button>
      </div>

      <!-- 表單 -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-x-5 gap-y-4">
        <el-form-item
          label="發票代碼"
          :prop="`uploadFiles.${index}.invoiceData.invoiceCode`"
          :rules="[
            {
              required: true,
              message: '請輸入發票代碼',
              trigger: 'blur',
            },
          ]"
        >
          <el-input
            v-model="fileInfo.invoiceData.invoiceCode"
            placeholder="請輸入發票代碼"
          />
        </el-form-item>

        <el-form-item
          label="發票號碼"
          :prop="`uploadFiles.${index}.invoiceData.invoiceNumber`"
          :rules="[
            {
              required: true,
              message: '請輸入發票號碼',
              trigger: 'blur',
            },
            {
              pattern: /^\d{8}$/,
              message: '發票號碼應為 8 位數字',
              trigger: 'blur',
            },
          ]"
        >
          <el-input
            v-model="fileInfo.invoiceData.invoiceNumber"
            placeholder="請輸入發票號碼"
          />
        </el-form-item>

        <el-form-item
          label="開票日期"
          :prop="`uploadFiles.${index}.invoiceData.date`"
          :rules="[
            {
              required: true,
              message: '請選擇開票日期',
              trigger: 'change',
            },
          ]"
        >
          <el-input v-model="fileInfo.invoiceData.date" type="date" />
        </el-form-item>

        <el-form-item
          label="金額"
          :prop="`uploadFiles.${index}.invoiceData.amount`"
          :rules="[
            { required: true, message: '請輸入金額', trigger: 'blur' },
            {
              pattern: /^\d+(\.\d{1,2})?$/,
              message: '請輸入有效的金額',
              trigger: 'blur',
            },
          ]"
        >
          <el-input v-model="fileInfo.invoiceData.amount" placeholder="0.00">
            <template #suffix>元</template>
          </el-input>
        </el-form-item>

        <el-form-item
          label="稅額"
          :prop="`uploadFiles.${index}.invoiceData.taxAmount`"
        >
          <el-input v-model="fileInfo.invoiceData.taxAmount" placeholder="0.00">
            <template #suffix>元</template>
          </el-input>
        </el-form-item>

        <el-form-item
          label="價稅合計"
          :prop="`uploadFiles.${index}.invoiceData.totalAmount`"
          :rules="[
            {
              required: true,
              message: '請輸入價稅合計',
              trigger: 'blur',
            },
            {
              pattern: /^\d+(\.\d{1,2})?$/,
              message: '請輸入有效的金額',
              trigger: 'blur',
            },
          ]"
        >
          <el-input
            v-model="fileInfo.invoiceData.totalAmount"
            placeholder="0.00"
          >
            <template #suffix>元</template>
          </el-input>
        </el-form-item>

        <el-form-item
          label="銷售方名稱"
          :prop="`uploadFiles.${index}.invoiceData.seller`"
          :rules="[
            {
              required: true,
              message: '請輸入銷售方名稱',
              trigger: 'blur',
            },
          ]"
          class="md:col-span-2"
        >
          <el-input
            v-model="fileInfo.invoiceData.seller"
            placeholder="請輸入銷售方名稱"
          />
        </el-form-item>

        <el-form-item
          label="銷售方納稅人識別號"
          :prop="`uploadFiles.${index}.invoiceData.sellerTaxId`"
          class="md:col-span-2"
        >
          <el-input
            v-model="fileInfo.invoiceData.sellerTaxId"
            placeholder="請輸入銷售方納稅人識別號"
          />
        </el-form-item>

        <el-form-item
          label="購買方名稱"
          :prop="`uploadFiles.${index}.invoiceData.buyer`"
          class="md:col-span-2"
        >
          <el-input
            v-model="fileInfo.invoiceData.buyer"
            placeholder="請輸入購買方名稱"
          />
        </el-form-item>

        <el-form-item
          label="購買方納稅人識別號"
          :prop="`uploadFiles.${index}.invoiceData.buyerTaxId`"
          class="md:col-span-2"
        >
          <el-input
            v-model="fileInfo.invoiceData.buyerTaxId"
            placeholder="請輸入購買方納稅人識別號"
          />
        </el-form-item>

        <el-form-item
          label="備註"
          :prop="`uploadFiles.${index}.invoiceData.remarks`"
          class="md:col-span-2"
        >
          <el-input
            v-model="fileInfo.invoiceData.remarks"
            type="textarea"
            :rows="3"
            placeholder="請輸入備註"
          />
        </el-form-item>
      </div>
    </div>

    <!-- 辨識中或失敗狀態 -->
    <div v-else class="flex items-center justify-center py-20">
      <div class="text-center">
        <div
          v-if="fileInfo.status === 'recognizing'"
          class="spinner-large mb-6"
        ></div>
        <div v-else-if="fileInfo.status === 'error'" class="error-icon mb-6">
          <svg
            class="w-16 h-16"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div class="text-lg text-gray-600 mb-4">
          {{ fileInfo.status === 'recognizing' ? '正在辨識中...' : '辨識失敗' }}
        </div>
        <button
          v-if="fileInfo.status === 'error'"
          @click="$emit('retry')"
          class="retry-button"
        >
          重新辨識
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { UploadFileInfo } from '../types/invoice'

defineProps<{
  fileInfo: UploadFileInfo
  index: number
}>()

defineEmits<{
  remove: []
  retry: []
}>()

const statusClass = (status: string) => {
  switch (status) {
    case 'success':
      return 'status-success'
    case 'recognizing':
      return 'status-processing'
    case 'error':
      return 'status-error'
    default:
      return ''
  }
}

const statusText = (status: string) => {
  switch (status) {
    case 'success':
      return '完成'
    case 'recognizing':
      return '處理中'
    case 'error':
      return '失敗'
    default:
      return ''
  }
}
</script>

<style scoped>
/* 發票卡片 */
.invoice-card {
  background: var(--color-u-bg-primary);
  border-radius: var(--radius-u-lg);
  padding: var(--spacing-u-2xl);
  box-shadow: var(--shadow-u-sm);
  transition: all 0.2s ease;
  border: 1px solid var(--color-u-border);
}

.invoice-card:hover {
  box-shadow: var(--shadow-u-md);
}

/* 狀態徽章 */
.status-badge {
  padding: 0.1875rem 0.625rem;
  border-radius: var(--radius-u-md);
  font-size: var(--font-size-u-xs);
  font-weight: 600;
  white-space: nowrap;
  flex-shrink: 0;
}

.status-success {
  background: var(--color-u-success-light);
  color: var(--color-u-success);
}

.status-processing {
  background: var(--color-u-primary-light);
  color: var(--color-u-primary);
}

.status-error {
  background: var(--color-u-error-light);
  color: var(--color-u-error);
}

/* 發票表單容器 */
.invoice-form-container {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-u-lg);
  width: 100%;
  min-width: 0;
}

/* 發票頭部 */
.invoice-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-u-md);
  padding-bottom: var(--spacing-u-lg);
  border-bottom: 1px solid var(--color-u-border);
}

.image-error {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-u-text-secondary);
}

.invoice-title {
  font-size: var(--font-size-u-lg);
  font-weight: 600;
  color: var(--color-u-text-primary);
  letter-spacing: -0.02em;
  margin: 0;
  line-height: 1.4;
}

.file-name {
  font-size: var(--font-size-u-sm);
  color: var(--color-u-text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 300px;
}

.delete-button {
  padding: var(--spacing-u-sm);
  background: var(--color-u-error-light);
  color: var(--color-u-error);
  border: none;
  border-radius: var(--radius-u-sm);
  cursor: pointer;
  transition: all 0.15s ease;
}

.delete-button:hover {
  background: rgba(255, 59, 48, 0.15);
}

.delete-button:active {
  transform: scale(0.95);
}

.retry-button {
  padding: 0.625rem var(--spacing-u-2xl);
  background: var(--color-u-primary);
  color: white;
  border: none;
  border-radius: var(--radius-u-md);
  font-size: var(--font-size-u-base);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
}

.retry-button:hover {
  background: var(--color-u-primary-hover);
}

.retry-button:active {
  transform: scale(0.98);
}

/* 表單網格佈局 */
.grid {
  width: 100%;
  display: grid;
}

.grid > :deep(.el-form-item) {
  min-width: 0;
  width: 100%;
}

/* 確保 md:col-span-2 的樣式正常工作 */
@media (min-width: 768px) {
  .grid .md\:col-span-2 {
    grid-column: span 2 / span 2;
  }
}

/* 載入動畫 */
.spinner-large {
  width: 60px;
  height: 60px;
  border: 4px solid #e5e7eb;
  border-top-color: #3b82f6;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto;
}

.spinner-small {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

.error-icon {
  display: inline-flex;
  color: #dc2626;
  margin: 0 auto;
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.invoice-thumbnail {
  width: 80px;
  height: 80px;
  border-radius: var(--radius-u-md);
  overflow: hidden;
  flex-shrink: 0;
  position: relative;
  border: 1px solid var(--color-u-border);
}

.thumbnail-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.thumbnail-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-u-md);
}

/* Element Plus 表單樣式覆蓋 */
:deep(.el-form) {
  width: 100%;
}

:deep(.el-form-item) {
  margin-bottom: 0;
  display: flex;
  flex-direction: column;
  width: 100%;
}

:deep(.el-form-item__label) {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-u-text-primary);
  letter-spacing: -0.01em;
  padding: 0;
  margin-bottom: var(--spacing-u-sm);
  line-height: 1.5;
  height: auto;
  display: flex;
  align-items: center;
  justify-content: flex-start;
}

:deep(
    .el-form-item.is-required:not(.is-no-asterisk)
      > .el-form-item__label::before
  ) {
  content: '*';
  color: var(--color-u-error);
  margin-right: var(--spacing-u-xs);
  font-size: 0.875rem;
  line-height: 1;
}

:deep(
    .el-form-item.is-required:not(.is-no-asterisk) > .el-form-item__label::after
  ) {
  display: none;
}

:deep(.el-form-item__content) {
  line-height: normal;
  display: flex;
  flex-direction: column;
}

:deep(.el-form-item__error) {
  font-size: var(--font-size-u-sm);
  color: var(--color-u-error);
  padding-top: 0.375rem;
  line-height: 1.4;
  margin: 0;
}

:deep(.el-input) {
  width: 100%;
}

:deep(.el-input__wrapper) {
  padding: var(--spacing-u-md) 0.875rem;
  border: 1px solid var(--color-u-border-hover);
  border-radius: var(--radius-u-md);
  box-shadow: none;
  background: var(--color-u-bg-primary);
  transition: all 0.15s ease;
  width: 100%;
}

:deep(.el-input__wrapper:hover) {
  border-color: var(--color-u-border-hover);
  box-shadow: none;
}

:deep(.el-input__wrapper.is-focus) {
  border-color: var(--color-u-primary);
  box-shadow: 0 0 0 4px var(--color-u-primary-light);
}

:deep(.el-input.is-disabled .el-input__wrapper) {
  background: var(--color-u-bg-secondary);
}

:deep(.el-input__inner) {
  font-size: var(--font-size-u-base);
  color: var(--color-u-text-primary);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

:deep(.el-input__inner::placeholder) {
  color: var(--color-u-text-secondary);
  font-weight: 400;
}

:deep(.el-textarea) {
  width: 100%;
}

:deep(.el-textarea__inner) {
  padding: var(--spacing-u-md) 0.875rem;
  border: 1px solid var(--color-u-border-hover);
  border-radius: var(--radius-u-md);
  font-size: var(--font-size-u-base);
  color: var(--color-u-text-primary);
  background: var(--color-u-bg-primary);
  transition: all 0.15s ease;
  box-shadow: none;
  width: 100%;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

:deep(.el-textarea__inner:hover) {
  border-color: var(--color-u-border-hover);
}

:deep(.el-textarea__inner:focus) {
  border-color: var(--color-u-primary);
  box-shadow: 0 0 0 4px var(--color-u-primary-light);
  outline: none;
}

:deep(.el-textarea__inner::placeholder) {
  color: var(--color-u-text-secondary);
}

:deep(.el-input__suffix) {
  color: var(--color-u-text-secondary);
  font-size: 0.875rem;
  font-weight: 500;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .invoice-card {
    padding: 1.125rem;
  }

  .invoice-form-container {
    gap: 0.875rem;
  }

  .invoice-header {
    padding-bottom: var(--spacing-u-md);
  }

  .invoice-thumbnail {
    width: 70px;
    height: 70px;
  }

  .grid {
    gap: 1rem !important;
  }
}

@media (max-width: 640px) {
  .invoice-card {
    padding: 0.875rem;
  }

  .invoice-form-container {
    gap: 0.625rem;
  }

  .invoice-header {
    flex-wrap: wrap;
    padding-bottom: 0.625rem;
    gap: 0.5rem;
  }

  .invoice-thumbnail {
    width: 60px;
    height: 60px;
  }

  .invoice-title {
    font-size: var(--font-size-u-base);
  }

  .file-name {
    font-size: var(--font-size-u-xs);
    max-width: 180px;
  }

  .grid {
    gap: 0.75rem !important;
  }

  :deep(.el-form-item__label) {
    font-size: var(--font-size-u-sm);
    margin-bottom: 0.375rem;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    padding: 0.625rem 0.75rem;
    font-size: 0.875rem;
  }
}

@media (max-width: 640px) {
  .invoice-card {
    padding: 0.875rem;
  }

  .invoice-form-container {
    gap: 0.625rem;
  }

  .invoice-header {
    flex-wrap: wrap;
    padding-bottom: 0.625rem;
    gap: 0.5rem;
  }

  .invoice-thumbnail {
    width: 60px;
    height: 60px;
  }

  .invoice-title {
    font-size: var(--font-size-u-base);
  }

  .file-name {
    font-size: var(--font-size-u-xs);
    max-width: 180px;
  }

  .grid {
    gap: 0.75rem !important;
  }
}
</style>
