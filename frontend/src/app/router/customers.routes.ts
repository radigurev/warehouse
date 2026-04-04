import type { RouteRecordRaw } from 'vue-router';

export const customerRoutes: RouteRecordRaw[] = [
  {
    path: 'customers',
    name: 'customers',
    component: () => import('@features/customers/views/CustomersView.vue'),
    meta: { titleKey: 'nav.customers', icon: 'mdi-account-multiple' },
  },
  {
    path: 'customers/create',
    name: 'customer-create',
    component: () => import('@features/customers/views/CustomerCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createCustomer', icon: 'mdi-account-plus' },
  },
  {
    path: 'customers/:id',
    name: 'customer-detail',
    component: () => import('@features/customers/views/CustomerDetailView.vue'),
    meta: { titleKey: 'pageTitle.customerDetail', icon: 'mdi-account' },
  },
  {
    path: 'customers/:id/edit',
    name: 'customer-edit',
    component: () => import('@features/customers/views/CustomerEditPage.vue'),
    meta: { titleKey: 'pageTitle.editCustomer', icon: 'mdi-account-edit' },
  },
  {
    path: 'customer-categories',
    name: 'categories',
    component: () => import('@features/customers/views/categories/CategoriesView.vue'),
    meta: { titleKey: 'nav.categories', icon: 'mdi-tag-multiple' },
  },
  {
    path: 'customer-categories/create',
    name: 'category-create',
    component: () => import('@features/customers/views/categories/CategoryCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createCategory', icon: 'mdi-tag-plus' },
  },
  {
    path: 'customer-categories/:id/edit',
    name: 'category-edit',
    component: () => import('@features/customers/views/categories/CategoryEditPage.vue'),
    meta: { titleKey: 'pageTitle.editCategory', icon: 'mdi-tag-edit' },
  },
];
