<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('supplierReturns.create') }}
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

        <template #header.supplierName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.supplierName" column-key="supplierName" />
          </div>
        </template>

        <template #item.status="{ item }">
          <v-chip :color="returnStatusColor(item.status)" size="small" label>
            {{ vm.t(`supplierReturns.status.${item.status}`) }}
          </v-chip>
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('supplierReturns.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip v-if="vm.canConfirm(item)" :label="vm.t('supplierReturns.confirm')" icon="mdi-check" color="success" :compact="vm.layout.isCompact" @click="vm.openConfirmDialog(item)" />
          <ActionChip v-if="vm.canCancel(item)" :label="vm.t('supplierReturns.cancel')" icon="mdi-close" color="error" :compact="vm.layout.isCompact" @click="vm.openCancelDialog(item)" />
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

    <SupplierReturnFormDialog v-model="vm.showFormDialog" @saved="vm.loadReturns" />

    <ConfirmDialog
      v-model="vm.showConfirmDialog"
      :title="vm.t('supplierReturns.confirm')"
      :message="vm.t('supplierReturns.confirmMessage', { returnNumber: vm.selectedReturn?.returnNumber })"
      :confirm-text="vm.t('supplierReturns.confirm')"
      color="success"
      icon="mdi-check"
      :loading="vm.confirming"
      @confirm="vm.handleConfirm"
    />

    <ConfirmDialog
      v-model="vm.showCancelDialog"
      :title="vm.t('supplierReturns.cancel')"
      :message="vm.t('supplierReturns.cancelMessage', { returnNumber: vm.selectedReturn?.returnNumber })"
      :confirm-text="vm.t('supplierReturns.cancel')"
      color="error"
      icon="mdi-close"
      :loading="vm.cancelling"
      @confirm="vm.handleCancel"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useSupplierReturnsView } from '@features/purchasing/composables/useSupplierReturnsView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import SupplierReturnFormDialog from '@features/purchasing/components/organisms/SupplierReturnFormDialog.vue';

const vm = reactive(useSupplierReturnsView());

function returnStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}
</script>
