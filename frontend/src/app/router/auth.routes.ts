import type { RouteRecordRaw } from 'vue-router';

export const authRoutes: RouteRecordRaw[] = [
  {
    path: '',
    name: 'dashboard',
    component: () => import('@features/auth/views/DashboardView.vue'),
    meta: { titleKey: 'nav.dashboard', icon: 'mdi-view-dashboard' },
  },
  {
    path: 'users',
    name: 'users',
    component: () => import('@features/auth/views/users/UsersView.vue'),
    meta: { titleKey: 'nav.users', icon: 'mdi-account-group' },
  },
  {
    path: 'users/create',
    name: 'user-create',
    component: () => import('@features/auth/views/users/UserCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createUser', icon: 'mdi-account-plus' },
  },
  {
    path: 'users/:id/edit',
    name: 'user-edit',
    component: () => import('@features/auth/views/users/UserEditPage.vue'),
    meta: { titleKey: 'pageTitle.editUser', icon: 'mdi-account-edit' },
  },
  {
    path: 'users/:id/password',
    name: 'user-password',
    component: () => import('@features/auth/views/users/UserPasswordPage.vue'),
    meta: { titleKey: 'pageTitle.changePassword', icon: 'mdi-lock-reset' },
  },
  {
    path: 'users/:id/roles',
    name: 'user-roles',
    component: () => import('@features/auth/views/users/UserRolesPage.vue'),
    meta: { titleKey: 'pageTitle.manageUserRoles', icon: 'mdi-shield-account' },
  },
  {
    path: 'roles',
    name: 'roles',
    component: () => import('@features/auth/views/roles/RolesView.vue'),
    meta: { titleKey: 'nav.roles', icon: 'mdi-shield-account' },
  },
  {
    path: 'roles/create',
    name: 'role-create',
    component: () => import('@features/auth/views/roles/RoleCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createRole', icon: 'mdi-shield-plus' },
  },
  {
    path: 'roles/:id/edit',
    name: 'role-edit',
    component: () => import('@features/auth/views/roles/RoleEditPage.vue'),
    meta: { titleKey: 'pageTitle.editRole', icon: 'mdi-shield-edit' },
  },
  {
    path: 'roles/:id/permissions',
    name: 'role-permissions',
    component: () => import('@features/auth/views/roles/RolePermissionsPage.vue'),
    meta: { titleKey: 'pageTitle.manageRolePermissions', icon: 'mdi-key' },
  },
  {
    path: 'permissions',
    name: 'permissions',
    component: () => import('@features/auth/views/permissions/PermissionsView.vue'),
    meta: { titleKey: 'nav.permissions', icon: 'mdi-key' },
  },
  {
    path: 'permissions/create',
    name: 'permission-create',
    component: () => import('@features/auth/views/permissions/PermissionCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createPermission', icon: 'mdi-key-plus' },
  },
  {
    path: 'audit',
    name: 'audit',
    component: () => import('@features/auth/views/audit/AuditView.vue'),
    meta: { titleKey: 'nav.audit', icon: 'mdi-clipboard-text-clock' },
  },
];
