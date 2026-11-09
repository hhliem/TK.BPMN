import { defineStore } from 'pinia';
import { ref } from 'vue';

export type AppTheme = 'light' | 'dark';

export const useAppThemeStore = defineStore('appTheme', () => {
  const THEME_STORAGE_KEY = 'bpm_theme';

  const initialTheme: AppTheme =
    (localStorage.getItem(THEME_STORAGE_KEY) as AppTheme) ||
    (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');

  const currentTheme = ref<AppTheme>(initialTheme);

  function applyTheme(theme: AppTheme): void {
    currentTheme.value = theme;
    localStorage.setItem(THEME_STORAGE_KEY, theme);
    if (theme === 'dark') {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }

  function toggleTheme(): void {
    const nextTheme: AppTheme = currentTheme.value === 'dark' ? 'light' : 'dark';
    applyTheme(nextTheme);
  }

  // Initial application
  applyTheme(initialTheme);

  return {
    currentTheme,
    toggleTheme,
    applyTheme
  };
});
