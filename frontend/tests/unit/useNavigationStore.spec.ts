/**
 * Unit Test: Navigation Store and Dynamic RBAC Filtering
 */
import { setActivePinia, createPinia } from 'pinia';
import { useNavigationStore } from '@/stores/useNavigationStore';
import { useAuthStore } from '@/modules/auth/stores/useAuthStore';

describe('useNavigationStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
  });

  it('should toggle sidebar collapsed state and persist to localStorage', () => {
    const navStore = useNavigationStore();
    expect(navStore.isCollapsed).toBe(false);

    navStore.toggleSidebar();
    expect(navStore.isCollapsed).toBe(true);
    expect(localStorage.getItem('bpm_sidebar_collapsed')).toBe('true');

    navStore.toggleSidebar();
    expect(navStore.isCollapsed).toBe(false);
    expect(localStorage.getItem('bpm_sidebar_collapsed')).toBe('false');
  });

  it('should filter navigation items dynamically based on user permissions', () => {
    const authStore = useAuthStore();
    const navStore = useNavigationStore();

    // User only has org:view permission
    authStore.user = {
      id: 'user-1',
      username: 'nv_an',
      email: 'an@tk.com',
      fullName: 'Nguyễn Văn An',
      roles: ['USER'],
      permissions: ['org:view']
    };

    const groups = navStore.authorizedGroups;

    // Core group should always be present (Dashboard, Inbox have no special permission)
    const coreGroup = groups.find((g) => g.id === 'core');
    expect(coreGroup).toBeDefined();

    // Org group should have 'Cây Cơ Cấu Tổ Chức' but NOT 'Danh Bạ Nhân Sự'
    const orgGroup = groups.find((g) => g.id === 'organization');
    expect(orgGroup).toBeDefined();
    const itemIds = orgGroup?.items.map((i) => i.id);
    expect(itemIds).toContain('org-chart');
    expect(itemIds).not.toContain('employees');

    // Admin group should be hidden because user lacks auth:manage / settings:manage
    const adminGroup = groups.find((g) => g.id === 'analytics-governance');
    expect(adminGroup).toBeUndefined();
  });

  it('should show all navigation items for ADMIN role', () => {
    const authStore = useAuthStore();
    const navStore = useNavigationStore();

    authStore.user = {
      id: 'admin-1',
      username: 'admin',
      email: 'admin@tk.com',
      fullName: 'Quản trị viên',
      roles: ['ADMIN'],
      permissions: ['*']
    };

    const groups = navStore.authorizedGroups;
    const adminGroup = groups.find((g) => g.id === 'analytics-governance');
    expect(adminGroup).toBeDefined();
    expect(adminGroup?.items.length).toBe(3);
  });
});
