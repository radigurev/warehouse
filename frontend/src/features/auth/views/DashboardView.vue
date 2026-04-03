<template>
  <div>
    <v-alert type="info" variant="tonal" class="mb-6" icon="mdi-hand-wave">
      {{ vm.t('dashboard.welcome', { name: vm.auth.username }) }}
    </v-alert>

    <v-row>
      <v-col cols="12" md="4">
        <v-card>
          <v-card-item>
            <template #prepend>
              <v-icon icon="mdi-account-group" size="40" color="primary" />
            </template>
            <v-card-title>{{ vm.t('dashboard.totalUsers') }}</v-card-title>
            <v-card-subtitle class="text-h4">{{ vm.stats.users }}</v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>

      <v-col cols="12" md="4">
        <v-card>
          <v-card-item>
            <template #prepend>
              <v-icon icon="mdi-shield-account" size="40" color="secondary" />
            </template>
            <v-card-title>{{ vm.t('dashboard.totalRoles') }}</v-card-title>
            <v-card-subtitle class="text-h4">{{ vm.stats.roles }}</v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>

      <v-col cols="12" md="4">
        <v-card>
          <v-card-item>
            <template #prepend>
              <v-icon icon="mdi-key" size="40" color="accent" />
            </template>
            <v-card-title>{{ vm.t('dashboard.totalPermissions') }}</v-card-title>
            <v-card-subtitle class="text-h4">{{ vm.stats.permissions }}</v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useDashboardView } from '@features/auth/composables/useDashboardView';

const vm = reactive(useDashboardView());
</script>
