<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('customerReturns.create') }}
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
        <template #header.returnNumber="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.returnNumber" column-key="returnNumber" />
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
          <v-chip :color="returnStatusColor(item.status)" size="small" label>
            {{ vm.t(`customerReturns.status.${item.status}`) }}
          </v-chip>
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('customerReturns.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip v-if="item.status === 'Draft'" :label="vm.t('customerReturns.confirm')" icon="mdi-check" color="success" :compact="vm.layout.isCompact" @click="vm.openConfirmDialog(item)" />
          <ActionChip v-if="item.status === 'Confirmed'" :label="vm.t('customerReturns.receive')" icon="mdi-package-down" color="primary" :compact="vm.layout.isCompact" @click="vm.openReceiveDialog(item)" />
          <ActionChip v-if="item.status === 'Received'" :label="vm.t('customerReturns.close')" icon="mdi-lock" color="blue-grey" :compact="vm.layout.isCompact" @click="vm.openCloseDialog(item)" />
          <ActionChip v-if="item.status === 'Draft' || item.status === 'Confirmed'" :label="vm.t('customerReturns.cancel')" icon="mdi-close" color="error" :compact="vm.layout.isCompact" @click="vm.openCancelDialog(item)" />
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
      :title="vm.t('customerReturns.confirm')"
      :message="vm.t('customerReturns.confirmMessage', { returnNumber: vm.selectedReturn?.returnNumber })"
      :confirm-text="vm.t('customerReturns.confirm')"
      color="success"
      icon="mdi-check"
      :loading="vm.confirming"
      @confirm="vm.handleConfirm"
    />

    <ConfirmDialog
      v-model="vm.showReceiveDialog"
      :title="vm.t('customerReturns.receive')"
      :message="vm.t('customerReturns.receiveMessage', { returnNumber: vm.selectedReturn?.returnNumber })"
      :confirm-text="vm.t('customerReturns.receive')"
      color="primary"
      icon="mdi-package-down"
      :loading="vm.receiving"
      @confirm="vm.handleReceive"
    />

    <ConfirmDialog
      v-model="vm.showCloseDialog"
      :title="vm.t('customerReturns.close')"
      :message="vm.t('customerReturns.closeMessage', { returnNumber: vm.selectedReturn?.returnNumber })"
      :confirm-text="vm.t('customerReturns.close')"
      color="blue-grey"
      icon="mdi-lock"
      :loading="vm.closing"
      @confirm="vm.handleClose"
    />

    <ConfirmDialog
      v-model="vm.showCancelDialog"
      :title="vm.t('customerReturns.cancel')"
      :message="vm.t('customerReturns.cancelMessage', { returnNumber: vm.selectedReturn?.returnNumber })"
      :confirm-text="vm.t('customerReturns.cancel')"
      color="error"
      icon="mdi-close"
      :loading="vm.cancelling"
      @confirm="vm.handleCancel"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useCustomerReturnsView } from '@features/fulfillment/composables/useCustomerReturnsView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const vm = reactive(useCustomerReturnsView());

function returnStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    Received: 'amber',
    Closed: 'green',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}
</script>
