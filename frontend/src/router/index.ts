import { createRouter, createWebHistory } from 'vue-router';
import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { requiresAuth: false },
  },
  {
    path: '/',
    component: () => import('@/layouts/DefaultLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'dashboard',
        component: () => import('@/views/DashboardView.vue'),
      },
      {
        path: 'users',
        name: 'users',
        component: () => import('@/views/users/UsersView.vue'),
      },
      {
        path: 'roles',
        name: 'roles',
        component: () => import('@/views/roles/RolesView.vue'),
      },
      {
        path: 'permissions',
        name: 'permissions',
        component: () => import('@/views/permissions/PermissionsView.vue'),
      },
      {
        path: 'audit',
        name: 'audit',
        component: () => import('@/views/audit/AuditView.vue'),
      },
    ],
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
