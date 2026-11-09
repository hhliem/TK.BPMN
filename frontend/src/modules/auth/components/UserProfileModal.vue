<script setup lang="ts">
import { ref, computed } from 'vue';
import { useAuthStore } from '../stores/useAuthStore';
import { authApi } from '../api/authApi';
import AppButton from '@/components/ui/AppButton.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppAvatar from '@/components/ui/AppAvatar.vue';
import AppBadge from '@/components/ui/AppBadge.vue';
import { User, Lock, X } from 'lucide-vue-next';

interface Props {
  isOpen: boolean;
}

defineProps<Props>();
const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'success', message: string): void;
}>();

const authStore = useAuthStore();
const activeTab = ref<'profile' | 'password'>('profile');

// Change Password form state
const currentPassword = ref('');
const newPassword = ref('');
const confirmPassword = ref('');
const errorMessage = ref('');
const isSubmitting = ref(false);

const isPasswordMatching = computed(() => {
  if (!confirmPassword.value) return true;
  return newPassword.value === confirmPassword.value;
});

const canSubmitPassword = computed(() => {
  return (
    currentPassword.value.length >= 6 &&
    newPassword.value.length >= 6 &&
    isPasswordMatching.value &&
    !isSubmitting.value
  );
});

async function handleChangePassword(): Promise<void> {
  if (!canSubmitPassword.value) return;

  errorMessage.value = '';
  isSubmitting.value = true;
  try {
    await authApi.changePassword({
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
      confirmPassword: confirmPassword.value
    });

    // Reset fields
    currentPassword.value = '';
    newPassword.value = '';
    confirmPassword.value = '';

    emit('success', 'Đổi mật khẩu thành công! Vui lòng bảo mật mật khẩu mới.');
    emit('close');
  } catch (err: unknown) {
    const error = err as { response?: { data?: { message?: string } } };
    errorMessage.value = error.response?.data?.message || 'Đổi mật khẩu thất bại. Vui lòng kiểm tra lại mật khẩu hiện tại.';
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs animate-fade-in"
    @keydown.esc="emit('close')"
  >
    <!-- Modal Card -->
    <div
      class="bg-surface text-foreground w-full max-w-lg rounded-xl border border-border shadow-modal overflow-hidden flex flex-col max-h-[90vh]"
    >
      <!-- Header -->
      <div class="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
        <div class="flex items-center gap-3">
          <AppAvatar :name="authStore.user?.fullName" size="md" />
          <div>
            <h3 class="text-base font-semibold leading-tight">{{ authStore.user?.fullName || 'Tài khoản' }}</h3>
            <p class="text-xs text-muted-foreground">{{ authStore.user?.email || 'N/A' }}</p>
          </div>
        </div>

        <button
          type="button"
          @click="emit('close')"
          class="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors cursor-pointer"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Navigation Tabs -->
      <div class="flex border-b border-border px-6 pt-2 gap-4">
        <button
          type="button"
          @click="activeTab = 'profile'"
          :class="[
            'pb-3 text-sm font-medium border-b-2 flex items-center gap-2 transition-colors cursor-pointer',
            activeTab === 'profile'
              ? 'border-primary text-primary font-semibold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          ]"
        >
          <User class="w-4 h-4" />
          Thông tin cá nhân
        </button>
        <button
          type="button"
          @click="activeTab = 'password'"
          :class="[
            'pb-3 text-sm font-medium border-b-2 flex items-center gap-2 transition-colors cursor-pointer',
            activeTab === 'password'
              ? 'border-primary text-primary font-semibold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          ]"
        >
          <Lock class="w-4 h-4" />
          Đổi mật khẩu
        </button>
      </div>

      <!-- Body Content -->
      <div class="p-6 overflow-y-auto flex-1">
        <!-- Tab 1: Profile -->
        <div v-if="activeTab === 'profile'" class="space-y-4">
          <div class="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span class="text-xs text-muted-foreground block">Tên đăng nhập</span>
              <span class="font-medium text-foreground">{{ authStore.user?.username }}</span>
            </div>
            <div>
              <span class="text-xs text-muted-foreground block">Email</span>
              <span class="font-medium text-foreground">{{ authStore.user?.email }}</span>
            </div>
            <div>
              <span class="text-xs text-muted-foreground block">Chức vụ</span>
              <span class="font-medium text-foreground">{{ authStore.user?.title || 'Chuyên viên' }}</span>
            </div>
            <div>
              <span class="text-xs text-muted-foreground block">Phòng ban</span>
              <span class="font-medium text-foreground">{{ authStore.user?.departmentName || 'Hành chính Tổng hợp' }}</span>
            </div>
          </div>

          <div class="pt-3 border-t border-border">
            <span class="text-xs text-muted-foreground block mb-2">Vai trò hệ thống</span>
            <div class="flex flex-wrap gap-1.5">
              <AppBadge
                v-for="role in authStore.user?.roles"
                :key="role"
                variant="primary"
                size="sm"
              >
                {{ role }}
              </AppBadge>
            </div>
          </div>
        </div>

        <!-- Tab 2: Change Password -->
        <form v-else @submit.prevent="handleChangePassword" class="space-y-4">
          <div v-if="errorMessage" class="p-3 bg-destructive/10 border border-destructive/20 text-destructive text-xs rounded-lg font-medium">
            {{ errorMessage }}
          </div>

          <AppInput
            v-model="currentPassword"
            label="Mật khẩu hiện tại"
            type="password"
            placeholder="Nhập mật khẩu đang dùng"
            required
          />

          <AppInput
            v-model="newPassword"
            label="Mật khẩu mới (Tối thiểu 6 ký tự)"
            type="password"
            placeholder="Nhập mật khẩu mới"
            required
          />

          <AppInput
            v-model="confirmPassword"
            label="Xác nhận mật khẩu mới"
            type="password"
            placeholder="Nhập lại mật khẩu mới"
            :error="!isPasswordMatching ? 'Mật khẩu xác nhận không trùng khớp' : ''"
            required
          />

          <div class="flex justify-end gap-3 pt-3">
            <AppButton variant="outline" type="button" @click="emit('close')">
              Hủy
            </AppButton>
            <AppButton
              variant="primary"
              type="submit"
              :disabled="!canSubmitPassword"
              :loading="isSubmitting"
            >
              Cập nhật mật khẩu
            </AppButton>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
