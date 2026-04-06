<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleRecordMovement">
        {{ vm.t('stockMovements.record') }}
      </v-btn>
    </div>

    <v-card class="view-list-card">
      <v-data-table-server
        :headers="vm.headers"
        :items="vm.filteredItems"
        :items-length="vm.totalCount"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :page="vm.searchParams.page"
        :items-per-page="vm.searchParams.pageSize"
        :items-per-page-options="[10, 25, 50, 100]"
        fixed-header
        hover
        @update:page="vm.handlePageChange"
        @update:items-per-page="vm.handlePageSizeChange"
      >
        <template #header.productName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.productName" column-key="productName" />
          </div>
        </template>

        <template #header.warehouseName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.warehouseName" column-key="warehouseName" />
          </div>
        </template>

        <template #header.reasonCode="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.reasonCode" column-key="reasonCode" />
          </div>
        </template>

        <template #item.reasonCode="{ item }">
          {{ vm.translateReason(item.reasonCode) }}
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #tfoot>
          <tr>
            <td :colspan="vm.headers.length" class="text-center text-caption text-medium-emphasis py-1">
              {{ vm.t('common.pageInfo', { page: vm.searchParams.page, pages: vm.totalPages, total: vm.totalCount }) }}
            </td>
          </tr>
        </template>
      </v-data-table-server>
    </v-card>

    <StockMovementFormDialog v-model="vm.showFormDialog" @saved="onMovementSaved" />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useStockMovementsView } from '@features/inventory/composables/useStockMovementsView';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import StockMovementFormDialog from '@features/inventory/components/organisms/StockMovementFormDialog.vue';
import type { RecordStockMovementRequest } from '@features/inventory/types/inventory';

const vm = reactive(useStockMovementsView());

async function onMovementSaved(request: RecordStockMovementRequest): Promise<void> {
  await vm.submitMovement(request);
}
</script>
