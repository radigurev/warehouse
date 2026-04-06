<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('permissions.create') }}
      </v-btn>
    </div>

    <v-card class="view-list-card">
      <v-data-table-server
        :headers="vm.headers"
        :items="vm.filteredItems"
        :items-length="vm.totalCount"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :page="vm.page"
        :items-per-page="vm.pageSize"
        :items-per-page-options="[10, 25, 50, 100]"
        fixed-header
        hover
        @update:page="vm.handlePageChange"
        @update:items-per-page="vm.handlePageSizeChange"
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

        <template #tfoot>
          <tr>
            <td :colspan="vm.headers.length" class="text-center text-caption text-medium-emphasis py-1">
              {{ vm.t('common.pageInfo', { page: vm.page, pages: vm.totalPages, total: vm.totalCount }) }}
            </td>
          </tr>
        </template>
      </v-data-table-server>
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
