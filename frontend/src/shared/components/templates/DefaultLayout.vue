<template>
  <v-app>
    <v-navigation-drawer
      v-model="drawerOpen"
      :rail="layout.sidebarCollapsed"
      permanent
      color="#334155"
      theme="dark"
    >
      <v-list-item
        :title="layout.sidebarCollapsed ? 'WMS' : t('app.title')"
        class="pa-4"
      >
        <template #prepend>
          <v-icon icon="mdi-warehouse" size="28" />
        </template>
      </v-list-item>

      <v-divider />

      <v-list nav :density="layout.vuetifyDensity" class="sidebar-nav">
        <v-list-item
          to="/"
          prepend-icon="mdi-view-dashboard"
          :title="t('nav.dashboard')"
          :active="route.path === '/'"
          rounded="xl"
        />

        <template v-if="auth.isAdmin">
          <!-- Expanded sidebar: grouped admin menu -->
          <v-list-group v-if="!layout.sidebarCollapsed" value="admin">
            <template #activator="{ props: groupProps }">
              <v-list-item
                v-bind="groupProps"
                prepend-icon="mdi-shield-crown"
                :title="t('nav.admin')"
                rounded="xl"
              />
            </template>

            <v-list-item
              to="/users"
              prepend-icon="mdi-account-group"
              :title="t('nav.users')"
              :active="route.path === '/users'"
              rounded="xl"
            />
            <v-list-item
              to="/roles"
              prepend-icon="mdi-shield-account"
              :title="t('nav.roles')"
              :active="route.path === '/roles'"
              rounded="xl"
            />
            <v-list-item
              to="/permissions"
              prepend-icon="mdi-key"
              :title="t('nav.permissions')"
              :active="route.path === '/permissions'"
              rounded="xl"
            />
            <v-list-item
              to="/audit"
              prepend-icon="mdi-clipboard-text-clock"
              :title="t('nav.audit')"
              :active="route.path === '/audit'"
              rounded="xl"
            />
          </v-list-group>

          <!-- Collapsed sidebar: flat icons with admin arrow divider -->
          <template v-else>
            <v-list-item
              prepend-icon="mdi-shield-crown"
              density="compact"
              rounded="xl"
              :class="['mt-2', { 'rail-admin-parent': adminRailExpanded }]"
              @click="adminRailExpanded = !adminRailExpanded"
            >
              <template #append>
                <v-icon
                  icon="mdi-chevron-down"
                  size="x-small"
                  :class="['rail-arrow', { 'rail-arrow--open': adminRailExpanded }]"
                />
              </template>
            </v-list-item>

            <v-expand-transition>
              <div v-show="adminRailExpanded" class="rail-admin-items">
                <v-list-item
                  to="/users"
                  prepend-icon="mdi-account-group"
                  :active="route.path === '/users'"
                  rounded="xl"
                  class="rail-admin-item"
                />
                <v-list-item
                  to="/roles"
                  prepend-icon="mdi-shield-account"
                  :active="route.path === '/roles'"
                  rounded="xl"
                  class="rail-admin-item"
                />
                <v-list-item
                  to="/permissions"
                  prepend-icon="mdi-key"
                  :active="route.path === '/permissions'"
                  rounded="xl"
                  class="rail-admin-item"
                />
                <v-list-item
                  to="/audit"
                  prepend-icon="mdi-clipboard-text-clock"
                  :active="route.path === '/audit'"
                  rounded="xl"
                  class="rail-admin-item"
                />
              </div>
            </v-expand-transition>
          </template>
        </template>

        <!-- Customers section -->
        <v-list-group v-if="!layout.sidebarCollapsed" value="customers">
          <template #activator="{ props: groupProps }">
            <v-list-item
              v-bind="groupProps"
              prepend-icon="mdi-account-multiple"
              :title="t('nav.customers')"
              rounded="xl"
            />
          </template>

          <v-list-item
            to="/customers"
            prepend-icon="mdi-account-group"
            :title="t('nav.customerList')"
            :active="route.path === '/customers'"
            rounded="xl"
          />
          <v-list-item
            to="/customer-categories"
            prepend-icon="mdi-tag-multiple"
            :title="t('nav.categories')"
            :active="route.path === '/customer-categories'"
            rounded="xl"
          />
        </v-list-group>

        <template v-else>
          <v-list-item
            prepend-icon="mdi-account-multiple"
            density="compact"
            rounded="xl"
            :class="['mt-2', { 'rail-admin-parent': customerRailExpanded }]"
            @click="customerRailExpanded = !customerRailExpanded"
          >
            <template #append>
              <v-icon
                icon="mdi-chevron-down"
                size="x-small"
                :class="['rail-arrow', { 'rail-arrow--open': customerRailExpanded }]"
              />
            </template>
          </v-list-item>

          <v-expand-transition>
            <div v-show="customerRailExpanded" class="rail-admin-items">
              <v-list-item
                to="/customers"
                prepend-icon="mdi-account-group"
                :active="route.path === '/customers'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/customer-categories"
                prepend-icon="mdi-tag-multiple"
                :active="route.path === '/customer-categories'"
                rounded="xl"
                class="rail-admin-item"
              />
            </div>
          </v-expand-transition>
        </template>

        <!-- Inventory — Products section -->
        <v-list-group v-if="!layout.sidebarCollapsed" value="inventory-products">
          <template #activator="{ props: groupProps }">
            <v-list-item
              v-bind="groupProps"
              prepend-icon="mdi-package-variant-closed"
              :title="t('nav.products')"
              rounded="xl"
            />
          </template>

          <v-list-item
            to="/products"
            prepend-icon="mdi-package-variant-closed"
            :title="t('nav.products')"
            :active="route.path === '/products'"
            rounded="xl"
          />
          <v-list-item
            to="/product-categories"
            prepend-icon="mdi-tag-multiple"
            :title="t('nav.productCategories')"
            :active="route.path === '/product-categories'"
            rounded="xl"
          />
          <v-list-item
            to="/units-of-measure"
            prepend-icon="mdi-ruler"
            :title="t('nav.unitsOfMeasure')"
            :active="route.path === '/units-of-measure'"
            rounded="xl"
          />
        </v-list-group>

        <template v-else>
          <v-list-item
            prepend-icon="mdi-package-variant-closed"
            density="compact"
            rounded="xl"
            :class="['mt-2', { 'rail-admin-parent': productsRailExpanded }]"
            @click="productsRailExpanded = !productsRailExpanded"
          >
            <template #append>
              <v-icon
                icon="mdi-chevron-down"
                size="x-small"
                :class="['rail-arrow', { 'rail-arrow--open': productsRailExpanded }]"
              />
            </template>
          </v-list-item>

          <v-expand-transition>
            <div v-show="productsRailExpanded" class="rail-admin-items">
              <v-list-item
                to="/products"
                prepend-icon="mdi-package-variant-closed"
                :active="route.path === '/products'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/product-categories"
                prepend-icon="mdi-tag-multiple"
                :active="route.path === '/product-categories'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/units-of-measure"
                prepend-icon="mdi-ruler"
                :active="route.path === '/units-of-measure'"
                rounded="xl"
                class="rail-admin-item"
              />
            </div>
          </v-expand-transition>
        </template>

        <!-- Inventory — Warehouse section -->
        <v-list-group v-if="!layout.sidebarCollapsed" value="inventory-warehouse">
          <template #activator="{ props: groupProps }">
            <v-list-item
              v-bind="groupProps"
              prepend-icon="mdi-warehouse"
              :title="t('nav.warehouseManagement')"
              rounded="xl"
            />
          </template>

          <v-list-item
            to="/warehouses"
            prepend-icon="mdi-warehouse"
            :title="t('nav.warehouses')"
            :active="route.path === '/warehouses'"
            rounded="xl"
          />
          <v-list-item
            to="/stock-levels"
            prepend-icon="mdi-package-variant"
            :title="t('nav.stockLevels')"
            :active="route.path === '/stock-levels'"
            rounded="xl"
          />
          <v-list-item
            to="/stock-movements"
            prepend-icon="mdi-swap-horizontal"
            :title="t('nav.stockMovements')"
            :active="route.path === '/stock-movements'"
            rounded="xl"
          />
          <v-list-item
            to="/batches"
            prepend-icon="mdi-barcode"
            :title="t('nav.batches')"
            :active="route.path === '/batches'"
            rounded="xl"
          />
        </v-list-group>

        <template v-else>
          <v-list-item
            prepend-icon="mdi-warehouse"
            density="compact"
            rounded="xl"
            :class="['mt-2', { 'rail-admin-parent': warehouseRailExpanded }]"
            @click="warehouseRailExpanded = !warehouseRailExpanded"
          >
            <template #append>
              <v-icon
                icon="mdi-chevron-down"
                size="x-small"
                :class="['rail-arrow', { 'rail-arrow--open': warehouseRailExpanded }]"
              />
            </template>
          </v-list-item>

          <v-expand-transition>
            <div v-show="warehouseRailExpanded" class="rail-admin-items">
              <v-list-item
                to="/warehouses"
                prepend-icon="mdi-warehouse"
                :active="route.path === '/warehouses'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/stock-levels"
                prepend-icon="mdi-package-variant"
                :active="route.path === '/stock-levels'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/stock-movements"
                prepend-icon="mdi-swap-horizontal"
                :active="route.path === '/stock-movements'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/batches"
                prepend-icon="mdi-barcode"
                :active="route.path === '/batches'"
                rounded="xl"
                class="rail-admin-item"
              />
            </div>
          </v-expand-transition>
        </template>

        <!-- Purchasing section -->
        <v-list-group v-if="!layout.sidebarCollapsed" value="purchasing">
          <template #activator="{ props: groupProps }">
            <v-list-item
              v-bind="groupProps"
              prepend-icon="mdi-cart"
              :title="t('nav.purchasing')"
              rounded="xl"
            />
          </template>

          <v-list-item
            to="/purchasing/purchase-orders"
            prepend-icon="mdi-file-document-outline"
            :title="t('nav.purchaseOrders')"
            :active="route.path === '/purchasing/purchase-orders'"
            rounded="xl"
          />
          <v-list-item
            to="/purchasing/suppliers"
            prepend-icon="mdi-domain"
            :title="t('nav.suppliers')"
            :active="route.path === '/purchasing/suppliers'"
            rounded="xl"
          />
          <v-list-item
            to="/purchasing/supplier-categories"
            prepend-icon="mdi-tag-multiple-outline"
            :title="t('nav.supplierCategories')"
            :active="route.path === '/purchasing/supplier-categories'"
            rounded="xl"
          />
          <v-list-item
            to="/purchasing/goods-receipts"
            prepend-icon="mdi-package-down"
            :title="t('nav.goodsReceipts')"
            :active="route.path === '/purchasing/goods-receipts'"
            rounded="xl"
          />
          <v-list-item
            to="/purchasing/supplier-returns"
            prepend-icon="mdi-package-up"
            :title="t('nav.supplierReturns')"
            :active="route.path === '/purchasing/supplier-returns'"
            rounded="xl"
          />
          <v-list-item
            to="/purchasing/purchase-events"
            prepend-icon="mdi-history"
            :title="t('nav.purchaseEvents')"
            :active="route.path === '/purchasing/purchase-events'"
            rounded="xl"
          />
        </v-list-group>

        <template v-else>
          <v-list-item
            prepend-icon="mdi-cart"
            density="compact"
            rounded="xl"
            :class="['mt-2', { 'rail-admin-parent': purchasingRailExpanded }]"
            @click="purchasingRailExpanded = !purchasingRailExpanded"
          >
            <template #append>
              <v-icon
                icon="mdi-chevron-down"
                size="x-small"
                :class="['rail-arrow', { 'rail-arrow--open': purchasingRailExpanded }]"
              />
            </template>
          </v-list-item>

          <v-expand-transition>
            <div v-show="purchasingRailExpanded" class="rail-admin-items">
              <v-list-item
                to="/purchasing/purchase-orders"
                prepend-icon="mdi-file-document-outline"
                :active="route.path === '/purchasing/purchase-orders'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/purchasing/suppliers"
                prepend-icon="mdi-domain"
                :active="route.path === '/purchasing/suppliers'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/purchasing/supplier-categories"
                prepend-icon="mdi-tag-multiple-outline"
                :active="route.path === '/purchasing/supplier-categories'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/purchasing/goods-receipts"
                prepend-icon="mdi-package-down"
                :active="route.path === '/purchasing/goods-receipts'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/purchasing/supplier-returns"
                prepend-icon="mdi-package-up"
                :active="route.path === '/purchasing/supplier-returns'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/purchasing/purchase-events"
                prepend-icon="mdi-history"
                :active="route.path === '/purchasing/purchase-events'"
                rounded="xl"
                class="rail-admin-item"
              />
            </div>
          </v-expand-transition>
        </template>

        <!-- Inventory — Operations section -->
        <v-list-group v-if="!layout.sidebarCollapsed" value="inventory-operations">
          <template #activator="{ props: groupProps }">
            <v-list-item
              v-bind="groupProps"
              prepend-icon="mdi-cog-transfer"
              :title="t('nav.operations')"
              rounded="xl"
            />
          </template>

          <v-list-item
            to="/adjustments"
            prepend-icon="mdi-pencil-ruler"
            :title="t('nav.adjustments')"
            :active="route.path === '/adjustments'"
            rounded="xl"
          />
          <v-list-item
            to="/transfers"
            prepend-icon="mdi-truck"
            :title="t('nav.transfers')"
            :active="route.path === '/transfers'"
            rounded="xl"
          />
          <v-list-item
            to="/stocktake"
            prepend-icon="mdi-clipboard-check"
            :title="t('nav.stocktake')"
            :active="route.path === '/stocktake'"
            rounded="xl"
          />
        </v-list-group>

        <template v-else>
          <v-list-item
            prepend-icon="mdi-cog-transfer"
            density="compact"
            rounded="xl"
            :class="['mt-2', { 'rail-admin-parent': operationsRailExpanded }]"
            @click="operationsRailExpanded = !operationsRailExpanded"
          >
            <template #append>
              <v-icon
                icon="mdi-chevron-down"
                size="x-small"
                :class="['rail-arrow', { 'rail-arrow--open': operationsRailExpanded }]"
              />
            </template>
          </v-list-item>

          <v-expand-transition>
            <div v-show="operationsRailExpanded" class="rail-admin-items">
              <v-list-item
                to="/adjustments"
                prepend-icon="mdi-pencil-ruler"
                :active="route.path === '/adjustments'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/transfers"
                prepend-icon="mdi-truck"
                :active="route.path === '/transfers'"
                rounded="xl"
                class="rail-admin-item"
              />
              <v-list-item
                to="/stocktake"
                prepend-icon="mdi-clipboard-check"
                :active="route.path === '/stocktake'"
                rounded="xl"
                class="rail-admin-item"
              />
            </div>
          </v-expand-transition>
        </template>
      </v-list>
    </v-navigation-drawer>

    <v-app-bar color="surface" elevation="0" border="b">
      <v-app-bar-nav-icon @click="layout.toggleSidebar()" />

      <v-icon v-if="pageIcon" :icon="pageIcon" class="mr-2" color="primary" />
      <v-toolbar-title class="font-weight-bold">{{ pageTitle }}</v-toolbar-title>

      <v-spacer />

      <v-menu open-on-hover :close-on-content-click="false" :open-delay="0" :close-delay="200">
        <template #activator="{ props: menuProps }">
          <v-btn v-bind="menuProps" variant="text" class="text-none">
            <v-avatar size="32" color="primary" class="mr-2">
              <span class="text-body-2 font-weight-medium text-white">
                {{ auth.username?.charAt(0).toUpperCase() }}
              </span>
            </v-avatar>
            {{ auth.username }}
            <v-icon end icon="mdi-chevron-down" size="small" />
          </v-btn>
        </template>

        <v-card min-width="260">
          <v-card-text class="pb-0">
            <div class="d-flex align-center mb-2">
              <v-avatar size="40" color="primary" class="mr-3">
                <span class="text-body-1 font-weight-medium text-white">
                  {{ auth.username?.charAt(0).toUpperCase() }}
                </span>
              </v-avatar>
              <div>
                <div class="text-subtitle-2 font-weight-medium">{{ auth.username }}</div>
                <div class="text-caption text-medium-emphasis">{{ t('app.title') }}</div>
              </div>
            </div>
          </v-card-text>

          <v-divider />

          <v-list density="compact" nav>
            <v-list-subheader>{{ t('app.userMenu.language') }}</v-list-subheader>
            <v-list-item
              title="English"
              prepend-icon="mdi-alpha-e-box"
              :active="currentLocale === 'en'"
              @click="switchLocale('en')"
            />
            <v-list-item
              title="Български"
              prepend-icon="mdi-alpha-b-box"
              :active="currentLocale === 'bg'"
              @click="switchLocale('bg')"
            />

            <v-divider class="my-1" />

            <v-list-subheader>{{ t('app.userMenu.display') }}</v-list-subheader>
            <v-list-item
              :title="layout.isCompact ? t('app.density.comfortable') : t('app.density.compact')"
              :prepend-icon="layout.isCompact ? 'mdi-arrow-expand-vertical' : 'mdi-arrow-collapse-vertical'"
              @click="layout.toggleDensity()"
            />

            <v-divider class="my-1" />

            <v-list-subheader>{{ t('app.userMenu.formDisplay') }}</v-list-subheader>
            <v-list-item
              :title="t('app.userMenu.formModal')"
              prepend-icon="mdi-card-outline"
              :active="layout.formDisplayMode === 'modal'"
              @click="layout.setFormDisplayMode('modal')"
            />
            <v-list-item
              :title="t('app.userMenu.formPage')"
              prepend-icon="mdi-page-layout-body"
              :active="layout.formDisplayMode === 'page'"
              @click="layout.setFormDisplayMode('page')"
            />

            <v-divider class="my-1" />

            <v-list-item
              :title="t('settings.title')"
              prepend-icon="mdi-cog"
              @click="router.push({ name: 'settings' })"
            />

            <v-divider class="my-1" />

            <v-list-item
              :title="t('app.logout')"
              prepend-icon="mdi-logout"
              base-color="error"
              @click="handleLogout"
            />
          </v-list>
        </v-card>
      </v-menu>
    </v-app-bar>

    <v-main :class="isFormPage ? 'main-form-page' : (layout.isCompact ? 'main-compact' : 'main-comfortable')">
      <v-container v-if="!isFormPage" :fluid="layout.isCompact">
        <router-view />
      </v-container>
      <router-view v-else />
    </v-main>

    <ToastNotification />
  </v-app>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@shared/stores/auth';
