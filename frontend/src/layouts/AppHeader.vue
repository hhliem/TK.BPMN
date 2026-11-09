<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useNavigationStore } from '@/stores/useNavigationStore';
import { useAppThemeStore } from '@/stores/useAppThemeStore';
import { useAuthStore } from '@/modules/auth/stores/useAuthStore';
import AppBreadcrumbs from '@/components/navigation/AppBreadcrumbs.vue';
import AppAvatar from '@/components/ui/AppAvatar.vue';
import UserProfileModal from '@/modules/auth/components/UserProfileModal.vue';
import {
  Menu,
  Sun,
  Moon,
  Bell,
  Search,
  User,
  LogOut,
  ChevronDown
} from 'lucide-vue-next';

const emit = defineEmits<{
  (e: 'show-toast', message: { type: 'success' | 'error' | 'warning' | 'info'; title: string; message?: string }): void;
}>();

const router = useRouter();
const navStore = useNavigationStore();
const themeStore = useAppThemeStore();
const authStore = useAuthStore();

const isUserDropdownOpen = ref(false);
const isProfileModalOpen = ref(false);

async function handleLogout(): Promise<void> {
  isUserDropdownOpen.value = false;
  await authStore.logout();
  await router.push('/login');
}

function handleProfileSuccess(msg: string): void {
  emit('show-toast', {
    type: 'success',
    title: 'Thông báo',
    message: msg
  });
}
</script>

<template>
  <header class="h-16 border-b border-border bg-surface/95 backdrop-blur-xs px-4 sm:px-6 flex items-center justify-between sticky top-0 z-30 transition-colors">
    <!-- Left: Sidebar Toggle & Breadcrumbs -->
    <div class="flex items-center gap-4">
      <button
        type="button"
        @click="navStore.toggleSidebar"
        class="p-2 text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors cursor-pointer"
        aria-label="Toggle Sidebar"
      >
        <Menu class="w-5 h-5" />
      </button>

      <AppBreadcrumbs />
    </div>

    <!-- Center: Quick Search Bar -->
    <div class="hidden md:flex items-center relative max-w-xs w-full">
      <Search class="w-4 h-4 text-muted-foreground absolute left-3 pointer-events-none" />
      <input
        type="text"
        placeholder="Tìm kiếm tác vụ, quy trình... (Ctrl + K)"
        class="w-full h-9 pl-9 pr-4 text-xs bg-muted/50 rounded-lg border border-transparent focus:border-border focus:bg-surface text-foreground transition-all focus:outline-none"
      />
    </div>

    <!-- Right: Theme, Realtime Status, Notifications & User Menu -->
    <div class="flex items-center gap-2 sm:gap-3">
      <!-- Dark/Light Mode Toggle -->
      <button
        type="button"
        @click="themeStore.toggleTheme"
        class="p-2 text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors cursor-pointer"
        :aria-label="themeStore.currentTheme === 'dark' ? 'Chuyển sang giao diện Sáng' : 'Chuyển sang giao diện Tối'"
      >
        <Sun v-if="themeStore.currentTheme === 'dark'" class="w-5 h-5 text-amber-400" />
        <Moon v-else class="w-5 h-5" />
      </button>

      <!-- Notifications Bell -->
      <button
        type="button"
        class="p-2 text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg relative transition-colors cursor-pointer"
        aria-label="Xem thông báo"
      >
        <Bell class="w-5 h-5" />
        <span class="absolute top-1.5 right-1.5 w-2 h-2 bg-accent rounded-full ring-2 ring-surface animate-pulse" />
      </button>

      <div class="h-5 w-px bg-border mx-1" />

      <!-- User Profile Menu -->
      <div class="relative">
        <button
          type="button"
          @click="isUserDropdownOpen = !isUserDropdownOpen"
          class="flex items-center gap-2.5 p-1 rounded-lg hover:bg-muted transition-colors cursor-pointer"
        >
          <AppAvatar :name="authStore.user?.fullName" size="sm" />
          <div class="hidden lg:flex flex-col text-left">
            <span class="text-xs font-semibold leading-tight text-foreground">{{ authStore.user?.fullName || 'Tài khoản' }}</span>
            <span class="text-[10px] text-muted-foreground leading-tight">{{ authStore.user?.title || 'Chuyên viên' }}</span>
          </div>
          <ChevronDown class="w-3.5 h-3.5 text-muted-foreground hidden lg:block" />
        </button>

        <!-- Dropdown Menu -->
        <div
          v-if="isUserDropdownOpen"
          class="absolute right-0 mt-2 w-56 bg-surface text-foreground rounded-xl border border-border shadow-modal py-1.5 z-50 animate-fade-in"
          @click.outside="isUserDropdownOpen = false"
        >
          <div class="px-3.5 py-2 border-b border-border/80 lg:hidden">
            <p class="text-xs font-semibold text-foreground">{{ authStore.user?.fullName }}</p>
            <p class="text-[11px] text-muted-foreground">{{ authStore.user?.email }}</p>
          </div>

          <button
            type="button"
            @click="isProfileModalOpen = true; isUserDropdownOpen = false"
            class="w-full flex items-center gap-2.5 px-3.5 py-2 text-xs text-foreground hover:bg-muted transition-colors text-left cursor-pointer"
          >
            <User class="w-4 h-4 text-muted-foreground" />
            Thông tin tài khoản
          </button>

          <div class="my-1 border-t border-border/80" />

          <button
            type="button"
            @click="handleLogout"
            class="w-full flex items-center gap-2.5 px-3.5 py-2 text-xs text-destructive hover:bg-destructive/10 transition-colors text-left cursor-pointer"
          >
            <LogOut class="w-4 h-4" />
            Đăng xuất an toàn
          </button>
        </div>
      </div>
    </div>

    <!-- User Profile Modal -->
    <UserProfileModal
      :is-open="isProfileModalOpen"
      @close="isProfileModalOpen = false"
      @success="handleProfileSuccess"
    />
  </header>
</template>
