<template>
  <v-app>
    <v-navigation-drawer
      v-model="drawerOpen"
      :rail="layout.sidebarCollapsed"
      permanent
      color="blue-darken-4"
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

      <v-list nav :density="layout.vuetifyDensity">
        <v-list-item
          v-for="item in navItems"
          :key="item.route"
          :to="item.route"
          :prepend-icon="item.icon"
          :title="item.title"
          :active="route.path === item.route"
          active-color="white"
        />
      </v-list>
    </v-navigation-drawer>

    <v-app-bar :density="layout.vuetifyDensity" color="blue-darken-3">
      <v-app-bar-nav-icon @click="layout.toggleSidebar()" />

      <v-toolbar-title>{{ t('app.title') }}</v-toolbar-title>

      <v-spacer />

      <v-btn
        :icon="layout.isCompact ? 'mdi-arrow-expand-vertical' : 'mdi-arrow-collapse-vertical'"
        :title="layout.isCompact ? t('app.density.comfortable') : t('app.density.compact')"
        variant="text"
        @click="layout.toggleDensity()"
      />

      <v-menu>
        <template #activator="{ props }">
          <v-btn
            v-bind="props"
            variant="text"
            :prepend-icon="currentLocale === 'en' ? 'mdi-alpha-e-box' : 'mdi-alpha-b-box'"
          >
            {{ currentLocale === 'en' ? 'EN' : 'BG' }}
          </v-btn>
        </template>
        <v-list>
          <v-list-item title="English" @click="switchLocale('en')" :active="currentLocale === 'en'" />
          <v-list-item title="Български" @click="switchLocale('bg')" :active="currentLocale === 'bg'" />
        </v-list>
      </v-menu>

      <v-chip class="mx-2" variant="text" prepend-icon="mdi-account">
        {{ auth.username }}
      </v-chip>

      <v-btn icon="mdi-logout" :title="t('app.logout')" variant="text" @click="handleLogout" />
    </v-app-bar>

    <v-main :class="layout.isCompact ? 'main-compact' : 'main-comfortable'">
      <v-container :fluid="layout.isCompact">
        <router-view />
      </v-container>
    </v-main>

    <v-snackbar
      v-model="notification.visible"
      :timeout="notification.timeout"
      :color="notification.type"
      location="bottom right"
    >
      {{ notification.message }}
      <template #actions>
        <v-btn variant="text" @click="notification.hide()">
          {{ t('common.close') }}
        </v-btn>
      </template>
    </v-snackbar>
  </v-app>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/auth';
import { useNotificationStore } from '@/stores/notification';
import { useLayoutStore } from '@/stores/layout';

const { t, locale } = useI18n();
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const notification = useNotificationStore();
const layout = useLayoutStore();
const drawerOpen = ref(true);

const currentLocale = computed(() => locale.value);

const navItems = computed(() => [
  { route: '/', icon: 'mdi-view-dashboard', title: t('nav.dashboard') },
  { route: '/users', icon: 'mdi-account-group', title: t('nav.users') },
  { route: '/roles', icon: 'mdi-shield-account', title: t('nav.roles') },
  { route: '/permissions', icon: 'mdi-key', title: t('nav.permissions') },
  { route: '/audit', icon: 'mdi-clipboard-text-clock', title: t('nav.audit') },
]);

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
</style>
