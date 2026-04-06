<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('roles.create') }}
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
        <template #header.name="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.name" column-key="name" />
          </div>
        </template>

        <template #item.isSystem="{ item }">
          <v-chip v-if="item.isSystem" color="warning" size="small" variant="flat">
            {{ vm.t('roles.system') }}
          </v-chip>
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip :label="vm.t('roles.managePermissions')" icon="mdi-key" color="accent" :compact="vm.layout.isCompact" @click="vm.handlePermissions(item)" />
          <ActionChip v-if="!item.isSystem" :label="vm.t('common.delete')" icon="mdi-delete" color="error" :compact="vm.layout.isCompact" @click="vm.openDeleteDialog(item)" />
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

    <RoleFormDialog v-model="vm.showFormDialog" :role="vm.selectedRole" @saved="vm.loadRoles" />
    <RolePermissionsDialog v-model="vm.showPermissionsDialog" :role-id="vm.selectedRole?.id ?? 0" :role-name="vm.selectedRole?.name ?? ''" />
    <ConfirmDialog
      v-model="vm.showDeleteDialog"
      :title="vm.t('roles.delete')"
      :message="vm.t('roles.deleteConfirm', { name: vm.selectedRole?.name })"
      :confirm-text="vm.t('common.delete')"
      color="error"
      icon="mdi-delete"
      :loading="vm.deleting"
      @confirm="vm.handleDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useRolesView } from '@features/auth/composables/useRolesView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import RoleFormDialog from '@features/auth/components/organisms/RoleFormDialog.vue';
import RolePermissionsDialog from '@features/auth/components/organisms/RolePermissionsDialog.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const vm = reactive(useRolesView());
</script>
