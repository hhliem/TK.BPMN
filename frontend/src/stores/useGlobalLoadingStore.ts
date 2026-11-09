import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useGlobalLoadingStore = defineStore('globalLoading', () => {
  const isLoading = ref<boolean>(false);
  const loadingMessage = ref<string>('Đang xử lý...');

  function startLoading(message = 'Đang xử lý...'): void {
    loadingMessage.value = message;
    isLoading.value = true;
  }

  function stopLoading(): void {
    isLoading.value = false;
  }

  return {
    isLoading,
    loadingMessage,
    startLoading,
    stopLoading
  };
});
