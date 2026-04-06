<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('unitsOfMeasure.create') }}
      </v-btn>
    </div>

    <v-card class="view-list-card">
      <v-data-table
        :headers="vm.headers"
        :items="vm.filteredItems"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :items-per-page="-1"
        fixed-header
        hover
      >
        <template #header.code="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.code" column-key="code" />
          </div>
        </template>

        <template #header.name="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.name" column-key="name" />
          </div>
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip :label="vm.t('unitsOfMeasure.delete')" icon="mdi-delete" color="error" :compact="vm.layout.isCompact" @click="vm.openDeleteDialog(item)" />
        </template>
      </v-data-table>
    </v-card>

    <UnitOfMeasureFormDialog v-model="vm.showFormDialog" :unit="vm.selectedUnit" @saved="vm.loadUnits" />
    <ConfirmDialog
      v-model="vm.showDeleteDialog"
      :title="vm.t('unitsOfMeasure.delete')"
      :message="vm.t('unitsOfMeasure.deleteConfirm', { name: vm.selectedUnit?.name })"
      :confirm-text="vm.t('unitsOfMeasure.delete')"
      color="error"
      icon="mdi-delete"
      :loading="vm.deleting"
      @confirm="vm.handleDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useUnitsOfMeasureView } from '@features/inventory/composables/useUnitsOfMeasureView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import UnitOfMeasureFormDialog from '@features/inventory/components/organisms/UnitOfMeasureFormDialog.vue';

const vm = reactive(useUnitsOfMeasureView());
</script>
