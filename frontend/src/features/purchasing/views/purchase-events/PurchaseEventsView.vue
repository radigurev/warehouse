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
        show-expand
        @update:page="vm.handlePageChange"
        @update:items-per-page="vm.handlePageSizeChange"
      >
        <template #header.eventType="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.eventType" column-key="eventType" />
          </div>
        </template>

        <template #header.entityType="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.entityType" column-key="entityType" />
          </div>
        </template>

        <template #item.occurredAtUtc="{ item }">
          {{ vm.formatDate(item.occurredAtUtc) }}
        </template>

        <template #item.payload="{ item }">
          {{ vm.truncatePayload(item.payload) }}
        </template>

        <template #expanded-row="{ columns, item }">
          <tr>
            <td :colspan="columns.length" class="pa-4">
              <pre class="text-body-2" style="white-space: pre-wrap; word-break: break-all">{{ formatPayload(item.payload) }}</pre>
            </td>
          </tr>
        </template>

        <template #tfoot>
          <tr>
            <td :colspan="vm.headers.length + 1" class="text-center text-caption text-medium-emphasis py-1">
              {{ vm.t('common.pageInfo', { page: vm.searchParams.page, pages: vm.totalPages, total: vm.totalCount }) }}
            </td>
          </tr>
        </template>
      </v-data-table-server>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { usePurchaseEventsView } from '@features/purchasing/composables/usePurchaseEventsView';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';

const vm = reactive(usePurchaseEventsView());

function formatPayload(payload: string | null): string {
  if (!payload) return '';
  try {
    return JSON.stringify(JSON.parse(payload), null, 2);
  } catch {
    return payload;
  }
}
</script>
