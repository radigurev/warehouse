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
        <template #header.pickListNumber="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.pickListNumber" column-key="pickListNumber" />
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
          <v-chip :color="pickListStatusColor(item.status)" size="small" label>
            {{ vm.t(`pickLists.status.${item.status}`) }}
          </v-chip>
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('pickLists.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip v-if="item.status === 'Pending'" :label="vm.t('pickLists.cancel')" icon="mdi-close" color="error" :compact="vm.layout.isCompact" @click="vm.openCancelDialog(item)" />
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
      v-model="vm.showCancelDialog"
      :title="vm.t('pickLists.cancel')"
      :message="vm.t('pickLists.cancelMessage', { pickListNumber: vm.selectedPickList?.pickListNumber })"
      :confirm-text="vm.t('pickLists.cancel')"
      color="error"
      icon="mdi-close"
      :loading="vm.cancelling"
      @confirm="vm.handleCancel"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { usePickListsView } from '@features/fulfillment/composables/usePickListsView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const vm = reactive(usePickListsView());

function pickListStatusColor(status: string): string {
  const map: Record<string, string> = {
    Pending: 'grey',
    Completed: 'green',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}
</script>
