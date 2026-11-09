import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { NavigationGroup, NavigationItem } from '@/types/navigation.types';
import { useAuthStore } from '@/modules/auth/stores/useAuthStore';
import {
  LayoutDashboard,
  GitBranch,
  Inbox,
  Building2,
  FileText,
  Bell,
  Newspaper,
  Bot,
  BarChart3,
  ShieldCheck,
  Settings
} from 'lucide-vue-next';

export const useNavigationStore = defineStore('navigation', () => {
  const SIDEBAR_COLLAPSED_KEY = 'bpm_sidebar_collapsed';

  const isCollapsed = ref<boolean>(
    localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === 'true'
  );

  function toggleSidebar(): void {
    isCollapsed.value = !isCollapsed.value;
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(isCollapsed.value));
  }

  function setSidebarCollapsed(collapsed: boolean): void {
    isCollapsed.value = collapsed;
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(collapsed));
  }

  // Base navigation definition structured according to 11 Enterprise Modules
  const navigationGroups = ref<NavigationGroup[]>([
    {
      id: 'core',
      title: 'TỔNG QUAN',
      items: [
        {
          id: 'dashboard',
          label: 'Bàn Làm Việc',
          path: '/dashboard',
          icon: LayoutDashboard
        },
        {
          id: 'inbox',
          label: 'Hộp Thư Công Việc',
          path: '/workflow/inbox',
          icon: Inbox,
          badge: '4',
          badgeVariant: 'accent'
        }
      ]
    },
    {
      id: 'workflow-management',
      title: 'QUY TRÌNH & HỒ SƠ',
      items: [
        {
          id: 'workflows',
          label: 'Quy Trình Trình Ký',
          path: '/workflow/definitions',
          icon: GitBranch,
          permission: 'workflow:view'
        },
        {
          id: 'forms',
          label: 'Biểu Mẫu Nghiệp Vụ',
          path: '/forms',
          icon: FileText,
          permission: 'form:view'
        }
      ]
    },
    {
      id: 'organization',
      title: 'TỔ CHỨC & DANH BẠ',
      items: [
        {
          id: 'org-chart',
          label: 'Cây Cơ Cấu Tổ Chức',
          path: '/org/tree',
          icon: Building2,
          permission: 'org:view'
        },
        {
          id: 'employees',
          label: 'Danh Bạ Nhân Sự',
          path: '/org/directory',
          icon: Building2,
          permission: 'org:directory:view'
        }
      ]
    },
    {
      id: 'communication',
      title: 'TRUYỀN THÔNG & AI',
      items: [
        {
          id: 'notifications',
          label: 'Thông Báo Hệ Thống',
          path: '/notifications',
          icon: Bell
        },
        {
          id: 'news',
          label: 'Bản Tin Nội Bộ',
          path: '/news',
          icon: Newspaper
        },
        {
          id: 'ai-assistant',
          label: 'Trợ Lý AI Quy Chế',
          path: '/ai-copilot',
          icon: Bot,
          badge: 'AI',
          badgeVariant: 'primary'
        }
      ]
    },
    {
      id: 'analytics-governance',
      title: 'BÁO CÁO & QUẢN TRỊ',
      items: [
        {
          id: 'analytics',
          label: 'Dashboard BI & Heatmap',
          path: '/analytics/bi',
          icon: BarChart3,
          permission: 'analytics:view'
        },
        {
          id: 'access-control',
          label: 'Ma Trận Phân Quyền',
          path: '/admin/roles',
          icon: ShieldCheck,
          permission: 'auth:manage'
        },
        {
          id: 'settings',
          label: 'Cấu Hình Hệ Thống',
          path: '/admin/settings',
          icon: Settings,
          permission: 'settings:manage'
        }
      ]
    }
  ]);

  // Dynamically filter navigation groups based on current user permissions
  const authorizedGroups = computed<NavigationGroup[]>(() => {
    const authStore = useAuthStore();

    return navigationGroups.value
      .map((group) => {
        // Filter group-level permission
        if (group.permission && !authStore.hasPermission(group.permission)) {
          return null;
        }

        // Filter item-level permissions
        const visibleItems = group.items.filter((item: NavigationItem) => {
          if (!item.permission) return true;
          return authStore.hasPermission(item.permission);
        });

        if (visibleItems.length === 0) return null;

        return {
          ...group,
          items: visibleItems
        };
      })
      .filter((group): group is NavigationGroup => group !== null);
  });

  return {
    isCollapsed,
    navigationGroups,
    authorizedGroups,
    toggleSidebar,
    setSidebarCollapsed
  };
});
