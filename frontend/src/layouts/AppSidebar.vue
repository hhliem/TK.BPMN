<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useNavigationStore } from '@/stores/useNavigationStore';
import AppBadge from '@/components/ui/AppBadge.vue';
import AppTooltip from '@/components/ui/AppTooltip.vue';

const route = useRoute();
const navStore = useNavigationStore();

const sidebarWidthClass = computed(() => {
  return navStore.isCollapsed ? 'w-[68px]' : 'w-[260px]';
});

function isItemActive(path: string): boolean {
  if (path === '/dashboard') {
    return route.path === '/dashboard';
  }
  return route.path.startsWith(path);
}
</script>

<template>
  <aside
    :class="[
      'h-screen sticky top-0 flex flex-col border-r border-border bg-surface transition-all duration-300 z-40 select-none shrink-0',
      sidebarWidthClass
    ]"
  >
    <!-- Brand Header -->
    <div class="h-16 flex items-center px-4 border-b border-border gap-3 overflow-hidden">
      <img src="/logo.svg" alt="Enterprise BPM" class="w-8 h-8 shrink-0 rounded-lg shadow-subtle" />
      <div v-if="!navStore.isCollapsed" class="flex flex-col overflow-hidden animate-fade-in">
        <span class="text-sm font-bold font-heading tracking-tight text-foreground whitespace-nowrap">
          ENTERPRISE BPM
        </span>
        <span class="text-[10px] text-muted-foreground whitespace-nowrap uppercase tracking-wider font-semibold">
          Platform Edition
        </span>
      </div>
    </div>

    <!-- Navigation Menu List -->
    <div class="flex-1 overflow-y-auto px-3 py-4 space-y-6">
      <div
        v-for="group in navStore.authorizedGroups"
        :key="group.id"
        class="space-y-1"
      >
        <!-- Group Header (Hidden if collapsed) -->
        <div
          v-if="!navStore.isCollapsed"
          class="px-3 text-[11px] font-bold text-muted-foreground/80 tracking-wider uppercase mb-1.5"
        >
          {{ group.title }}
        </div>

        <!-- Group Divider (If collapsed) -->
        <div v-else class="h-px bg-border my-2 mx-2" />

        <!-- Navigation Items -->
        <div
          v-for="item in group.items"
          :key="item.id"
        >
          <!-- When Sidebar is Collapsed -> Wrap with Tooltip -->
          <AppTooltip
            v-if="navStore.isCollapsed"
            :text="item.label"
            position="right"
            class="w-full"
          >
            <router-link
              :to="item.path"
              :class="[
                'flex items-center justify-center w-11 h-11 mx-auto rounded-xl transition-all cursor-pointer',
                isItemActive(item.path)
                  ? 'bg-primary text-primary-foreground shadow-subtle'
                  : 'text-muted-foreground hover:text-foreground hover:bg-muted'
              ]"
            >
              <component :is="item.icon" class="w-5 h-5 shrink-0" />
            </router-link>
          </AppTooltip>

          <!-- When Sidebar is Expanded -->
          <router-link
            v-else
            :to="item.path"
            :class="[
              'flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-medium transition-all cursor-pointer group',
              isItemActive(item.path)
                ? 'bg-primary text-primary-foreground shadow-subtle font-semibold'
                : 'text-muted-foreground hover:text-foreground hover:bg-muted'
            ]"
          >
            <div class="flex items-center gap-3 min-w-0">
              <component
                :is="item.icon"
                :class="[
                  'w-5 h-5 shrink-0 transition-colors',
                  isItemActive(item.path) ? 'text-primary-foreground' : 'text-muted-foreground group-hover:text-foreground'
                ]"
              />
              <span class="truncate">{{ item.label }}</span>
            </div>

            <!-- Optional Badge -->
            <AppBadge
              v-if="item.badge"
              :variant="item.badgeVariant || 'primary'"
              size="sm"
            >
              {{ item.badge }}
            </AppBadge>
          </router-link>
        </div>
      </div>
    </div>

    <!-- Sidebar Footer / Version Info -->
    <div class="p-3 border-t border-border text-center overflow-hidden">
      <div v-if="!navStore.isCollapsed" class="text-[11px] text-muted-foreground truncate animate-fade-in">
        <span>BPM Engine v3.0 • .NET & Vue</span>
      </div>
      <div v-else class="text-[10px] text-muted-foreground font-semibold">
        v3.0
      </div>
    </div>
  </aside>
</template>
