<template>
  <div>
    <v-card class="mb-4">
      <v-card-text>
        <v-row align="center">
          <v-col cols="12" md="3">
            <v-autocomplete
              v-model="vm.filters.userId"
              :items="vm.userItems"
              :label="vm.t('audit.filters.user')"
              prepend-inner-icon="mdi-account-search"
              hide-details
              clearable
              :custom-filter="vm.minLengthFilter"
            />
          </v-col>

          <v-col cols="12" md="2">
            <v-autocomplete
              v-model="vm.filters.action"
              :items="vm.actionItems"
              :label="vm.t('audit.filters.action')"
              prepend-inner-icon="mdi-flash"
              hide-details
              clearable
              :custom-filter="vm.minLengthFilter"
            />
          </v-col>

          <v-col cols="12" md="2">
            <v-menu v-model="vm.dateFromMenu" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  v-bind="menuProps"
                  :model-value="vm.filters.dateFrom"
                  :label="vm.t('audit.filters.dateFrom')"
                  prepend-inner-icon="mdi-calendar-start"
                  hide-details
                  clearable
                  readonly
                  @click:clear="vm.filters.dateFrom = ''"
                />
              </template>
              <v-date-picker
                :model-value="vm.filters.dateFrom ? new Date(vm.filters.dateFrom) : undefined"
                @update:model-value="vm.onDateFromPicked"
                color="primary"
              />
            </v-menu>
          </v-col>

          <v-col cols="12" md="2">
            <v-menu v-model="vm.dateToMenu" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  v-bind="menuProps"
                  :model-value="vm.filters.dateTo"
                  :label="vm.t('audit.filters.dateTo')"
                  prepend-inner-icon="mdi-calendar-end"
                  hide-details
                  clearable
                  readonly
                  @click:clear="vm.filters.dateTo = ''"
                />
              </template>
              <v-date-picker
                :model-value="vm.filters.dateTo ? new Date(vm.filters.dateTo) : undefined"
                @update:model-value="vm.onDateToPicked"
                color="primary"
              />
            </v-menu>
          </v-col>

          <v-col cols="12" md="3" class="d-flex ga-2">
            <v-btn color="primary" @click="vm.loadAuditLogs">{{ vm.t('audit.filters.apply') }}</v-btn>
            <v-btn variant="outlined" @click="vm.clearFilters">{{ vm.t('audit.filters.clear') }}</v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card>
      <v-data-table-server
        :headers="vm.headers"
        :items="vm.filteredItems"
        :items-length="vm.totalCount"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :page="vm.page"
        :items-per-page="vm.pageSize"
        :items-per-page-options="[10, 25, 50, 100]"
        hover
        @update:page="vm.handlePageChange"
        @update:items-per-page="vm.handlePageSizeChange"
      >
        <template #header.action="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.action" column-key="action" />
          </div>
        </template>

        <template #header.resource="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.resource" column-key="resource" />
          </div>
        </template>

        <template #item.userId="{ item }">
          {{ item.userId ? (vm.userMap.get(item.userId) ?? `#${item.userId}`) : '—' }}
        </template>

        <template #item.createdAt="{ item }">
          {{ vm.formatDateTime(item.createdAt) }}
        </template>

        <template #item.details="{ item }">
          <ActionChip
            v-if="item.details"
            :label="vm.t('audit.viewDetails')"
            icon="mdi-code-json"
            color="secondary"
            :compact="vm.layout.isCompact"
            @click="vm.openDetails(item.details)"
          />
        </template>

        <template #tfoot>
          <tr>
            <td :colspan="vm.headers.length" class="text-center text-caption text-medium-emphasis py-1">
              {{ vm.t('common.pageInfo', { page: vm.page, pages: vm.totalPages, total: vm.totalCount }) }}
            </td>
          </tr>
        </template>
      </v-data-table-server>
    </v-card>

    <v-dialog v-model="vm.showDetailsDialog" max-width="600">
      <v-card>
        <v-card-title class="text-h6">{{ vm.t('audit.viewDetails') }}</v-card-title>
        <v-card-text>
          <pre class="text-body-2" style="white-space: pre-wrap; word-break: break-word">{{ vm.formattedDetails }}</pre>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="vm.showDetailsDialog = false">{{ vm.t('common.close') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useAuditView } from '@features/auth/composables/useAuditView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';

const vm = reactive(useAuditView());
</script>
