import type { RouteRecordRaw } from 'vue-router';

export const inventoryRoutes: RouteRecordRaw[] = [
  // Products
  {
    path: 'products',
    name: 'products',
    component: () => import('@features/inventory/views/products/ProductsView.vue'),
    meta: { titleKey: 'nav.products', icon: 'mdi-package-variant-closed' },
  },
  {
    path: 'products/create',
    name: 'product-create',
    component: () => import('@features/inventory/views/products/ProductCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createProduct', icon: 'mdi-package-variant-closed-plus' },
  },
  {
    path: 'products/:id',
    name: 'product-detail',
    component: () => import('@features/inventory/views/products/ProductDetailView.vue'),
    meta: { titleKey: 'pageTitle.productDetail', icon: 'mdi-package-variant-closed' },
  },
  {
    path: 'products/:id/edit',
    name: 'product-edit',
    component: () => import('@features/inventory/views/products/ProductEditPage.vue'),
    meta: { titleKey: 'pageTitle.editProduct', icon: 'mdi-package-variant-closed' },
  },
  // Product Categories
  {
    path: 'product-categories',
    name: 'product-categories',
    component: () => import('@features/inventory/views/product-categories/ProductCategoriesView.vue'),
    meta: { titleKey: 'nav.productCategories', icon: 'mdi-tag-multiple' },
  },
  {
    path: 'product-categories/create',
    name: 'product-category-create',
    component: () => import('@features/inventory/views/product-categories/ProductCategoryCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createProductCategory', icon: 'mdi-tag-plus' },
  },
  {
    path: 'product-categories/:id/edit',
    name: 'product-category-edit',
    component: () => import('@features/inventory/views/product-categories/ProductCategoryEditPage.vue'),
    meta: { titleKey: 'pageTitle.editProductCategory', icon: 'mdi-tag-edit' },
  },
  // Units of Measure
  {
    path: 'units-of-measure',
    name: 'units-of-measure',
    component: () => import('@features/inventory/views/units-of-measure/UnitsOfMeasureView.vue'),
    meta: { titleKey: 'nav.unitsOfMeasure', icon: 'mdi-ruler' },
  },
  {
    path: 'units-of-measure/create',
    name: 'unit-create',
    component: () => import('@features/inventory/views/units-of-measure/UnitCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createUnit', icon: 'mdi-ruler' },
  },
  {
    path: 'units-of-measure/:id/edit',
    name: 'unit-edit',
    component: () => import('@features/inventory/views/units-of-measure/UnitEditPage.vue'),
    meta: { titleKey: 'pageTitle.editUnit', icon: 'mdi-ruler' },
  },
  // Warehouses
  {
    path: 'warehouses',
    name: 'warehouses',
    component: () => import('@features/inventory/views/warehouses/WarehousesView.vue'),
    meta: { titleKey: 'nav.warehouses', icon: 'mdi-warehouse' },
  },
  {
    path: 'warehouses/create',
    name: 'warehouse-create',
    component: () => import('@features/inventory/views/warehouses/WarehouseCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createWarehouse', icon: 'mdi-warehouse' },
  },
  {
    path: 'warehouses/:id',
    name: 'warehouse-detail',
    component: () => import('@features/inventory/views/warehouses/WarehouseDetailView.vue'),
    meta: { titleKey: 'pageTitle.warehouseDetail', icon: 'mdi-warehouse' },
  },
  {
    path: 'warehouses/:id/edit',
    name: 'warehouse-edit',
    component: () => import('@features/inventory/views/warehouses/WarehouseEditPage.vue'),
    meta: { titleKey: 'pageTitle.editWarehouse', icon: 'mdi-warehouse' },
  },
  // Stock Levels
  {
    path: 'stock-levels',
    name: 'stock-levels',
    component: () => import('@features/inventory/views/stock-levels/StockLevelsView.vue'),
    meta: { titleKey: 'nav.stockLevels', icon: 'mdi-package-variant' },
  },
  // Stock Movements
  {
    path: 'stock-movements',
    name: 'stock-movements',
    component: () => import('@features/inventory/views/stock-movements/StockMovementsView.vue'),
    meta: { titleKey: 'nav.stockMovements', icon: 'mdi-swap-horizontal' },
  },
  // Batches
  {
    path: 'batches',
    name: 'batches',
    component: () => import('@features/inventory/views/batches/BatchesView.vue'),
    meta: { titleKey: 'nav.batches', icon: 'mdi-barcode' },
  },
  {
    path: 'batches/create',
    name: 'batch-create',
    component: () => import('@features/inventory/views/batches/BatchCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createBatch', icon: 'mdi-barcode' },
  },
  {
    path: 'batches/:id/edit',
    name: 'batch-edit',
    component: () => import('@features/inventory/views/batches/BatchEditPage.vue'),
    meta: { titleKey: 'pageTitle.editBatch', icon: 'mdi-barcode' },
  },
  // Adjustments
  {
    path: 'adjustments',
    name: 'adjustments',
    component: () => import('@features/inventory/views/adjustments/AdjustmentsView.vue'),
    meta: { titleKey: 'nav.adjustments', icon: 'mdi-pencil-ruler' },
  },
  {
    path: 'adjustments/create',
    name: 'adjustment-create',
    component: () => import('@features/inventory/views/adjustments/AdjustmentCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createAdjustment', icon: 'mdi-pencil-ruler' },
  },
  {
    path: 'adjustments/:id',
    name: 'adjustment-detail',
    component: () => import('@features/inventory/views/adjustments/AdjustmentDetailView.vue'),
    meta: { titleKey: 'pageTitle.adjustmentDetail', icon: 'mdi-pencil-ruler' },
  },
  // Transfers
  {
    path: 'transfers',
    name: 'transfers',
    component: () => import('@features/inventory/views/transfers/TransfersView.vue'),
    meta: { titleKey: 'nav.transfers', icon: 'mdi-truck' },
  },
  {
    path: 'transfers/create',
    name: 'transfer-create',
    component: () => import('@features/inventory/views/transfers/TransferCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createTransfer', icon: 'mdi-truck' },
  },
  {
    path: 'transfers/:id',
    name: 'transfer-detail',
    component: () => import('@features/inventory/views/transfers/TransferDetailView.vue'),
    meta: { titleKey: 'pageTitle.transferDetail', icon: 'mdi-truck' },
  },
  // Stocktake
  {
    path: 'stocktake',
    name: 'stocktake',
    component: () => import('@features/inventory/views/stocktake/StocktakeView.vue'),
    meta: { titleKey: 'nav.stocktake', icon: 'mdi-clipboard-check' },
  },
  {
    path: 'stocktake/create',
    name: 'stocktake-create',
    component: () => import('@features/inventory/views/stocktake/StocktakeCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createStocktake', icon: 'mdi-clipboard-check' },
  },
  {
    path: 'stocktake/:id',
    name: 'stocktake-detail',
    component: () => import('@features/inventory/views/stocktake/StocktakeDetailView.vue'),
    meta: { titleKey: 'pageTitle.stocktakeDetail', icon: 'mdi-clipboard-check' },
  },
];
