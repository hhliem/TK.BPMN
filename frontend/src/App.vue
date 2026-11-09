<script setup lang="ts">
import { useGlobalLoadingStore } from '@/stores/useGlobalLoadingStore';
import { Loader2 } from 'lucide-vue-next';

const globalLoading = useGlobalLoadingStore();
</script>

<template>
  <div id="bpm-root" class="min-h-screen">
    <!-- Router View -->
    <router-view />

    <!-- Global Loading Overlay -->
    <div
      v-if="globalLoading.isLoading"
      role="status"
      aria-live="polite"
      class="fixed inset-0 z-50 flex flex-col items-center justify-center bg-slate-900/40 backdrop-blur-xs animate-fade-in text-white"
    >
      <Loader2 class="w-10 h-10 animate-spin text-accent mb-3" />
      <span class="text-sm font-medium">{{ globalLoading.loadingMessage }}</span>
    </div>
  </div>
</template>
