<script setup lang="ts">
import { computed } from 'vue';

interface Props {
  src?: string;
  name?: string;
  size?: 'sm' | 'md' | 'lg';
}

const props = withDefaults(defineProps<Props>(), {
  name: 'User',
  size: 'md'
});

const initials = computed(() => {
  if (!props.name) return 'U';
  const parts = props.name.trim().split(' ');
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
});

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm':
      return 'w-7 h-7 text-xs';
    case 'lg':
      return 'w-12 h-12 text-base';
    case 'md':
    default:
      return 'w-9 h-9 text-sm';
  }
});
</script>

<template>
  <div
    :class="[
      'relative inline-flex items-center justify-center shrink-0 rounded-full font-semibold select-none overflow-hidden border border-border/80 shadow-subtle',
      'bg-primary/10 text-primary',
      sizeClasses
    ]"
  >
    <img
      v-if="src"
      :src="src"
      :alt="name"
      class="w-full h-full object-cover"
    />
    <span v-else>{{ initials }}</span>
  </div>
</template>
