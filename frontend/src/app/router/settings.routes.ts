import type { RouteRecordRaw } from 'vue-router';

export const settingsRoutes: RouteRecordRaw[] = [
  {
    path: 'settings',
    name: 'settings',
    component: () => import('@features/auth/views/settings/SettingsView.vue'),
    meta: { titleKey: 'settings.title', icon: 'mdi-cog' },
  },
  {
    path: 'settings/edit-profile',
    name: 'settings-edit-profile',
    component: () => import('@features/auth/views/settings/SettingsEditProfilePage.vue'),
    meta: { titleKey: 'settings.editProfile', icon: 'mdi-account-edit' },
  },
  {
    path: 'settings/change-password',
    name: 'settings-change-password',
    component: () => import('@features/auth/views/settings/SettingsChangePasswordPage.vue'),
    meta: { titleKey: 'pageTitle.changePassword', icon: 'mdi-lock-reset' },
  },
];
