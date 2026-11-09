<script setup lang="ts">
import { useAuthStore } from '@/modules/auth/stores/useAuthStore';
import AppBadge from '@/components/ui/AppBadge.vue';
import AppButton from '@/components/ui/AppButton.vue';
import {
  Inbox,
  FileCheck2,
  Clock,
  Sparkles,
  ArrowUpRight,
  PlusCircle,
  FileText
} from 'lucide-vue-next';

const authStore = useAuthStore();

// Demo KPI Cards conforming to Bento Grid style from Skill 02
const kpiStats = [
  {
    title: 'Hồ sơ chờ bạn duyệt',
    value: '4',
    subtitle: '2 hồ sơ cần xử lý trong 2 giờ tới',
    icon: Inbox,
    color: 'text-accent',
    bg: 'bg-accent/10',
    link: '/workflow/inbox'
  },
  {
    title: 'Đề xuất đang trình ký',
    value: '7',
    subtitle: 'Đang ở bước Kế toán trưởng',
    icon: FileCheck2,
    color: 'text-secondary',
    bg: 'bg-secondary/10',
    link: '/workflow/inbox'
  },
  {
    title: 'Tỷ lệ đúng hạn SLA',
    value: '96.8%',
    subtitle: '+2.4% so với tháng trước',
    icon: Clock,
    color: 'text-primary',
    bg: 'bg-primary/10',
    link: '/analytics/bi'
  },
  {
    title: 'Trợ lý AI Copilot',
    value: 'Sẵn sàng',
    subtitle: 'Đã nạp 48 văn bản quy chế mới',
    icon: Sparkles,
    color: 'text-amber-500',
    bg: 'bg-amber-500/10',
    link: '/ai-copilot'
  }
];

// Recent Workflow Tasks sample
const recentTasks = [
  {
    id: 'TSK-2026-089',
    title: 'Đề nghị tạm ứng chi phí công tác Đà Nẵng',
    submitter: 'Nguyễn Văn An (Phòng Kinh doanh)',
    date: 'Hôm nay, 09:30',
    amount: '15.000.000 đ',
    sla: 'Còn 1h 45m',
    slaVariant: 'destructive' as const
  },
  {
    id: 'TSK-2026-088',
    title: 'Thanh toán chi phí bản quyền Microsoft 365',
    submitter: 'Trần Thị Mai (Ban Công nghệ Thông tin)',
    date: 'Hôm nay, 08:15',
    amount: '42.500.000 đ',
    sla: 'Còn 6h 30m',
    slaVariant: 'accent' as const
  },
  {
    id: 'TSK-2026-085',
    title: 'Phê duyệt kế hoạch tuyển dụng Quý 4/2026',
    submitter: 'Lê Hoàng Long (Phòng Nhân sự)',
    date: 'Hôm qua, 16:40',
    amount: '—',
    sla: 'Còn 1 ngày',
    slaVariant: 'primary' as const
  }
];
</script>

