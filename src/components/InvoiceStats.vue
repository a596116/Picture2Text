<template>
  <div v-if="uploadFiles.length > 0" class="mt-6">
    <div class="stats-container">
      <div class="stat-item">
        <div class="stat-value">{{ uploadFiles.length }}</div>
        <div class="stat-label">總計</div>
      </div>
      <div class="stat-divider"></div>
      <div class="stat-item stat-success">
        <div class="stat-value">{{ successCount }}</div>
        <div class="stat-label">成功</div>
      </div>
      <div class="stat-divider"></div>
      <div class="stat-item stat-processing">
        <div class="stat-value">{{ recognizingCount }}</div>
        <div class="stat-label">處理中</div>
      </div>
      <div class="stat-divider"></div>
      <div class="stat-item stat-error">
        <div class="stat-value">{{ errorCount }}</div>
        <div class="stat-label">失敗</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { UploadFileInfo } from '../types/invoice'

const props = defineProps<{
  uploadFiles: UploadFileInfo[]
}>()

const successCount = computed(
  () => props.uploadFiles.filter((f) => f.status === 'success').length
)

const recognizingCount = computed(
  () => props.uploadFiles.filter((f) => f.status === 'recognizing').length
)

const errorCount = computed(
  () => props.uploadFiles.filter((f) => f.status === 'error').length
)
</script>

<style scoped>
/* 統計容器 */
.stats-container {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 2.5rem;
  background: var(--color-u-bg-primary);
  border-radius: var(--radius-u-lg);
  padding: var(--spacing-u-2xl) 2.5rem;
  box-shadow: var(--shadow-u-sm);
  border: 1px solid var(--color-u-border);
  transition: all 0.2s ease;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-u-xs);
}

.stat-value {
  font-size: 1.75rem;
  font-weight: 600;
  color: var(--color-u-text-primary);
  letter-spacing: -0.02em;
}

.stat-label {
  font-size: var(--font-size-u-sm);
  color: var(--color-u-text-secondary);
  font-weight: 500;
}

.stat-success .stat-value {
  color: var(--color-u-success);
}

.stat-processing .stat-value {
  color: var(--color-u-primary);
}

.stat-error .stat-value {
  color: var(--color-u-error);
}

.stat-divider {
  width: 1px;
  height: 40px;
  background: var(--color-u-border);
}

/* 響應式調整 */
@media (max-width: 768px) {
  .stats-container {
    gap: 1.5rem;
    padding: 1.25rem 1.75rem;
  }

  .stat-value {
    font-size: 1.5rem;
  }
}

@media (max-width: 640px) {
  .stats-container {
    gap: 1.25rem;
    padding: 1rem 1.25rem;
  }

  .stat-value {
    font-size: 1.375rem;
  }

  .stat-divider {
    height: 32px;
  }
}
</style>
