<script setup lang="ts">
import { computed } from 'vue';

interface Props {
  variant?: 'primary' | 'secondary' | 'accent' | 'destructive' | 'muted' | 'outline';
  size?: 'sm' | 'md';
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'md'
});

const variantClasses = computed(() => {
  switch (props.variant) {
    case 'primary':
      return 'bg-primary/10 text-primary border-primary/20';
    case 'secondary':
      return 'bg-secondary/10 text-secondary border-secondary/20';
    case 'accent':
      return 'bg-accent/15 text-accent-hover dark:text-accent border-accent/30 font-semibold';
    case 'destructive':
      return 'bg-destructive/10 text-destructive border-destructive/20';
    case 'muted':
      return 'bg-muted text-muted-foreground border-border';
    case 'outline':
      return 'bg-transparent text-foreground border-border';
    default:
      return 'bg-primary/10 text-primary border-primary/20';
  }
});

const sizeClasses = computed(() => {
  return props.size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-2.5 py-1 text-xs';
});
</script>

<template>
  <span
    :class="[
      'inline-flex items-center rounded-full font-medium border leading-none tracking-wide select-none',
      variantClasses,
      sizeClasses
    ]"
  >
    <slot />
  </span>
</template>
