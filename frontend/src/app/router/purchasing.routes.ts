import type { RouteRecordRaw } from 'vue-router';

export const purchasingRoutes: RouteRecordRaw[] = [
  // Purchase Orders
  {
    path: 'purchasing/purchase-orders',
    name: 'purchase-orders',
    component: () => import('@features/purchasing/views/purchase-orders/PurchaseOrdersView.vue'),
    meta: { titleKey: 'nav.purchaseOrders', icon: 'mdi-file-document-outline' },
  },
  {
    path: 'purchasing/purchase-orders/create',
    name: 'purchase-order-create',
    component: () => import('@features/purchasing/views/purchase-orders/PurchaseOrderCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createPurchaseOrder', icon: 'mdi-file-document-outline' },
  },
  {
    path: 'purchasing/purchase-orders/:id',
    name: 'purchase-order-detail',
    component: () => import('@features/purchasing/views/purchase-orders/PurchaseOrderDetailView.vue'),
    meta: { titleKey: 'pageTitle.purchaseOrderDetail', icon: 'mdi-file-document-outline' },
  },
  {
    path: 'purchasing/purchase-orders/:id/edit',
    name: 'purchase-order-edit',
    component: () => import('@features/purchasing/views/purchase-orders/PurchaseOrderEditPage.vue'),
    meta: { titleKey: 'pageTitle.editPurchaseOrder', icon: 'mdi-file-document-outline' },
  },
  // Suppliers
  {
    path: 'purchasing/suppliers',
    name: 'suppliers',
    component: () => import('@features/purchasing/views/suppliers/SuppliersView.vue'),
    meta: { titleKey: 'nav.suppliers', icon: 'mdi-domain' },
  },
  {
    path: 'purchasing/suppliers/create',
    name: 'supplier-create',
    component: () => import('@features/purchasing/views/suppliers/SupplierCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createSupplier', icon: 'mdi-domain' },
  },
  {
    path: 'purchasing/suppliers/:id',
    name: 'supplier-detail',
    component: () => import('@features/purchasing/views/suppliers/SupplierDetailView.vue'),
    meta: { titleKey: 'pageTitle.supplierDetail', icon: 'mdi-domain' },
  },
  {
    path: 'purchasing/suppliers/:id/edit',
    name: 'supplier-edit',
    component: () => import('@features/purchasing/views/suppliers/SupplierEditPage.vue'),
    meta: { titleKey: 'pageTitle.editSupplier', icon: 'mdi-domain' },
  },
  // Supplier Categories
  {
    path: 'purchasing/supplier-categories',
    name: 'supplier-categories',
    component: () => import('@features/purchasing/views/supplier-categories/SupplierCategoriesView.vue'),
    meta: { titleKey: 'nav.supplierCategories', icon: 'mdi-tag-multiple-outline' },
  },
  {
    path: 'purchasing/supplier-categories/create',
    name: 'supplier-category-create',
    component: () => import('@features/purchasing/views/supplier-categories/SupplierCategoryCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createSupplierCategory', icon: 'mdi-tag-multiple-outline' },
  },
  {
    path: 'purchasing/supplier-categories/:id/edit',
    name: 'supplier-category-edit',
    component: () => import('@features/purchasing/views/supplier-categories/SupplierCategoryEditPage.vue'),
    meta: { titleKey: 'pageTitle.editSupplierCategory', icon: 'mdi-tag-multiple-outline' },
  },
  // Goods Receipts
  {
    path: 'purchasing/goods-receipts',
    name: 'goods-receipts',
    component: () => import('@features/purchasing/views/goods-receipts/GoodsReceiptsView.vue'),
    meta: { titleKey: 'nav.goodsReceipts', icon: 'mdi-package-down' },
  },
  {
    path: 'purchasing/goods-receipts/create',
    name: 'goods-receipt-create',
    component: () => import('@features/purchasing/views/goods-receipts/GoodsReceiptCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createGoodsReceipt', icon: 'mdi-package-down' },
  },
  {
    path: 'purchasing/goods-receipts/:id',
    name: 'goods-receipt-detail',
    component: () => import('@features/purchasing/views/goods-receipts/GoodsReceiptDetailView.vue'),
    meta: { titleKey: 'pageTitle.goodsReceiptDetail', icon: 'mdi-package-down' },
  },
  // Supplier Returns
  {
    path: 'purchasing/supplier-returns',
    name: 'supplier-returns',
    component: () => import('@features/purchasing/views/supplier-returns/SupplierReturnsView.vue'),
    meta: { titleKey: 'nav.supplierReturns', icon: 'mdi-package-up' },
  },
  {
    path: 'purchasing/supplier-returns/create',
    name: 'supplier-return-create',
    component: () => import('@features/purchasing/views/supplier-returns/SupplierReturnCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createSupplierReturn', icon: 'mdi-package-up' },
  },
  {
    path: 'purchasing/supplier-returns/:id',
    name: 'supplier-return-detail',
    component: () => import('@features/purchasing/views/supplier-returns/SupplierReturnDetailView.vue'),
    meta: { titleKey: 'pageTitle.supplierReturnDetail', icon: 'mdi-package-up' },
  },
  // Purchase Events
  {
    path: 'purchasing/purchase-events',
    name: 'purchase-events',
    component: () => import('@features/purchasing/views/purchase-events/PurchaseEventsView.vue'),
    meta: { titleKey: 'nav.purchaseEvents', icon: 'mdi-history' },
  },
];
