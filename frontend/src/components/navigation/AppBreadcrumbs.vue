<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { ChevronRight, Home } from 'lucide-vue-next';

const route = useRoute();

const breadcrumbs = computed(() => {
  const matched = route.matched.filter((r) => r.meta && r.meta.title);
  return matched.map((r) => ({
    title: (r.meta.title as string) || '',
    path: r.path
  }));
});
</script>

<template>
  <nav aria-label="Breadcrumb" class="hidden sm:flex items-center space-x-1.5 text-xs text-muted-foreground select-none">
    <router-link to="/dashboard" class="flex items-center hover:text-foreground transition-colors p-1 rounded">
      <Home class="w-3.5 h-3.5" />
      <span class="sr-only">Trang chủ</span>
    </router-link>

    <template v-for="(item, index) in breadcrumbs" :key="item.path">
      <ChevronRight class="w-3.5 h-3.5 text-muted-foreground/60" />
      <router-link
        v-if="index < breadcrumbs.length - 1"
        :to="item.path"
        class="hover:text-foreground transition-colors p-0.5 rounded font-medium"
      >
        {{ item.title }}
      </router-link>
      <span v-else class="text-foreground font-semibold">
        {{ item.title }}
      </span>
    </template>
  </nav>
</template>
