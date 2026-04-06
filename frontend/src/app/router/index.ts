import { createRouter, createWebHistory } from 'vue-router';
import type { RouteRecordRaw } from 'vue-router';
import { authRoutes } from './auth.routes';
import { customerRoutes } from './customers.routes';
import { inventoryRoutes } from './inventory.routes';
import { settingsRoutes } from './settings.routes';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@features/auth/views/LoginView.vue'),
    meta: { requiresAuth: false },
  },
  {
    path: '/',
    component: () => import('@shared/components/templates/DefaultLayout.vue'),
    meta: { requiresAuth: true },
    children: [...authRoutes, ...customerRoutes, ...inventoryRoutes, ...settingsRoutes],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach((to, _from, next) => {
  const accessToken = localStorage.getItem('accessToken');
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth !== false);

  if (requiresAuth && !accessToken) {
    next({ name: 'login', query: { redirect: to.fullPath } });
  } else if (to.name === 'login' && accessToken) {
    next({ name: 'dashboard' });
  } else {
    next();
  }
});

export default router;
