import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authApi } from '../api/authApi';
import { tokenStorage } from '@/services/storage/tokenStorage';
import type { UserProfile, LoginRequest } from '../types/auth.types';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<UserProfile | null>(null);
  const token = ref<string | null>(tokenStorage.getAccessToken());
  const isLoading = ref<boolean>(false);

  const isAuthenticated = computed<boolean>(() => !!token.value && !!user.value);

  // Initialize cached user profile from storage
  function initAuth(): void {
    const cachedUser = tokenStorage.getUser();
    if (cachedUser) {
      try {
        user.value = JSON.parse(cachedUser) as UserProfile;
      } catch {
        user.value = null;
      }
    }
  }

  async function login(credentials: LoginRequest): Promise<void> {
    isLoading.value = true;
    try {
      const response = await authApi.login(credentials);
      token.value = response.accessToken;
      user.value = response.user;

      const remember = credentials.rememberMe ?? true;
      tokenStorage.setAccessToken(response.accessToken, remember);
      tokenStorage.setRefreshToken(response.refreshToken, remember);
      tokenStorage.setUser(JSON.stringify(response.user), remember);
    } finally {
      isLoading.value = false;
    }
  }

  async function logout(): Promise<void> {
    isLoading.value = true;
    try {
      await authApi.logout();
    } finally {
      token.value = null;
      user.value = null;
      tokenStorage.clearTokens();
      isLoading.value = false;
    }
  }

  async function fetchProfile(): Promise<void> {
    if (!tokenStorage.getAccessToken()) return;
    try {
      const profile = await authApi.getProfile();
      user.value = profile;
      tokenStorage.setUser(JSON.stringify(profile));
    } catch {
      // If profile fetch fails, session might be invalid
      await logout();
    }
  }

  function hasPermission(permission: string | string[]): boolean {
    if (!user.value || !user.value.permissions) return false;
    // SuperAdmin bypass
    if (user.value.roles?.includes('ADMIN') || user.value.permissions.includes('*')) {
      return true;
    }

    if (Array.isArray(permission)) {
      return permission.some((p) => user.value?.permissions.includes(p));
    }
    return user.value.permissions.includes(permission);
  }

  function hasRole(role: string): boolean {
    if (!user.value || !user.value.roles) return false;
    return user.value.roles.includes(role);
  }

  return {
    user,
    token,
    isLoading,
    isAuthenticated,
    initAuth,
    login,
    logout,
    fetchProfile,
    hasPermission,
    hasRole
  };
});
