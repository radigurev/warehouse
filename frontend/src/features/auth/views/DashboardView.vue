<template>
  <div>
    <v-alert type="info" variant="tonal" class="mb-6" icon="mdi-hand-wave">
      {{ t('dashboard.welcome', { name: auth.username }) }}
    </v-alert>

    <v-row>
      <v-col cols="12" md="4">
        <v-card>
          <v-card-item>
            <template #prepend>
              <v-icon icon="mdi-account-group" size="40" color="primary" />
            </template>
            <v-card-title>{{ t('dashboard.totalUsers') }}</v-card-title>
            <v-card-subtitle class="text-h4">{{ stats.users }}</v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>

      <v-col cols="12" md="4">
        <v-card>
          <v-card-item>
            <template #prepend>
              <v-icon icon="mdi-shield-account" size="40" color="secondary" />
            </template>
            <v-card-title>{{ t('dashboard.totalRoles') }}</v-card-title>
            <v-card-subtitle class="text-h4">{{ stats.roles }}</v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>

      <v-col cols="12" md="4">
        <v-card>
          <v-card-item>
            <template #prepend>
              <v-icon icon="mdi-key" size="40" color="accent" />
            </template>
            <v-card-title>{{ t('dashboard.totalPermissions') }}</v-card-title>
            <v-card-subtitle class="text-h4">{{ stats.permissions }}</v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup lang="ts">
import { useDashboardView } from '@features/auth/composables/useDashboardView';

const { t, auth, stats } = useDashboardView();
</script>
