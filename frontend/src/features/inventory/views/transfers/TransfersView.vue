<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('transfers.create') }}
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
        <template #header.sourceWarehouseName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.sourceWarehouseName" column-key="sourceWarehouseName" />
          </div>
        </template>

        <template #header.destinationWarehouseName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.destinationWarehouseName" column-key="destinationWarehouseName" />
          </div>
        </template>

        <template #header.status="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.status" column-key="status" />
          </div>
        </template>

        <template #item.status="{ item }">
          <v-chip :color="vm.statusColor(item.status)" size="small" variant="flat">
            {{ vm.t(`transfers.statuses.${item.status}`) }}
          </v-chip>
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('transfers.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
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

    <TransferFormDialog v-model="vm.showFormDialog" @saved="vm.loadTransfers" />
    <TransferDetailDialog v-model="vm.showDetailDialog" :transfer-id="vm.selectedTransfer?.id ?? null" />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useTransfersView } from '@features/inventory/composables/useTransfersView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import TransferFormDialog from '@features/inventory/components/organisms/TransferFormDialog.vue';
import TransferDetailDialog from '@features/inventory/components/organisms/TransferDetailDialog.vue';

const vm = reactive(useTransfersView());
</script>
