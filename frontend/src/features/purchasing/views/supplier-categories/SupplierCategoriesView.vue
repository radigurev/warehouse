<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('supplierCategories.create') }}
      </v-btn>
    </div>

    <v-card class="view-list-card">
      <v-data-table-server
        :headers="vm.headers"
        :items="vm.filteredItems"
        :items-length="vm.filteredItems.length"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :items-per-page="-1"
        fixed-header
        hover
        hide-default-footer
      >
        <template #header.name="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.name" column-key="name" />
          </div>
        </template>

        <template #item.description="{ item }">
          {{ item.description || '\u2014' }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip :label="vm.t('common.delete')" icon="mdi-delete" color="error" :compact="vm.layout.isCompact" @click="vm.openDeleteDialog(item)" />
        </template>
      </v-data-table-server>
    </v-card>

    <SupplierCategoryFormDialog v-model="vm.showFormDialog" :category="vm.selectedCategory" @saved="vm.loadCategories" />

    <ConfirmDialog
      v-model="vm.showDeleteDialog"
      :title="vm.t('supplierCategories.delete')"
      :message="vm.t('supplierCategories.deleteConfirm', { name: vm.selectedCategory?.name })"
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
import { useSupplierCategoriesView } from '@features/purchasing/composables/useSupplierCategoriesView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import SupplierCategoryFormDialog from '@features/purchasing/components/organisms/SupplierCategoryFormDialog.vue';

const vm = reactive(useSupplierCategoriesView());
</script>
