<template>
  <div>
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
        <template #header.shipmentNumber="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.shipmentNumber" column-key="shipmentNumber" />
          </div>
        </template>

        <template #header.salesOrderNumber="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.salesOrderNumber" column-key="salesOrderNumber" />
          </div>
        </template>

        <template #header.status="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.status" column-key="status" />
          </div>
        </template>

        <template #item.status="{ item }">
          <v-chip :color="shipmentStatusColor(item.status)" size="small" label>
            {{ vm.t(`shipments.status.${item.status}`) }}
          </v-chip>
        </template>

        <template #item.dispatchedAtUtc="{ item }">
          {{ vm.formatDate(item.dispatchedAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('shipments.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip v-if="!isTerminalStatus(item.status)" :label="vm.t('shipments.updateStatus')" icon="mdi-update" color="primary" :compact="vm.layout.isCompact" @click="vm.openStatusDialog(item)" />
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

    <ShipmentStatusDialog
      v-model="vm.showStatusDialog"
      :current-status="vm.selectedShipment?.status ?? 'Dispatched'"
      @updated="vm.handleStatusUpdated"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useShipmentsView } from '@features/fulfillment/composables/useShipmentsView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ShipmentStatusDialog from '@features/fulfillment/components/organisms/ShipmentStatusDialog.vue';

const vm = reactive(useShipmentsView());

function shipmentStatusColor(status: string): string {
  const map: Record<string, string> = {
    Dispatched: 'blue',
    InTransit: 'teal',
    OutForDelivery: 'amber',
    Delivered: 'green',
    Failed: 'red',
    Returned: 'orange',
  };
  return map[status] || 'grey';
}

function isTerminalStatus(status: string): boolean {
  return status === 'Delivered' || status === 'Returned';
}
</script>