import { useLayoutStore } from '@shared/stores/layout';
import ToastNotification from '@shared/components/molecules/ToastNotification.vue';

const { t, locale } = useI18n();
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const layout = useLayoutStore();
const drawerOpen = ref(true);
const adminRailExpanded = ref(true);
const customerRailExpanded = ref(true);
const productsRailExpanded = ref(true);
const warehouseRailExpanded = ref(true);
const purchasingRailExpanded = ref(true);
const operationsRailExpanded = ref(true);

const currentLocale = computed(() => locale.value);

const formPageRoutes = ['user-create', 'user-edit', 'user-password', 'user-roles', 'role-create', 'role-edit', 'role-permissions', 'permission-create', 'customer-create', 'customer-edit', 'category-create', 'category-edit', 'settings-edit-profile', 'settings-change-password', 'product-create', 'product-edit', 'product-category-create', 'product-category-edit', 'unit-create', 'unit-edit', 'warehouse-create', 'warehouse-edit', 'batch-create', 'batch-edit', 'adjustment-create', 'transfer-create', 'stocktake-create', 'purchase-order-create', 'purchase-order-edit', 'supplier-create', 'supplier-edit', 'supplier-category-create', 'supplier-category-edit', 'goods-receipt-create', 'supplier-return-create'];
const isFormPage = computed(() => formPageRoutes.includes(route.name as string));

