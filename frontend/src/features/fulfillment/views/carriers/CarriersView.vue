<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('carriers.create') }}
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
        <template #header.name="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.name" column-key="name" />
          </div>
        </template>

        <template #header.code="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.code" column-key="code" />
          </div>
        </template>

        <template #item.isActive="{ item }">
          <StatusChip :active="item.isActive" />
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('carriers.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip v-if="item.isActive" :label="vm.t('carriers.deactivate')" icon="mdi-toggle-switch-off" color="error" :compact="vm.layout.isCompact" @click="vm.openDeactivateDialog(item)" />
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
      v-model="vm.showDeactivateDialog"
      :title="vm.t('carriers.deactivate')"
      :message="vm.t('carriers.deactivateConfirm', { name: vm.selectedCarrier?.name })"
      :confirm-text="vm.t('carriers.deactivate')"
      color="error"
      icon="mdi-toggle-switch-off"
      :loading="vm.deactivating"
      @confirm="vm.handleDeactivate"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useCarriersView } from '@features/fulfillment/composables/useCarriersView';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const vm = reactive(useCarriersView());
</script>
