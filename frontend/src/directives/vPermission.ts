import type { Directive, DirectiveBinding } from 'vue';
import { useAuthStore } from '@/modules/auth/stores/useAuthStore';

/**
 * Custom Directive: v-permission
 * Example:
 * <button v-permission="'workflow:task:approve'">Duyệt hồ sơ</button>
 * <div v-permission="['org:view', 'org:edit']">Nội dung quản trị</div>
 */
export const vPermission: Directive = {
  mounted(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
    const { value } = binding;
    if (!value) return;

    const authStore = useAuthStore();
    const hasRequiredPermission = authStore.hasPermission(value);

    if (!hasRequiredPermission) {
      el.parentNode?.removeChild(el);
    }
  },
  updated(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
    const { value } = binding;
    if (!value) return;

    const authStore = useAuthStore();
    const hasRequiredPermission = authStore.hasPermission(value);

    if (!hasRequiredPermission) {
      el.parentNode?.removeChild(el);
    }
  }
};

export default vPermission;
