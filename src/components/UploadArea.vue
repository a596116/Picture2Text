<template>
  <div class="upload-card mb-8">
    <el-upload
      ref="uploadRef"
      :auto-upload="false"
      :on-change="handleFileChange"
      :show-file-list="false"
      accept="image/*"
      multiple
      drag
      class="modern-upload"
    >
      <div class="upload-content">
        <div class="upload-icon-wrapper">
          <svg
            class="upload-icon"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="1.5"
              d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
            />
          </svg>
        </div>
        <div class="upload-text-primary">拖曳圖片至此，或點擊上傳</div>
        <div class="upload-text-secondary">
          支援 JPG、PNG 格式，可批次上傳
        </div>
      </div>
    </el-upload>
  </div>
</template>

<script setup lang="ts">
import type { UploadFile } from 'element-plus'

defineProps<{
  onFileChange: (file: UploadFile) => void
}>()

const emit = defineEmits<{
  fileChange: [file: UploadFile]
}>()

const handleFileChange = (file: UploadFile) => {
  emit('fileChange', file)
}
</script>

<style scoped>
/* 上傳卡片 */
.upload-card {
  background: var(--color-u-bg-primary);
  border-radius: var(--radius-u-lg);
  padding: 0;
  box-shadow: var(--shadow-u-sm);
  overflow: hidden;
  transition: all 0.2s ease;
  border: 1px solid var(--color-u-border);
}

.upload-card:hover {
  box-shadow: var(--shadow-u-md);
}

/* Element Plus Upload 客製化 */
:deep(.modern-upload) {
  width: 100%;
}

:deep(.el-upload-dragger) {
  padding: 5rem 2.5rem;
  border: 2px dashed var(--color-u-border-hover);
  border-radius: var(--radius-u-lg);
  background: rgba(0, 0, 0, 0.02);
  cursor: pointer;
  transition: all 0.2s ease;
  width: 100%;
}

:deep(.el-upload-dragger:hover) {
  border-color: var(--color-u-border-hover);
  background: rgba(0, 0, 0, 0.03);
}

/* 上傳區域內容 */
.upload-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.upload-icon-wrapper {
  width: 80px;
  height: 80px;
  border-radius: var(--radius-u-lg);
  background: var(--color-u-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--spacing-u-sm);
  transition: all 0.2s ease;
}

:deep(.el-upload-dragger:hover) .upload-icon-wrapper {
  transform: scale(1.05);
}

.upload-icon {
  width: 40px;
  height: 40px;
  color: white;
}

.upload-text-primary {
  font-size: var(--font-size-u-lg);
  font-weight: 500;
  color: var(--color-u-text-primary);
  letter-spacing: -0.01em;
}

.upload-text-secondary {
  font-size: 0.875rem;
  color: var(--color-u-text-secondary);
}

/* 響應式調整 */
@media (max-width: 768px) {
  :deep(.el-upload-dragger) {
    padding: 3.75rem 1.875rem;
  }
}

@media (max-width: 640px) {
  :deep(.el-upload-dragger) {
    padding: 3.125rem 1.5rem;
  }

  .upload-icon-wrapper {
    width: 70px;
    height: 70px;
  }

  .upload-icon {
    width: 36px;
    height: 36px;
  }

  .upload-text-primary {
    font-size: 1rem;
  }

  .upload-text-secondary {
    font-size: 0.8125rem;
  }
}
</style>
