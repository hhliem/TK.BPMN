<script setup lang="ts">
import { computed } from 'vue';
import { Loader2 } from 'lucide-vue-next';

interface Props {
  variant?: 'primary' | 'secondary' | 'accent' | 'destructive' | 'ghost' | 'outline';
  size?: 'sm' | 'md' | 'lg';
  type?: 'button' | 'submit' | 'reset';
  disabled?: boolean;
  loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'md',
  type: 'button',
  disabled: false,
  loading: false
});

const variantClasses = computed(() => {
  switch (props.variant) {
    case 'primary':
      return 'bg-primary text-primary-foreground hover:bg-primary-hover shadow-subtle focus-visible:ring-primary';
    case 'secondary':
      return 'bg-secondary text-secondary-foreground hover:bg-secondary-hover shadow-subtle focus-visible:ring-secondary';
    case 'accent':
      return 'bg-accent text-accent-foreground hover:bg-accent-hover shadow-subtle focus-visible:ring-accent font-semibold';
    case 'destructive':
      return 'bg-destructive text-destructive-foreground hover:bg-destructive-hover shadow-subtle focus-visible:ring-destructive';
    case 'outline':
      return 'border border-border bg-transparent hover:bg-muted text-foreground focus-visible:ring-ring';
    case 'ghost':
      return 'bg-transparent hover:bg-muted text-foreground focus-visible:ring-ring';
    default:
      return 'bg-primary text-primary-foreground hover:bg-primary-hover';
  }
});

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm':
      return 'h-8 px-3 text-xs gap-1.5';
    case 'lg':
      return 'h-12 px-6 text-base gap-2.5';
    case 'md':
    default:
      return 'h-10 px-4 text-sm gap-2';
  }
});
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    :class="[
      'inline-flex items-center justify-center rounded-lg font-medium transition-all duration-fast select-none cursor-pointer',
      'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed',
      variantClasses,
      sizeClasses
    ]"
  >
    <Loader2 v-if="loading" class="w-4 h-4 animate-spin shrink-0" />
    <slot />
  </button>
</template>
