<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('salesOrders.create') }}
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
        <template #header.orderNumber="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.orderNumber" column-key="orderNumber" />
          </div>
        </template>

        <template #header.customerName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.customerName" column-key="customerName" />
          </div>
        </template>

        <template #header.status="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.status" column-key="status" />
          </div>
        </template>

        <template #item.status="{ item }">
          <v-chip :color="soStatusColor(item.status)" size="small" label>
            {{ vm.t(`salesOrders.status.${item.status}`) }}
          </v-chip>
        </template>

        <template #item.requestedShipDate="{ item }">
          {{ item.requestedShipDate ? vm.formatDate(item.requestedShipDate) : '\u2014' }}
        </template>

        <template #item.totalAmount="{ item }">
          {{ item.totalAmount.toFixed(2) }}
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('salesOrders.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip v-if="item.status === 'Draft'" :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip v-if="item.status === 'Draft'" :label="vm.t('salesOrders.confirm')" icon="mdi-check" color="success" :compact="vm.layout.isCompact" @click="vm.openConfirmDialog(item)" />
          <ActionChip v-if="item.status === 'Draft' || item.status === 'Confirmed'" :label="vm.t('salesOrders.cancel')" icon="mdi-close" color="error" :compact="vm.layout.isCompact" @click="vm.openCancelDialog(item)" />
          <ActionChip v-if="item.status === 'Shipped'" :label="vm.t('salesOrders.complete')" icon="mdi-check-all" color="green" :compact="vm.layout.isCompact" @click="vm.openCompleteDialog(item)" />
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

    <ConfirmDialog
      v-model="vm.showConfirmDialog"
      :title="vm.t('salesOrders.confirm')"
      :message="vm.t('salesOrders.confirmMessage', { orderNumber: vm.selectedOrder?.orderNumber })"
      :confirm-text="vm.t('salesOrders.confirm')"
      color="success"
      icon="mdi-check"
      :loading="vm.confirming"
      @confirm="vm.handleConfirm"
    />

    <ConfirmDialog
      v-model="vm.showCancelDialog"
      :title="vm.t('salesOrders.cancel')"
      :message="vm.t('salesOrders.cancelMessage', { orderNumber: vm.selectedOrder?.orderNumber })"
      :confirm-text="vm.t('salesOrders.cancel')"
      color="error"
      icon="mdi-close"
      :loading="vm.cancelling"
      @confirm="vm.handleCancel"
    />

    <ConfirmDialog
      v-model="vm.showCompleteDialog"
      :title="vm.t('salesOrders.complete')"
      :message="vm.t('salesOrders.completeMessage', { orderNumber: vm.selectedOrder?.orderNumber })"
      :confirm-text="vm.t('salesOrders.complete')"
      color="green"
      icon="mdi-check-all"
      :loading="vm.completing"
      @confirm="vm.handleComplete"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useSalesOrdersView } from '@features/fulfillment/composables/useSalesOrdersView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const vm = reactive(useSalesOrdersView());

function soStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    Picking: 'amber',
    Packed: 'indigo',
    Shipped: 'teal',
    Completed: 'green',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}
</script>
