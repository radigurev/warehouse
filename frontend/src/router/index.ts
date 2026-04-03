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
    component: () => import('@/components/templates/DefaultLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'dashboard',
        component: () => import('@/views/DashboardView.vue'),
        meta: { titleKey: 'nav.dashboard', icon: 'mdi-view-dashboard' },
      },
      {
        path: 'users',
        name: 'users',
        component: () => import('@/views/users/UsersView.vue'),
        meta: { titleKey: 'nav.users', icon: 'mdi-account-group' },
      },
      {
        path: 'users/create',
        name: 'user-create',
        component: () => import('@/views/users/UserCreatePage.vue'),
        meta: { titleKey: 'pageTitle.createUser', icon: 'mdi-account-plus' },
      },
      {
        path: 'users/:id/edit',
        name: 'user-edit',
        component: () => import('@/views/users/UserEditPage.vue'),
        meta: { titleKey: 'pageTitle.editUser', icon: 'mdi-account-edit' },
      },
      {
        path: 'users/:id/password',
        name: 'user-password',
        component: () => import('@/views/users/UserPasswordPage.vue'),
        meta: { titleKey: 'pageTitle.changePassword', icon: 'mdi-lock-reset' },
      },
      {
        path: 'users/:id/roles',
        name: 'user-roles',
        component: () => import('@/views/users/UserRolesPage.vue'),
        meta: { titleKey: 'pageTitle.manageUserRoles', icon: 'mdi-shield-account' },
      },
      {
        path: 'roles',
        name: 'roles',
        component: () => import('@/views/roles/RolesView.vue'),
        meta: { titleKey: 'nav.roles', icon: 'mdi-shield-account' },
      },
      {
        path: 'roles/create',
        name: 'role-create',
        component: () => import('@/views/roles/RoleCreatePage.vue'),
        meta: { titleKey: 'pageTitle.createRole', icon: 'mdi-shield-plus' },
      },
      {
        path: 'roles/:id/edit',
        name: 'role-edit',
        component: () => import('@/views/roles/RoleEditPage.vue'),
        meta: { titleKey: 'pageTitle.editRole', icon: 'mdi-shield-edit' },
      },
      {
        path: 'roles/:id/permissions',
        name: 'role-permissions',
        component: () => import('@/views/roles/RolePermissionsPage.vue'),
        meta: { titleKey: 'pageTitle.manageRolePermissions', icon: 'mdi-key' },
      },
      {
        path: 'permissions',
        name: 'permissions',
        component: () => import('@/views/permissions/PermissionsView.vue'),
        meta: { titleKey: 'nav.permissions', icon: 'mdi-key' },
      },
      {
        path: 'permissions/create',
        name: 'permission-create',
        component: () => import('@/views/permissions/PermissionCreatePage.vue'),
        meta: { titleKey: 'pageTitle.createPermission', icon: 'mdi-key-plus' },
      },
      {
        path: 'audit',
        name: 'audit',
        component: () => import('@/views/audit/AuditView.vue'),
        meta: { titleKey: 'nav.audit', icon: 'mdi-clipboard-text-clock' },
      },
      {
        path: 'settings',
        name: 'settings',
        component: () => import('@/views/settings/SettingsView.vue'),
        meta: { titleKey: 'settings.title', icon: 'mdi-cog' },
      },
      {
        path: 'settings/edit-profile',
        name: 'settings-edit-profile',
        component: () => import('@/views/settings/SettingsEditProfilePage.vue'),
        meta: { titleKey: 'settings.editProfile', icon: 'mdi-account-edit' },
      },
      {
        path: 'settings/change-password',
        name: 'settings-change-password',
        component: () => import('@/views/settings/SettingsChangePasswordPage.vue'),
        meta: { titleKey: 'pageTitle.changePassword', icon: 'mdi-lock-reset' },
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
