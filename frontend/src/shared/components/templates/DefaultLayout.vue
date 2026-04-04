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

const currentLocale = computed(() => locale.value);

const formPageRoutes = ['user-create', 'user-edit', 'user-password', 'user-roles', 'role-create', 'role-edit', 'role-permissions', 'permission-create', 'customer-create', 'customer-edit', 'category-create', 'category-edit', 'settings-edit-profile', 'settings-change-password'];
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
.main-compact {
  height: 100vh;
  overflow: hidden;
}

.main-comfortable {
  overflow-y: auto;
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
