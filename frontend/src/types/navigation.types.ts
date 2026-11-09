import type { Component } from 'vue';

export interface NavigationItem {
  id: string;
  label: string;
  path: string;
  icon?: Component | string;
  permission?: string | string[]; // Permission code required to view this item
  badge?: string | number;
  badgeVariant?: 'primary' | 'accent' | 'destructive' | 'muted';
  children?: NavigationItem[];
  isExternal?: boolean;
}

export interface NavigationGroup {
  id: string;
  title: string;
  items: NavigationItem[];
  permission?: string | string[];
}
