<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../stores/useAuthStore';
import AppButton from '@/components/ui/AppButton.vue';
import AppInput from '@/components/ui/AppInput.vue';
import { ShieldCheck, Lock, User, ArrowRight } from 'lucide-vue-next';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const username = ref('admin');
const password = ref('Admin@123');
const rememberMe = ref(true);
const errorMessage = ref('');
const isLoading = ref(false);

async function handleLogin(): Promise<void> {
  if (!username.value || !password.value) {
    errorMessage.value = 'Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu';
    return;
  }

  errorMessage.value = '';
  isLoading.value = true;

  try {
    await authStore.login({
      username: username.value,
      password: password.value,
      rememberMe: rememberMe.value
    });

    // Redirect to redirect query param or default dashboard
    const redirectUrl = (route.query.redirect as string) || '/dashboard';
    await router.push(redirectUrl);
  } catch (err: unknown) {
    const error = err as { response?: { data?: { message?: string } } };
    errorMessage.value = error.response?.data?.message || 'Tên đăng nhập hoặc mật khẩu không chính xác';
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="min-h-screen flex flex-col justify-center items-center p-4 bg-background relative overflow-hidden">
    <!-- Subtle Background Pattern from Swiss Minimalist -->
    <div class="absolute inset-0 bg-[radial-gradient(#CBD5E1_1px,transparent_1px)] [background-size:24px_24px] opacity-40 dark:opacity-10 pointer-events-none" />

    <!-- Card Container -->
    <div class="w-full max-w-md bg-surface text-foreground rounded-2xl border border-border shadow-modal p-8 z-10 animate-fade-in">
      <!-- Brand Header -->
      <div class="flex flex-col items-center text-center mb-8">
        <div class="w-14 h-14 bg-primary text-primary-foreground rounded-2xl flex items-center justify-center shadow-card mb-4">
          <ShieldCheck class="w-8 h-8 text-accent" />
        </div>
        <h1 class="text-2xl font-bold tracking-tight text-foreground font-heading">
          Enterprise BPMN
        </h1>
        <p class="text-sm text-muted-foreground mt-1">
          Nền tảng Quản trị và Thực thi Quy trình Doanh nghiệp
        </p>
      </div>

      <!-- Error Toast/Alert -->
      <div
        v-if="errorMessage"
        role="alert"
        class="mb-6 p-3 bg-destructive/10 border border-destructive/20 text-destructive text-sm rounded-lg font-medium animate-fade-in flex items-center gap-2"
      >
        <span class="w-2 h-2 rounded-full bg-destructive shrink-0" />
        {{ errorMessage }}
      </div>

      <!-- Session Expired Notification -->
      <div
        v-if="route.query.expired"
        role="alert"
        class="mb-6 p-3 bg-amber-500/10 border border-amber-500/30 text-amber-700 dark:text-amber-400 text-sm rounded-lg font-medium animate-fade-in"
      >
        Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại để tiếp tục.
      </div>

      <!-- Form -->
      <form @submit.prevent="handleLogin" class="space-y-4">
        <div>
          <AppInput
            v-model="username"
            label="Tên đăng nhập"
            placeholder="Nhập tên tài khoản (VD: admin)"
            required
            :disabled="isLoading"
          >
            <template #suffix>
              <User class="w-4 h-4 text-muted-foreground mr-3" />
            </template>
          </AppInput>
        </div>

        <div>
          <AppInput
            v-model="password"
            label="Mật khẩu"
            type="password"
            placeholder="Nhập mật khẩu"
            required
            :disabled="isLoading"
          >
            <template #suffix>
              <Lock class="w-4 h-4 text-muted-foreground mr-3" />
            </template>
          </AppInput>
        </div>

        <!-- Options: Remember Me & Forgot Password -->
        <div class="flex items-center justify-between text-sm py-1">
          <label class="flex items-center gap-2 select-none cursor-pointer text-muted-foreground hover:text-foreground">
            <input
              type="checkbox"
              v-model="rememberMe"
              class="rounded border-border text-primary focus:ring-primary w-4 h-4 cursor-pointer"
            />
            Ghi nhớ đăng nhập
          </label>

          <a href="#" class="text-xs font-semibold text-primary hover:underline">
            Quên mật khẩu?
          </a>
        </div>

        <!-- Submit Button -->
        <div class="pt-2">
          <AppButton
            variant="primary"
            type="submit"
            size="lg"
            class="w-full text-base font-semibold group"
            :loading="isLoading"
          >
            <span>Đăng nhập hệ thống</span>
            <ArrowRight class="w-4 h-4 ml-1 transition-transform group-hover:translate-x-1" />
          </AppButton>
        </div>
      </form>

      <!-- Trust Footer -->
      <div class="mt-8 pt-6 border-t border-border/60 text-center">
        <p class="text-xs text-muted-foreground">
          Bảo mật đa tầng với kiểm toán bất biến & RBAC/ABAC
        </p>
      </div>
    </div>
  </div>
</template>
