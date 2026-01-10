<template>
  <button
    :class="[
      baseStyle,
      variants[variant],
      fullWidth ? 'w-full' : '',
      className,
    ]"
    v-bind="$attrs"
    :disabled="disabled"
  >
    <slot />
  </button>
</template>

<script setup lang="ts">
interface ButtonProps {
  variant?: 'primary' | 'secondary' | 'danger'
  fullWidth?: boolean
  className?: string
  disabled?: boolean
}

const props = withDefaults(defineProps<ButtonProps>(), {
  variant: 'primary',
  fullWidth: false,
  className: '',
  disabled: false,
})

const baseStyle =
  'px-6 py-3 rounded-xl font-medium cursor-pointer text-sm transition-all duration-300 transform active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed disabled:active:scale-100 shadow-sm'

const variants = {
  primary: 'bg-slate-900 text-white hover:bg-slate-800 shadow-slate-300/50',
  secondary:
    'bg-white text-slate-700 border border-slate-200 hover:bg-slate-50 hover:border-slate-300',
  danger: 'bg-red-500 text-white hover:bg-red-600 shadow-red-200',
}
</script>
