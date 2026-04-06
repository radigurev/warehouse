<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('warehouses.create') }}
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
        <template #header.name="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.name" column-key="name" />
          </div>
        </template>

        <template #header.code="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.code" column-key="code" />
          </div>
        </template>

        <template #header.address="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.address" column-key="address" />
          </div>
        </template>

        <template #item.isActive="{ item }">
          <StatusChip :active="item.isActive" />
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('warehouses.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip v-if="item.isActive" :label="vm.t('warehouses.deactivate')" icon="mdi-warehouse" color="error" :compact="vm.layout.isCompact" @click="vm.openDeactivateDialog(item)" />
          <ActionChip v-else :label="vm.t('warehouses.reactivate')" icon="mdi-warehouse" color="success" :compact="vm.layout.isCompact" @click="vm.handleReactivate(item)" />
        </template>
      </v-data-table>
    </v-card>

    <WarehouseFormDialog v-model="vm.showFormDialog" :warehouse="vm.selectedWarehouse" @saved="vm.loadWarehouses" />
    <ConfirmDialog
      v-model="vm.showDeactivateDialog"
      :title="vm.t('warehouses.deactivate')"
      :message="vm.t('warehouses.deactivateConfirm', { name: vm.selectedWarehouse?.name })"
      :confirm-text="vm.t('warehouses.deactivate')"
      color="error"
      icon="mdi-warehouse"
      :loading="vm.deactivating"
      @confirm="vm.handleDeactivate"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useWarehousesView } from '@features/inventory/composables/useWarehousesView';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import WarehouseFormDialog from '@features/inventory/components/organisms/WarehouseFormDialog.vue';

const vm = reactive(useWarehousesView());
</script>
