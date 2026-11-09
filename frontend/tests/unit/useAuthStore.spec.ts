/**
 * Unit Test: Auth Store & Permission Checks
 */
import { setActivePinia, createPinia } from 'pinia';
import { useAuthStore } from '@/modules/auth/stores/useAuthStore';
import { tokenStorage } from '@/services/storage/tokenStorage';

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    tokenStorage.clearTokens();
  });

  it('should initialize with null user and unauthenticated status', () => {
    const authStore = useAuthStore();
    expect(authStore.user).toBeNull();
    expect(authStore.isAuthenticated).toBe(false);
  });

  it('should correctly evaluate single and multiple permissions', () => {
    const authStore = useAuthStore();
    authStore.user = {
      id: 'user-01',
      username: 'manager_01',
      email: 'manager@tk.com',
      fullName: 'Trưởng Phòng Duyệt',
      roles: ['MANAGER'],
      permissions: ['workflow:task:approve', 'workflow:task:reject', 'document:view']
    };

    expect(authStore.hasPermission('workflow:task:approve')).toBe(true);
    expect(authStore.hasPermission('admin:roles:edit')).toBe(false);
    expect(authStore.hasPermission(['admin:roles:edit', 'workflow:task:approve'])).toBe(true);
    expect(authStore.hasPermission(['admin:roles:edit', 'org:delete'])).toBe(false);
  });

  it('should grant all permissions when user has role ADMIN or wildcard *', () => {
    const authStore = useAuthStore();
    authStore.user = {
      id: 'admin-01',
      username: 'superadmin',
      email: 'admin@tk.com',
      fullName: 'Quản Trị Viên Cấp Cao',
      roles: ['ADMIN'],
      permissions: ['*']
    };

    expect(authStore.hasPermission('any:custom:permission:here')).toBe(true);
    expect(authStore.hasRole('ADMIN')).toBe(true);
    expect(authStore.hasRole('USER')).toBe(false);
  });
});
