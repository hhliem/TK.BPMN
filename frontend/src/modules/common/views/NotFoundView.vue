<script setup lang="ts">
import { useRouter } from 'vue-router';
import AppButton from '@/components/ui/AppButton.vue';
import { Compass, Home, ArrowLeft } from 'lucide-vue-next';

const router = useRouter();
</script>

<template>
  <div class="min-h-screen flex items-center justify-center p-6 bg-background">
    <div class="max-w-md w-full text-center space-y-6">
      <div class="w-20 h-20 bg-secondary/10 text-secondary rounded-3xl mx-auto flex items-center justify-center shadow-card">
        <Compass class="w-10 h-10" />
      </div>

      <div class="space-y-2">
        <h1 class="text-3xl font-bold font-heading text-foreground">404 — Không tìm thấy trang</h1>
        <p class="text-sm text-muted-foreground">
          Đường dẫn bạn yêu cầu không tồn tại hoặc đã được thay đổi trong hệ thống.
        </p>
      </div>

      <div class="flex justify-center gap-3 pt-2">
        <AppButton variant="outline" @click="router.back()">
          <ArrowLeft class="w-4 h-4 mr-1.5" />
          Quay lại
        </AppButton>
        <AppButton variant="primary" @click="router.push('/dashboard')">
          <Home class="w-4 h-4 mr-1.5" />
          Về trang chủ
        </AppButton>
      </div>
    </div>
  </div>
</template>
