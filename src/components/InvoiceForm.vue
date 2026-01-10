<template>
  <el-form
    ref="formRef"
    :model="formData"
    :rules="rules"
    label-position="top"
    class="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500 custom-form"
  >
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <!-- 發票代碼 -->
      <el-form-item
        label="發票代碼"
        prop="invoiceCode"
        class="custom-form-item"
      >
        <el-input
          v-model="formData.invoiceCode"
          placeholder="請輸入發票代碼"
          class="custom-input"
          @input="handleChange('invoiceCode', formData.invoiceCode)"
        />
      </el-form-item>

      <!-- 發票號碼 -->
      <el-form-item
        label="發票號碼"
        prop="invoiceNumber"
        class="custom-form-item"
      >
        <el-input
          v-model="formData.invoiceNumber"
          placeholder="請輸入發票號碼"
          class="custom-input"
          @input="handleChange('invoiceNumber', formData.invoiceNumber)"
        />
      </el-form-item>

      <!-- 開票日期 -->
      <el-form-item label="開票日期" prop="date" class="custom-form-item">
        <el-date-picker
          v-model="formData.date"
          type="date"
          placeholder="請選擇開票日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          class="custom-input w-full"
          @change="handleChange('date', formData.date || '')"
        />
      </el-form-item>

      <!-- 價稅合計 -->
      <el-form-item
        label="價稅合計"
        prop="totalAmount"
        class="custom-form-item"
      >
        <div class="relative w-full">
          <el-input
            v-model="formData.totalAmount"
            type="number"
            placeholder="0.00"
            class="custom-input total-amount-input"
            @input="handleChange('totalAmount', formData.totalAmount)"
            @blur="handleChange('totalAmount', formData.totalAmount)"
          >
            <template #prefix>
              <span class="text-slate-400 font-medium z-10">$</span>
            </template>
          </el-input>
        </div>
      </el-form-item>

      <!-- 金額 -->
      <el-form-item label="金額" prop="amount" class="custom-form-item">
        <div class="relative w-full">
          <el-input
            v-model="formData.amount"
            type="number"
            placeholder="0.00"
            class="custom-input"
            @input="handleChange('amount', formData.amount)"
            @blur="handleChange('amount', formData.amount)"
          >
            <template #prefix>
              <span class="text-slate-400 font-medium z-10">$</span>
            </template>
          </el-input>
        </div>
      </el-form-item>

      <!-- 稅額 -->
      <el-form-item label="稅額" prop="taxAmount" class="custom-form-item">
        <div class="relative w-full">
          <el-input
            v-model="formData.taxAmount"
            type="number"
            placeholder="0.00"
            class="custom-input"
            @input="handleChange('taxAmount', formData.taxAmount)"
            @blur="handleChange('taxAmount', formData.taxAmount)"
          >
            <template #prefix>
              <span class="text-slate-400 font-medium z-10">$</span>
            </template>
          </el-input>
        </div>
      </el-form-item>

      <!-- 銷售方名稱 -->
      <el-form-item
        label="銷售方名稱"
        prop="seller"
        class="custom-form-item md:col-span-2"
      >
        <el-input
          v-model="formData.seller"
          placeholder="請輸入銷售方名稱"
          class="custom-input"
          @input="handleChange('seller', formData.seller)"
        />
      </el-form-item>

      <!-- 銷售方納稅人識別號 -->
      <el-form-item
        label="銷售方納稅人識別號"
        prop="sellerTaxId"
        class="custom-form-item md:col-span-2"
      >
        <el-input
          v-model="formData.sellerTaxId"
          placeholder="請輸入銷售方納稅人識別號"
          class="custom-input"
          @input="handleChange('sellerTaxId', formData.sellerTaxId)"
        />
      </el-form-item>

      <!-- 購買方名稱 -->
      <el-form-item
        label="購買方名稱"
        prop="buyer"
        class="custom-form-item md:col-span-2"
      >
        <el-input
          v-model="formData.buyer"
          placeholder="請輸入購買方名稱"
          class="custom-input"
          @input="handleChange('buyer', formData.buyer)"
        />
      </el-form-item>

      <!-- 購買方納稅人識別號 -->
      <el-form-item
        label="購買方納稅人識別號"
        prop="buyerTaxId"
        class="custom-form-item md:col-span-2"
      >
        <el-input
          v-model="formData.buyerTaxId"
          placeholder="請輸入購買方納稅人識別號"
          class="custom-input"
          @input="handleChange('buyerTaxId', formData.buyerTaxId)"
        />
      </el-form-item>

      <!-- 備註 -->
      <el-form-item
        label="備註"
        prop="remarks"
        class="custom-form-item md:col-span-2"
      >
        <el-input
          v-model="formData.remarks"
          type="textarea"
          :rows="3"
          placeholder="請輸入備註"
          class="custom-input"
          @input="handleChange('remarks', formData.remarks)"
        />
      </el-form-item>
    </div>
  </el-form>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import type { InvoiceData } from '../types/invoice'

