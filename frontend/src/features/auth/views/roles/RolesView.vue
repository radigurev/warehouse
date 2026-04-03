<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="handleCreate">
        {{ t('roles.create') }}
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
        <template #header.name="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.name" column-key="name" />
          </div>
        </template>

        <template #item.isSystem="{ item }">
          <v-chip v-if="item.isSystem" color="warning" size="small" variant="flat">
            {{ t('roles.system') }}
          </v-chip>
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="t('common.edit')" icon="mdi-pencil" color="primary" :compact="layout.isCompact" @click="handleEdit(item)" />
          <ActionChip :label="t('roles.managePermissions')" icon="mdi-key" color="accent" :compact="layout.isCompact" @click="handlePermissions(item)" />
          <ActionChip v-if="!item.isSystem" :label="t('common.delete')" icon="mdi-delete" color="error" :compact="layout.isCompact" @click="openDeleteDialog(item)" />
        </template>
      </v-data-table>
    </v-card>

    <RoleFormDialog v-model="showFormDialog" :role="selectedRole" @saved="loadRoles" />
    <RolePermissionsDialog v-model="showPermissionsDialog" :role-id="selectedRole?.id ?? 0" :role-name="selectedRole?.name ?? ''" />
    <ConfirmDialog
      v-model="showDeleteDialog"
      :title="t('roles.delete')"
      :message="t('roles.deleteConfirm', { name: selectedRole?.name })"
      :confirm-text="t('common.delete')"
      color="error"
      icon="mdi-delete"
      :loading="deleting"
      @confirm="handleDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { useRolesView } from '@features/auth/composables/useRolesView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import RoleFormDialog from '@features/auth/components/organisms/RoleFormDialog.vue';
import RolePermissionsDialog from '@features/auth/components/organisms/RolePermissionsDialog.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const {
  t,
  layout,
  loading,
  selectedRole,
  showFormDialog,
  showPermissionsDialog,
  showDeleteDialog,
  deleting,
  columnFilters,
  filteredItems,
  headers,
  loadRoles,
  handleCreate,
  handleEdit,
  handlePermissions,
  openDeleteDialog,
  handleDelete,
} = useRolesView();
</script>
