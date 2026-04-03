<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('permissions.create') }}
      </v-btn>
    </div>

    <v-card>
      <v-data-table
        :headers="vm.headers"
        :items="vm.filteredItems"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :items-per-page="25"
        hover
      >
        <template #header.resource="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.resource" column-key="resource" />
          </div>
        </template>

        <template #header.action="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.action" column-key="action" />
          </div>
        </template>
      </v-data-table>
    </v-card>

    <PermissionFormDialog v-model="vm.showFormDialog" @saved="vm.loadPermissions" />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { usePermissionsView } from '@features/auth/composables/usePermissionsView';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import PermissionFormDialog from '@features/auth/components/organisms/PermissionFormDialog.vue';

const vm = reactive(usePermissionsView());
</script>
