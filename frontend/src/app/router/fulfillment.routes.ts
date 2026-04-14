import type { RouteRecordRaw } from 'vue-router';

export const fulfillmentRoutes: RouteRecordRaw[] = [
  // Sales Orders
  {
    path: 'fulfillment/sales-orders',
    name: 'sales-orders',
    component: () => import('@features/fulfillment/views/sales-orders/SalesOrdersView.vue'),
    meta: { titleKey: 'nav.salesOrders', icon: 'mdi-cart-outline' },
  },
  {
    path: 'fulfillment/sales-orders/create',
    name: 'sales-order-create',
    component: () => import('@features/fulfillment/views/sales-orders/SalesOrderCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createSalesOrder', icon: 'mdi-cart-outline' },
  },
  {
    path: 'fulfillment/sales-orders/:id',
    name: 'sales-order-detail',
    component: () => import('@features/fulfillment/views/sales-orders/SalesOrderDetailView.vue'),
    meta: { titleKey: 'pageTitle.salesOrderDetail', icon: 'mdi-cart-outline' },
  },
  {
    path: 'fulfillment/sales-orders/:id/edit',
    name: 'sales-order-edit',
    component: () => import('@features/fulfillment/views/sales-orders/SalesOrderEditPage.vue'),
    meta: { titleKey: 'pageTitle.editSalesOrder', icon: 'mdi-cart-outline' },
  },
  // Pick Lists
  {
    path: 'fulfillment/pick-lists',
    name: 'pick-lists',
    component: () => import('@features/fulfillment/views/pick-lists/PickListsView.vue'),
    meta: { titleKey: 'nav.pickLists', icon: 'mdi-format-list-checks' },
  },
  {
    path: 'fulfillment/pick-lists/:id',
    name: 'pick-list-detail',
    component: () => import('@features/fulfillment/views/pick-lists/PickListDetailView.vue'),
    meta: { titleKey: 'pageTitle.pickListDetail', icon: 'mdi-format-list-checks' },
  },
  // Shipments
  {
    path: 'fulfillment/shipments',
    name: 'shipments',
    component: () => import('@features/fulfillment/views/shipments/ShipmentsView.vue'),
    meta: { titleKey: 'nav.shipments', icon: 'mdi-truck-delivery' },
  },
  {
    path: 'fulfillment/shipments/:id',
    name: 'shipment-detail',
    component: () => import('@features/fulfillment/views/shipments/ShipmentDetailView.vue'),
    meta: { titleKey: 'pageTitle.shipmentDetail', icon: 'mdi-truck-delivery' },
  },
  // Carriers
  {
    path: 'fulfillment/carriers',
    name: 'carriers',
    component: () => import('@features/fulfillment/views/carriers/CarriersView.vue'),
    meta: { titleKey: 'nav.carriers', icon: 'mdi-truck-fast-outline' },
  },
  {
    path: 'fulfillment/carriers/create',
    name: 'carrier-create',
    component: () => import('@features/fulfillment/views/carriers/CarrierCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createCarrier', icon: 'mdi-truck-fast-outline' },
  },
  {
    path: 'fulfillment/carriers/:id',
    name: 'carrier-detail',
    component: () => import('@features/fulfillment/views/carriers/CarrierDetailView.vue'),
    meta: { titleKey: 'pageTitle.carrierDetail', icon: 'mdi-truck-fast-outline' },
  },
  {
    path: 'fulfillment/carriers/:id/edit',
    name: 'carrier-edit',
    component: () => import('@features/fulfillment/views/carriers/CarrierEditPage.vue'),
    meta: { titleKey: 'pageTitle.editCarrier', icon: 'mdi-truck-fast-outline' },
  },
  // Customer Returns
  {
    path: 'fulfillment/customer-returns',
    name: 'customer-returns',
    component: () => import('@features/fulfillment/views/customer-returns/CustomerReturnsView.vue'),
    meta: { titleKey: 'nav.customerReturns', icon: 'mdi-package-variant-closed-remove' },
  },
  {
    path: 'fulfillment/customer-returns/create',
    name: 'customer-return-create',
    component: () => import('@features/fulfillment/views/customer-returns/CustomerReturnCreatePage.vue'),
    meta: { titleKey: 'pageTitle.createCustomerReturn', icon: 'mdi-package-variant-closed-remove' },
  },
  {
    path: 'fulfillment/customer-returns/:id',
    name: 'customer-return-detail',
    component: () => import('@features/fulfillment/views/customer-returns/CustomerReturnDetailView.vue'),
    meta: { titleKey: 'pageTitle.customerReturnDetail', icon: 'mdi-package-variant-closed-remove' },
  },
  // Fulfillment Events
  {
    path: 'fulfillment/fulfillment-events',
    name: 'fulfillment-events',
    component: () => import('@features/fulfillment/views/fulfillment-events/FulfillmentEventsView.vue'),
    meta: { titleKey: 'nav.fulfillmentEvents', icon: 'mdi-history' },
  },
];
