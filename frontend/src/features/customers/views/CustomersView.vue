<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('customers.create') }}
      </v-btn>
    </div>

    <v-card>
      <v-data-table
        :headers="vm.headers"
        :items="vm.filteredItems"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :items-per-page="25"
        hover
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

        <template #header.categoryName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.categoryName" column-key="categoryName" />
          </div>
        </template>

        <template #item.isActive="{ item }">
          <StatusChip :active="item.isActive" />
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ vm.formatDate(item.createdAtUtc) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('customers.viewDetails')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.handleDetail(item)" />
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip v-if="item.isActive" :label="vm.t('customers.deactivate')" icon="mdi-account-off" color="error" :compact="vm.layout.isCompact" @click="vm.openDeactivateDialog(item)" />
          <ActionChip v-else :label="vm.t('customers.reactivate')" icon="mdi-account-check" color="success" :compact="vm.layout.isCompact" @click="vm.handleReactivate(item)" />
        </template>
      </v-data-table>
    </v-card>

    <CustomerFormDialog v-model="vm.showFormDialog" :customer="vm.selectedCustomer" @saved="vm.loadCustomers" />
    <ConfirmDialog
      v-model="vm.showDeactivateDialog"
      :title="vm.t('customers.deactivate')"
      :message="vm.t('customers.deactivateConfirm', { name: vm.selectedCustomer?.name })"
      :confirm-text="vm.t('customers.deactivate')"
      color="error"
      icon="mdi-account-off"
      :loading="vm.deactivating"
      @confirm="vm.handleDeactivate"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useCustomersView } from '@features/customers/composables/useCustomersView';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import CustomerFormDialog from '@features/customers/components/organisms/CustomerFormDialog.vue';

const vm = reactive(useCustomersView());
</script>
