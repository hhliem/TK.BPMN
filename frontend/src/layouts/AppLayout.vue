<script setup lang="ts">
import { ref } from 'vue';
import AppSidebar from './AppSidebar.vue';
import AppHeader from './AppHeader.vue';
import AppToast, { type ToastMessage } from '@/components/feedback/AppToast.vue';

const toastRef = ref<InstanceType<typeof AppToast> | null>(null);

function handleShowToast(toast: Omit<ToastMessage, 'id'>): void {
  toastRef.value?.addToast(toast);
}
</script>

<template>
  <div class="min-h-screen flex bg-background text-foreground">
    <!-- Collapsible Sidebar -->
    <AppSidebar />

    <!-- Main Content Area -->
    <div class="flex-1 flex flex-col min-w-0">
      <!-- Fixed Header -->
      <AppHeader @show-toast="handleShowToast" />

      <!-- Page Outlet with Animation -->
      <main class="flex-1 p-4 sm:p-6 md:p-8 max-w-7xl w-full mx-auto animate-fade-in">
        <router-view />
      </main>
    </div>

    <!-- Global Toast System -->
    <AppToast ref="toastRef" />
  </div>
</template>