interface InvoiceFormProps {
  data: InvoiceData
}

const props = defineProps<InvoiceFormProps>()
const emit = defineEmits<{
  change: [data: InvoiceData]
}>()

const formRef = ref<FormInstance>()

// 標誌：是否正在同步外部資料，避免循環更新
let isSyncing = false

// 表單資料
const formData = reactive<InvoiceData>({
  id: props.data.id,
  invoiceCode: props.data.invoiceCode || '',
  invoiceNumber: props.data.invoiceNumber || '',
  date: props.data.date || '',
  amount: props.data.amount || '',
  taxAmount: props.data.taxAmount || '',
  totalAmount: props.data.totalAmount || '',
  seller: props.data.seller || '',
  sellerTaxId: props.data.sellerTaxId || '',
  buyer: props.data.buyer || '',
  buyerTaxId: props.data.buyerTaxId || '',
  remarks: props.data.remarks || '',
})

// 驗證規則
const rules = reactive<FormRules<InvoiceData>>({
  invoiceCode: [{ required: true, message: '請輸入發票代碼', trigger: 'blur' }],
  invoiceNumber: [
    { required: true, message: '請輸入發票號碼', trigger: 'blur' },
  ],
  date: [{ required: true, message: '請選擇開票日期', trigger: 'change' }],
  totalAmount: [
    { required: true, message: '請輸入價稅合計', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (!value || value === '') {
          callback()
          return
        }
        const numValue = Number(value)
        if (isNaN(numValue)) {
          callback(new Error('價稅合計必須為數字'))
        } else if (numValue < 0) {
          callback(new Error('價稅合計不能為負數'))
        } else {
          callback()
        }
      },
      trigger: 'blur',
    },
  ],
  amount: [
    {
      validator: (_rule, value, callback) => {
        if (!value || value === '') {
          callback()
          return
        }
        const numValue = Number(value)
        if (isNaN(numValue)) {
          callback(new Error('金額必須為數字'))
        } else if (numValue < 0) {
          callback(new Error('金額不能為負數'))
        } else {
          callback()
        }
      },
      trigger: 'blur',
    },
  ],
  taxAmount: [
    {
      validator: (_rule, value, callback) => {
        if (!value || value === '') {
          callback()
          return
        }
        const numValue = Number(value)
        if (isNaN(numValue)) {
          callback(new Error('稅額必須為數字'))
        } else if (numValue < 0) {
          callback(new Error('稅額不能為負數'))
        } else {
          callback()
        }
      },
      trigger: 'blur',
    },
  ],
  seller: [{ required: true, message: '請輸入銷售方名稱', trigger: 'blur' }],
  buyer: [{ required: true, message: '請輸入購買方名稱', trigger: 'blur' }],
})

// 監聽 props.data 的變化，同步到 formData
watch(
  () => props.data,
  (newData) => {
    // 如果正在同步，跳過此次更新
    if (isSyncing) return

    // 檢查是否有實際變化
    const hasChange =
      formData.id !== newData.id ||
      formData.invoiceCode !== (newData.invoiceCode || '') ||
      formData.invoiceNumber !== (newData.invoiceNumber || '') ||
      formData.date !== (newData.date || '') ||
      formData.amount !== (newData.amount || '') ||
      formData.taxAmount !== (newData.taxAmount || '') ||
      formData.totalAmount !== (newData.totalAmount || '') ||
      formData.seller !== (newData.seller || '') ||
      formData.sellerTaxId !== (newData.sellerTaxId || '') ||
      formData.buyer !== (newData.buyer || '') ||
      formData.buyerTaxId !== (newData.buyerTaxId || '') ||
      formData.remarks !== (newData.remarks || '')

    if (hasChange) {
      isSyncing = true
      formData.id = newData.id
      formData.invoiceCode = newData.invoiceCode || ''
      formData.invoiceNumber = newData.invoiceNumber || ''
      formData.date = newData.date || ''
      formData.amount = newData.amount || ''
      formData.taxAmount = newData.taxAmount || ''
      formData.totalAmount = newData.totalAmount || ''
      formData.seller = newData.seller || ''
      formData.sellerTaxId = newData.sellerTaxId || ''
      formData.buyer = newData.buyer || ''
      formData.buyerTaxId = newData.buyerTaxId || ''
      formData.remarks = newData.remarks || ''
      // 使用 nextTick 確保 DOM 更新後再重置標誌
      setTimeout(() => {
        isSyncing = false
      }, 0)
    }
  },
  { deep: true }
)

