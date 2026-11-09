import { useAuthStore } from '@/modules/auth/stores/useAuthStore';

export function hasPermission(permission: string | string[]): boolean {
  const authStore = useAuthStore();
  return authStore.hasPermission(permission);
}

export function hasRole(role: string): boolean {
  const authStore = useAuthStore();
  return authStore.hasRole(role);
}
