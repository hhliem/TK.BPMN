<script setup lang="ts">
import { useRouter } from 'vue-router';
import AppButton from '@/components/ui/AppButton.vue';
import { ShieldAlert, ArrowLeft, Home } from 'lucide-vue-next';

const router = useRouter();
</script>

<template>
  <div class="min-h-screen flex items-center justify-center p-6 bg-background">
    <div class="max-w-md w-full text-center space-y-6">
      <div class="w-20 h-20 bg-destructive/10 text-destructive rounded-3xl mx-auto flex items-center justify-center shadow-card">
        <ShieldAlert class="w-10 h-10" />
      </div>

      <div class="space-y-2">
        <h1 class="text-3xl font-bold font-heading text-foreground">403 — Không có quyền truy cập</h1>
        <p class="text-sm text-muted-foreground">
          Bạn không có quyền hạn cần thiết để truy cập phân hệ hoặc tính năng này. Vui lòng liên hệ Quản trị viên hệ thống nếu bạn cần phân quyền.
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
