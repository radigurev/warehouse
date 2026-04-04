<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('categories.create') }}
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
        <template #header.name="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.name" column-key="name" />
          </div>
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip :label="vm.t('common.delete')" icon="mdi-delete" color="error" :compact="vm.layout.isCompact" @click="vm.openDeleteDialog(item)" />
        </template>
      </v-data-table>
    </v-card>

    <CategoryFormDialog v-model="vm.showFormDialog" :category="vm.selectedCategory" @saved="vm.loadCategories" />
    <ConfirmDialog
      v-model="vm.showDeleteDialog"
      :title="vm.t('categories.delete')"
      :message="vm.t('categories.deleteConfirm', { name: vm.selectedCategory?.name })"
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
import { useCategoriesView } from '@features/customers/composables/useCategoriesView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import CategoryFormDialog from '@features/customers/components/organisms/CategoryFormDialog.vue';

const vm = reactive(useCategoriesView());
</script>
