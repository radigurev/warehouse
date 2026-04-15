<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('goodsReceipts.create') }}
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
        <template #header.receiptNumber="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.receiptNumber" column-key="receiptNumber" />
          </div>
        </template>

        <template #header.purchaseOrderNumber="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.purchaseOrderNumber" column-key="purchaseOrderNumber" />
          </div>
        </template>

        <template #item.receivedAtUtc="{ item }">
          {{ vm.formatDate(item.receivedAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('goodsReceipts.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
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

    <GoodsReceiptFormDialog v-model="vm.showFormDialog" @saved="vm.loadReceipts" />
    <GoodsReceiptDetailDialog v-model="vm.showDetailDialog" :receipt-id="vm.selectedReceipt?.id ?? null" />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useGoodsReceiptsView } from '@features/purchasing/composables/useGoodsReceiptsView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import GoodsReceiptFormDialog from '@features/purchasing/components/organisms/GoodsReceiptFormDialog.vue';
import GoodsReceiptDetailDialog from '@features/purchasing/components/organisms/GoodsReceiptDetailDialog.vue';

const vm = reactive(useGoodsReceiptsView());
</script>