<template>
  <div class="space-y-6">
    <!-- Welcome Header Banner -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-surface p-6 rounded-2xl border border-border shadow-card">
      <div>
        <h1 class="text-xl sm:text-2xl font-bold font-heading text-foreground">
          Xin chào, {{ authStore.user?.fullName || 'Người dùng' }} 👋
        </h1>
        <p class="text-sm text-muted-foreground mt-1">
          Hôm nay bạn có <span class="font-semibold text-accent">4 hồ sơ</span> cần phê duyệt trong phiên làm việc.
        </p>
      </div>

      <div class="flex items-center gap-2">
        <router-link to="/forms">
          <AppButton variant="primary" class="font-semibold">
            <PlusCircle class="w-4 h-4 mr-1.5" />
            Tạo đề xuất mới
          </AppButton>
        </router-link>
      </div>
    </div>

    <!-- Bento Grid KPI Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <div
        v-for="kpi in kpiStats"
        :key="kpi.title"
        class="bg-surface p-5 rounded-2xl border border-border shadow-subtle hover:shadow-card transition-all duration-fast flex flex-col justify-between"
      >
        <div class="flex items-center justify-between mb-3">
          <span class="text-xs font-semibold text-muted-foreground tracking-wide uppercase">{{ kpi.title }}</span>
          <div :class="['w-9 h-9 rounded-xl flex items-center justify-center', kpi.bg, kpi.color]">
            <component :is="kpi.icon" class="w-5 h-5" />
          </div>
        </div>

        <div>
          <div class="text-2xl font-bold font-heading text-foreground tracking-tight">{{ kpi.value }}</div>
          <div class="text-xs text-muted-foreground mt-1 flex items-center justify-between">
            <span>{{ kpi.subtitle }}</span>
            <router-link :to="kpi.link" class="text-primary hover:underline flex items-center">
              <ArrowUpRight class="w-3.5 h-3.5" />
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Content: Recent Tasks Table & Quick Access -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Recent Approvals List (2 Cols) -->
      <div class="lg:col-span-2 bg-surface rounded-2xl border border-border shadow-subtle p-5">
        <div class="flex items-center justify-between pb-4 border-b border-border mb-4">
          <div>
            <h2 class="text-base font-bold font-heading text-foreground">Hồ sơ Cần Duyệt Gấp</h2>
            <p class="text-xs text-muted-foreground">Các tác vụ ưu tiên có thời hạn SLA sắp tới</p>
          </div>
          <router-link to="/workflow/inbox" class="text-xs font-semibold text-primary hover:underline">
            Xem tất cả
          </router-link>
        </div>

        <div class="divide-y divide-border">
          <div
            v-for="task in recentTasks"
            :key="task.id"
            class="py-3.5 flex flex-col sm:flex-row sm:items-center justify-between gap-3 hover:bg-muted/40 px-2 rounded-xl transition-colors cursor-pointer"
          >
            <div class="flex items-start gap-3 min-w-0">
              <div class="w-9 h-9 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0 mt-0.5">
                <FileText class="w-5 h-5" />
              </div>
              <div class="min-w-0">
                <p class="text-sm font-semibold text-foreground truncate">{{ task.title }}</p>
                <p class="text-xs text-muted-foreground mt-0.5">{{ task.submitter }} • {{ task.date }}</p>
              </div>
            </div>

            <div class="flex items-center gap-3 shrink-0 self-end sm:self-center">
              <span class="text-xs font-bold text-foreground">{{ task.amount }}</span>
              <AppBadge :variant="task.slaVariant" size="sm">
                {{ task.sla }}
              </AppBadge>
            </div>
          </div>
        </div>
      </div>

      <!-- Quick Shortcuts / Tips (1 Col) -->
      <div class="bg-surface rounded-2xl border border-border shadow-subtle p-5 space-y-4 flex flex-col justify-between">
        <div>
          <h2 class="text-base font-bold font-heading text-foreground mb-1">Cẩm Nang Quy Trình</h2>
          <p class="text-xs text-muted-foreground mb-4">Tra cứu nhanh biểu mẫu và quy định nội bộ</p>

          <div class="space-y-2.5">
            <router-link
              to="/forms"
              class="flex items-center justify-between p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors text-xs font-medium"
            >
              <span>1. Quy định Hạn mức Công tác phí 2026</span>
              <ArrowUpRight class="w-4 h-4 text-muted-foreground" />
            </router-link>

            <router-link
              to="/forms"
              class="flex items-center justify-between p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors text-xs font-medium"
            >
              <span>2. Thủ tục Tạm ứng & Hoàn ứng Hóa đơn</span>
              <ArrowUpRight class="w-4 h-4 text-muted-foreground" />
            </router-link>

            <router-link
              to="/forms"
              class="flex items-center justify-between p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors text-xs font-medium"
            >
              <span>3. Ma trận Phân cấp Thẩm quyền Ký duyệt</span>
              <ArrowUpRight class="w-4 h-4 text-muted-foreground" />
            </router-link>
          </div>
        </div>

        <div class="p-3.5 bg-primary/5 rounded-xl border border-primary/15 text-xs">
          <p class="font-semibold text-primary">Mẹo phím tắt nhanh:</p>
          <p class="text-muted-foreground mt-0.5">Nhấn <kbd class="px-1.5 py-0.5 bg-muted rounded border text-foreground font-mono">Ctrl + Space</kbd> để gọi ngay Trợ lý AI hỏi quy chế.</p>
        </div>
      </div>
    </div>
  </div>
</template>
