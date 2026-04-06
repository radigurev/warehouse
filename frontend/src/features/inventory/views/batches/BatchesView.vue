<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('batches.create') }}
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
        <template #header.batchNumber="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.batchNumber" column-key="batchNumber" />
          </div>
        </template>

        <template #header.productName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.productName" column-key="productName" />
          </div>
        </template>

        <template #item.expiryDate="{ item }">
          {{ vm.formatDate(item.expiryDate) }}
        </template>

        <template #item.isActive="{ item }">
          <StatusChip :active="item.isActive" />
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip v-if="item.isActive" :label="vm.t('batches.deactivate')" icon="mdi-close-circle" color="error" :compact="vm.layout.isCompact" @click="vm.openDeactivateDialog(item)" />
        </template>
      </v-data-table>
    </v-card>

    <BatchFormDialog v-model="vm.showFormDialog" :batch="vm.selectedBatch" @saved="vm.loadBatches" />
    <ConfirmDialog
      v-model="vm.showDeactivateDialog"
      :title="vm.t('batches.deactivate')"
      :message="vm.t('batches.deactivateConfirm', { name: vm.selectedBatch?.batchNumber })"
      :confirm-text="vm.t('batches.deactivate')"
      color="error"
      icon="mdi-close-circle"
      :loading="vm.deactivating"
      @confirm="vm.handleDeactivate"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useBatchesView } from '@features/inventory/composables/useBatchesView';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import BatchFormDialog from '@features/inventory/components/organisms/BatchFormDialog.vue';

const vm = reactive(useBatchesView());
</script>