const handleChange = (
  field: keyof InvoiceData,
  value: string | null | undefined
) => {
  const safeValue = String(value ?? '')

  // 如果值沒有變化，直接返回
  if (formData[field] === safeValue) return // 更新 formData
  ;(formData as any)[field] = safeValue
  // 發送變更事件給父組件
  emit('change', { ...formData, [field]: safeValue })
}

// 暴露驗證方法供父組件使用
defineExpose({
  validate: () => formRef.value?.validate(),
  resetFields: () => formRef.value?.resetFields(),
  clearValidate: () => formRef.value?.clearValidate(),
})
</script>

<style scoped lang="scss">
.custom-form {
  /* 自定義表單標籤樣式 */
  :deep(.el-form-item__label) {
    font-size: 0.75rem;
    font-weight: 600;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin-left: 0.25rem;
    margin-bottom: 0.25rem;
    padding: 0;
  }

  /* 自定義輸入框樣式 */
  :deep(.custom-input .el-input__wrapper) {
    background-color: rgba(255, 255, 255, 0.5);
    backdrop-filter: blur(4px);
    border: 1px solid #e2e8f0;
    border-radius: 0.75rem;
    // padding: 0.5rem 0.75rem;
    box-shadow: none;
    transition: all 0.2s;
  }

  :deep(.custom-input .el-input__wrapper:hover) {
    border-color: #cbd5e1;
  }

  :deep(.custom-input .el-input__wrapper.is-focus) {
    border-color: #3b82f6;
    box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2);
  }

  :deep(.custom-input .el-input__inner) {
    color: #1e293b;
    font-weight: 400;
    padding: 0;
  }

  :deep(.custom-input.pl-8 .el-input__inner) {
    padding-left: 0;
  }

  /* 日期選擇器樣式 */
  :deep(.el-date-editor) {
    width: 100%;
  }

  :deep(.el-date-editor .el-input__wrapper) {
    background-color: rgba(255, 255, 255, 0.5) !important;
    backdrop-filter: blur(4px);
    // border: 1px solid #e2e8f0 !important;
    border-radius: 0.75rem;
    // padding: 0.5rem 0.75rem;
    box-shadow: none !important;
    transition: all 0.2s;
  }

  :deep(.el-date-editor .el-input__wrapper:hover) {
    border-color: #cbd5e1 !important;
  }

  :deep(.el-date-editor .el-input__wrapper.is-focus) {
    border-color: #3b82f6 !important;
    box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2) !important;
  }

  :deep(.el-date-editor .el-input__inner) {
    color: #1e293b;
    font-weight: 400;
    padding: 0;
  }

  :deep(.el-date-editor .el-input__prefix) {
    color: #64748b;
  }

  :deep(.el-date-editor .el-input__suffix) {
    color: #64748b;
  }

  :deep(.el-date-editor .el-input__suffix-inner .el-icon) {
    color: #64748b;
  }

  /* 文字區域樣式 */
  :deep(.el-textarea .el-textarea__inner) {
    background-color: rgba(255, 255, 255, 0.5);
    backdrop-filter: blur(4px);
    border: 1px solid #e2e8f0;
    border-radius: 0.75rem;
    padding: 0.75rem 1rem;
    color: #1e293b;
    resize: none;
    box-shadow: none;
    transition: all 0.2s;
  }

  :deep(.el-textarea .el-textarea__inner:hover) {
    border-color: #cbd5e1;
  }

  :deep(.el-textarea .el-textarea__inner:focus) {
    border-color: #3b82f6;
    box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2);
  }

  /* 表單項樣式 */
  :deep(.custom-form-item) {
    margin-bottom: 0;
  }

  :deep(.el-form-item__error) {
    font-size: 0.75rem;
    padding-top: 0.25rem;
    color: #ef4444;
  }

  /* 數字輸入框樣式 */
  :deep(.custom-input .el-input__inner[type='number']) {
    font-weight: 400;
  }

  /* 價稅合計輸入框加粗 */
  :deep(.total-amount-input .el-input__inner) {
    font-weight: 700;
  }
}
</style>
