import { createRouter, createWebHistory } from 'vue-router';
import { routes } from './routes';
import { useAuthStore } from '@/modules/auth/stores/useAuthStore';
import { tokenStorage } from '@/services/storage/tokenStorage';

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior() {
    return { top: 0 };
  }
});

// Dynamic Title & Navigation Guards
router.beforeEach(async (to, _from, next) => {
  // 1. Update Document Title
  const baseTitle = 'Enterprise BPMN Platform';
  document.title = to.meta.title ? `${to.meta.title} | ${baseTitle}` : baseTitle;

  const authStore = useAuthStore();
  const token = tokenStorage.getAccessToken();

  // 2. If token exists but user profile not in store yet, init profile
  if (token && !authStore.user) {
    authStore.initAuth();
  }

  // 3. If accessing /login while already authenticated, go to dashboard
  if (to.path === '/login' && token) {
    return next('/dashboard');
  }

  // 4. Check if route is public
  if (to.meta.public) {
    return next();
  }

  // 5. Check Authentication
  if (to.meta.requiresAuth && !token) {
    return next({
      path: '/login',
      query: { redirect: to.fullPath }
    });
  }

  // 6. Check RBAC Permissions
  if (to.meta.permission) {
    const requiredPermission = to.meta.permission as string | string[];
    if (!authStore.hasPermission(requiredPermission)) {
      return next('/403');
    }
  }

  next();
});

export default router;
