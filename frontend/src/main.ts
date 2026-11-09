import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import { router } from './router';
import { vPermission } from './directives/vPermission';
import { useAuthStore } from './modules/auth/stores/useAuthStore';

// Main Stylesheet (Tailwind CSS + Design Tokens from Skill 02)
import './assets/styles/main.css';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);

// Register custom directives
app.directive('permission', vPermission);

// Initialize authentication from stored tokens
const authStore = useAuthStore();
authStore.initAuth();

app.mount('#app');
