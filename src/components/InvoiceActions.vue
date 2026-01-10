<template>
  <div
    v-if="uploadFiles.length > 0"
    class="mt-8 flex flex-col sm:flex-row justify-center items-center gap-4"
  >
    <button @click="$emit('clear')" class="secondary-button w-full sm:w-auto">
      清空全部
    </button>
    <button
      @click="$emit('save')"
      :disabled="!hasSuccessFiles || saving"
      class="primary-button w-full sm:w-auto"
    >
      <span v-if="saving" class="flex items-center justify-center">
        <div class="spinner-small mr-2"></div>
        儲存中...
      </span>
      <span v-else>儲存全部發票 ({{ successCount }})</span>
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { UploadFileInfo } from '../types/invoice'

const props = defineProps<{
  uploadFiles: UploadFileInfo[]
  saving: boolean
}>()

defineEmits<{
  clear: []
  save: []
}>()

const successCount = computed(
  () => props.uploadFiles.filter((f) => f.status === 'success').length
)

const hasSuccessFiles = computed(() => successCount.value > 0)
</script>

<style scoped>
/* 按鈕 */
.primary-button {
  padding: var(--spacing-u-md) 2rem;
  background: var(--color-u-primary);
  color: white;
  border: none;
  border-radius: var(--radius-u-md);
  font-size: var(--font-size-u-base);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  min-width: 160px;
}

.primary-button:hover:not(:disabled) {
  background: var(--color-u-primary-hover);
}

.primary-button:active:not(:disabled) {
  transform: scale(0.98);
}

.primary-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.secondary-button {
  padding: var(--spacing-u-md) 2rem;
  background: rgba(0, 0, 0, 0.03);
  color: var(--color-u-text-primary);
  border: none;
  border-radius: var(--radius-u-md);
  font-size: var(--font-size-u-base);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  min-width: 120px;
}

.secondary-button:hover {
  background: rgba(0, 0, 0, 0.06);
}

.secondary-button:active {
  transform: scale(0.98);
}

.spinner-small {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

/* 響應式調整 */
@media (max-width: 768px) {
  .primary-button,
  .secondary-button {
    min-width: unset;
    width: 100%;
  }
}
</style>
