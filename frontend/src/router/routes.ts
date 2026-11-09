import type { RouteRecordRaw } from 'vue-router';
import AppLayout from '@/layouts/AppLayout.vue';

export const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/modules/auth/views/LoginView.vue'),
    meta: {
      public: true,
      title: 'Đăng nhập'
    }
  },
  {
    path: '/',
    component: AppLayout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Bàn Làm Việc',
          requiresAuth: true
        }
      },
      {
        path: 'workflow/inbox',
        name: 'WorkflowInbox',
        component: () => import('@/modules/common/views/DashboardView.vue'), // Placeholder for Sprint 04
        meta: {
          title: 'Hộp Thư Công Việc',
          requiresAuth: true
        }
      },
      {
        path: 'workflow/definitions',
        name: 'WorkflowDefinitions',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Quy Trình Trình Ký',
          requiresAuth: true,
          permission: 'workflow:view'
        }
      },
      {
        path: 'forms',
        name: 'FormMasterData',
        component: () => import('@/modules/common/views/DashboardView.vue'), // Placeholder for Sprint 03
        meta: {
          title: 'Biểu Mẫu Nghiệp Vụ',
          requiresAuth: true,
          permission: 'form:view'
        }
      },
      {
        path: 'org/tree',
        name: 'OrgTree',
        component: () => import('@/modules/common/views/DashboardView.vue'), // Placeholder for Sprint 02
        meta: {
          title: 'Cây Cơ Cấu Tổ Chức',
          requiresAuth: true,
          permission: 'org:view'
        }
      },
      {
        path: 'org/directory',
        name: 'OrgDirectory',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Danh Bạ Nhân Sự',
          requiresAuth: true,
          permission: 'org:directory:view'
        }
      },
      {
        path: 'notifications',
        name: 'Notifications',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Thông Báo Hệ Thống',
          requiresAuth: true
        }
      },
      {
        path: 'news',
        name: 'InternalNews',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Bản Tin Nội Bộ',
          requiresAuth: true
        }
      },
      {
        path: 'ai-copilot',
        name: 'AiCopilot',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Trợ Lý AI Quy Chế',
          requiresAuth: true
        }
      },
      {
        path: 'analytics/bi',
        name: 'AnalyticsBI',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Dashboard BI',
          requiresAuth: true,
          permission: 'analytics:view'
        }
      },
      {
        path: 'admin/roles',
        name: 'RoleMatrix',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Ma Trận Phân Quyền',
          requiresAuth: true,
          permission: 'auth:manage'
        }
      },
      {
        path: 'admin/settings',
        name: 'SystemSettings',
        component: () => import('@/modules/common/views/DashboardView.vue'),
        meta: {
          title: 'Cấu Hình Hệ Thống',
          requiresAuth: true,
          permission: 'settings:manage'
        }
      }
    ]
  },
  {
    path: '/403',
    name: 'Forbidden',
    component: () => import('@/modules/common/views/ForbiddenView.vue'),
    meta: {
      public: true,
      title: 'Không Có Quyền Truy Cập'
    }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/modules/common/views/NotFoundView.vue'),
    meta: {
      public: true,
      title: 'Không Tìm Thấy Trang'
    }
  }
];
