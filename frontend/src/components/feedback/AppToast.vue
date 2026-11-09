<script setup lang="ts">
import { ref } from 'vue';
import { CheckCircle2, AlertCircle, Info, X } from 'lucide-vue-next';

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message?: string;
}

const toasts = ref<ToastMessage[]>([]);

function addToast(toast: Omit<ToastMessage, 'id'>, duration = 4000): void {
  const id = Math.random().toString(36).substring(2, 9);
  const newToast: ToastMessage = { ...toast, id };
  toasts.value.push(newToast);

  if (duration > 0) {
    setTimeout(() => {
      removeToast(id);
    }, duration);
  }
}

function removeToast(id: string): void {
  toasts.value = toasts.value.filter((t) => t.id !== id);
}

defineExpose({
  addToast,
  removeToast
});
</script>

<template>
  <div class="fixed top-4 right-4 z-50 flex flex-col gap-2 max-w-sm w-full pointer-events-none">
    <TransitionGroup
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="transform translate-x-8 opacity-0"
      enter-to-class="transform translate-x-0 opacity-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-for="toast in toasts"
        :key="toast.id"
        :class="[
          'pointer-events-auto flex items-start gap-3 p-3.5 rounded-lg border shadow-modal bg-surface text-foreground',
          toast.type === 'success' ? 'border-accent/40 bg-accent/5' : '',
          toast.type === 'error' ? 'border-destructive/40 bg-destructive/5' : '',
          toast.type === 'warning' ? 'border-amber-500/40 bg-amber-500/5' : '',
          toast.type === 'info' ? 'border-secondary/40 bg-secondary/5' : ''
        ]"
      >
        <div class="shrink-0 mt-0.5">
          <CheckCircle2 v-if="toast.type === 'success'" class="w-5 h-5 text-accent" />
          <AlertCircle v-else-if="toast.type === 'error'" class="w-5 h-5 text-destructive" />
          <AlertCircle v-else-if="toast.type === 'warning'" class="w-5 h-5 text-amber-500" />
          <Info v-else class="w-5 h-5 text-secondary" />
        </div>

        <div class="flex-1 text-sm">
          <div class="font-semibold text-foreground">{{ toast.title }}</div>
          <div v-if="toast.message" class="text-xs text-muted-foreground mt-0.5">{{ toast.message }}</div>
        </div>

        <button
          type="button"
          @click="removeToast(toast.id)"
          class="shrink-0 text-muted-foreground hover:text-foreground transition-colors p-0.5 rounded cursor-pointer"
        >
          <X class="w-4 h-4" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>
