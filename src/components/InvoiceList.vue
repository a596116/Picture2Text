<template>
  <el-form
    v-if="uploadFiles.length > 0"
    ref="formRef"
    :model="{ uploadFiles }"
    class="invoice-list"
  >
    <InvoiceCard
      v-for="(fileInfo, index) in uploadFiles"
      :key="fileInfo.id"
      :file-info="fileInfo"
      :index="index"
      @remove="$emit('remove', fileInfo.id)"
      @retry="$emit('retry', fileInfo)"
    />
  </el-form>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { FormInstance } from 'element-plus'
import type { UploadFileInfo } from '../types/invoice'
import InvoiceCard from './InvoiceCard.vue'

defineProps<{
  uploadFiles: UploadFileInfo[]
}>()

defineEmits<{
  remove: [id: string]
  retry: [fileInfo: UploadFileInfo]
}>()

const formRef = ref<FormInstance>()

const validate = async () => {
  if (!formRef.value) {
    return Promise.reject(new Error('表單未初始化'))
  }
  return formRef.value.validate()
}

const clearValidate = () => {
  formRef.value?.clearValidate()
}

const resetFields = () => {
  formRef.value?.resetFields()
}

defineExpose({
  formRef,
  validate,
  clearValidate,
  resetFields,
})
</script>

<style scoped>
/* 發票列表 */
.invoice-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .invoice-list {
    gap: 0.75rem;
  }
}

@media (max-width: 640px) {
  .invoice-list {
    gap: 0.625rem;
  }
}
</style>
