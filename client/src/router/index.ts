import { createRouter, createWebHistory } from 'vue-router';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
    },
    {
      path: '/ai-editor',
      name: 'ai-editor',
      component: () => import('../views/AIEditorView.vue'),
    },
  ],
});

export default router;
