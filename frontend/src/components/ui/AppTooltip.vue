<script setup lang="ts">
import { ref } from 'vue';

interface Props {
  text: string;
  position?: 'top' | 'right' | 'bottom' | 'left';
}

withDefaults(defineProps<Props>(), {
  position: 'right'
});

const isVisible = ref(false);
</script>

<template>
  <div
    class="relative inline-flex items-center"
    @mouseenter="isVisible = true"
    @mouseleave="isVisible = false"
    @focusin="isVisible = true"
    @focusout="isVisible = false"
  >
    <slot />
    <div
      v-if="isVisible"
      role="tooltip"
      :class="[
        'absolute z-50 px-2 py-1 text-xs font-medium text-white bg-slate-900 dark:bg-slate-800 rounded shadow-modal whitespace-nowrap pointer-events-none animate-fade-in',
        position === 'right' ? 'left-full ml-2 top-1/2 -translate-y-1/2' : '',
        position === 'left' ? 'right-full mr-2 top-1/2 -translate-y-1/2' : '',
        position === 'top' ? 'bottom-full mb-2 left-1/2 -translate-x-1/2' : '',
        position === 'bottom' ? 'top-full mt-2 left-1/2 -translate-x-1/2' : ''
      ]"
    >
      {{ text }}
    </div>
  </div>
</template>