const pageTitle = computed(() => {
  const titleKey = route.meta.titleKey as string | undefined;
  return titleKey ? t(titleKey) : t('app.title');
});

const pageIcon = computed(() => {
  return (route.meta.icon as string | undefined) || '';
});

function switchLocale(lang: string): void {
  locale.value = lang;
  localStorage.setItem('locale', lang);
}

async function handleLogout(): Promise<void> {
  await auth.logout();
  router.push({ name: 'login' });
}
</script>

<style scoped>
.main-compact,
.main-comfortable {
  overflow: hidden !important;
}

.main-compact :deep(.v-main__wrap),
.main-comfortable :deep(.v-main__wrap) {
  overflow: hidden !important;
}

.main-compact :deep(.v-container),
.main-comfortable :deep(.v-container) {
  overflow-y: auto !important;
  overflow-x: hidden !important;
  max-height: calc(100vh - 64px);
}

:deep(.v-application) {
  overflow: hidden !important;
}

.main-form-page {
  display: flex;
  flex-direction: column;
  overflow: hidden !important;
}

.main-form-page :deep(.v-main__wrap) {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar-nav {
  overflow-y: auto;
  flex: 1 1 auto;
}

.sidebar-nav :deep(.v-list-item--active) {
  background: rgba(99, 102, 241, 0.2) !important;
}

.rail-arrow {
  transition: transform 0.3s ease;
}

.rail-arrow--open {
  transform: rotate(180deg);
}

.rail-admin-items {
  padding-bottom: 2px;
}

.rail-admin-parent {
  background: rgba(99, 102, 241, 0.2) !important;
}

.rail-admin-items {
  background: rgba(99, 102, 241, 0.3);
  border-radius: 12px;
  margin-top: 4px;
}
</style>

<style>
html,
body,
.v-application {
  overflow: hidden !important;
  height: 100vh;
}

.view-list-card .v-table__wrapper {
  max-height: calc(100vh - 200px);
  overflow-y: auto;
}
</style>
