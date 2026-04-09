<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('purchaseOrders.create') }}
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

        <template #header.supplierName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.supplierName" column-key="supplierName" />
          </div>
        </template>

        <template #header.status="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.status" column-key="status" />
          </div>
        </template>

        <template #item.status="{ item }">
          <v-chip :color="poStatusColor(item.status)" size="small" label>
            {{ vm.t(`purchaseOrders.status.${item.status}`) }}
          </v-chip>
        </template>

        <template #item.expectedDeliveryDate="{ item }">
          {{ item.expectedDeliveryDate ? vm.formatDate(item.expectedDeliveryDate) : '\u2014' }}
        </template>

        <template #item.totalAmount="{ item }">
          {{ item.totalAmount.toFixed(2) }}
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('purchaseOrders.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip v-if="vm.canEdit(item)" :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip v-if="vm.canConfirm(item)" :label="vm.t('purchaseOrders.confirm')" icon="mdi-check" color="success" :compact="vm.layout.isCompact" @click="vm.openConfirmDialog(item)" />
          <ActionChip v-if="vm.canCancel(item)" :label="vm.t('purchaseOrders.cancel')" icon="mdi-close" color="error" :compact="vm.layout.isCompact" @click="vm.openCancelDialog(item)" />
          <ActionChip v-if="vm.canClose(item)" :label="vm.t('purchaseOrders.close')" icon="mdi-lock" color="blue-grey" :compact="vm.layout.isCompact" @click="vm.openCloseDialog(item)" />
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

    <PurchaseOrderFormDialog v-model="vm.showFormDialog" :purchase-order="vm.selectedOrder" @saved="vm.loadOrders" />

    <ConfirmDialog
      v-model="vm.showConfirmDialog"
      :title="vm.t('purchaseOrders.confirm')"
      :message="vm.t('purchaseOrders.confirmMessage', { orderNumber: vm.selectedOrder?.orderNumber })"
      :confirm-text="vm.t('purchaseOrders.confirm')"
      color="success"
      icon="mdi-check"
      :loading="vm.confirming"
      @confirm="vm.handleConfirm"
    />

    <ConfirmDialog
      v-model="vm.showCancelDialog"
      :title="vm.t('purchaseOrders.cancel')"
      :message="vm.t('purchaseOrders.cancelMessage', { orderNumber: vm.selectedOrder?.orderNumber })"
      :confirm-text="vm.t('purchaseOrders.cancel')"
      color="error"
      icon="mdi-close"
      :loading="vm.cancelling"
      @confirm="vm.handleCancel"
    />

    <ConfirmDialog
      v-model="vm.showCloseDialog"
      :title="vm.t('purchaseOrders.close')"
      :message="vm.t('purchaseOrders.closeMessage', { orderNumber: vm.selectedOrder?.orderNumber })"
      :confirm-text="vm.t('purchaseOrders.close')"
      color="blue-grey"
      icon="mdi-lock"
      :loading="vm.closing"
      @confirm="vm.handleClose"
    />
    <PurchaseOrderDetailDialog v-model="vm.showDetailDialog" :po-id="vm.selectedOrder?.id ?? null" />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { usePurchaseOrdersView } from '@features/purchasing/composables/usePurchaseOrdersView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import PurchaseOrderFormDialog from '@features/purchasing/components/organisms/PurchaseOrderFormDialog.vue';
import PurchaseOrderDetailDialog from '@features/purchasing/components/organisms/PurchaseOrderDetailDialog.vue';

const vm = reactive(usePurchaseOrdersView());

function poStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    PartiallyReceived: 'orange',
    Received: 'green',
    Closed: 'blue-grey',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}
</script>
