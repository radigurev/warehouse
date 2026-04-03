<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="handleCreate">
        {{ t('permissions.create') }}
      </v-btn>
    </div>

    <v-card>
      <v-data-table
        :headers="headers"
        :items="filteredItems"
        :loading="loading"
        :density="layout.vuetifyDensity"
        :items-per-page="25"
        hover
      >
        <template #header.resource="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.resource" column-key="resource" />
          </div>
        </template>

        <template #header.action="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.action" column-key="action" />
          </div>
        </template>
      </v-data-table>
    </v-card>

    <PermissionFormDialog v-model="showFormDialog" @saved="loadPermissions" />
  </div>
</template>

<script setup lang="ts">
import { usePermissionsView } from '@features/auth/composables/usePermissionsView';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import PermissionFormDialog from '@features/auth/components/organisms/PermissionFormDialog.vue';

const {
  t,
  layout,
  loading,
  showFormDialog,
  columnFilters,
  filteredItems,
  headers,
  loadPermissions,
  handleCreate,
} = usePermissionsView();
</script>
