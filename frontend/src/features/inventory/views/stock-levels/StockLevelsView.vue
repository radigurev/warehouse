<template>
  <div>
    <v-card class="view-list-card">
      <v-data-table
        :headers="vm.headers"
        :items="vm.filteredItems"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :items-per-page="-1"
        fixed-header
        hover
      >
        <template #header.productName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.productName" column-key="productName" />
          </div>
        </template>

        <template #header.productCode="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.productCode" column-key="productCode" />
          </div>
        </template>

        <template #header.warehouseName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.warehouseName" column-key="warehouseName" />
          </div>
        </template>

        <template #header.locationName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.locationName" column-key="locationName" />
          </div>
        </template>
      </v-data-table>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useStockLevelsView } from '@features/inventory/composables/useStockLevelsView';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';

const vm = reactive(useStockLevelsView());
</script>
