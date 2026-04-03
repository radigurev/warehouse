<template>
  <div>
    <v-card class="mb-4">
      <v-card-text>
        <v-row align="center">
          <v-col cols="12" md="3">
            <v-autocomplete
              v-model="filters.userId"
              :items="userItems"
              :label="t('audit.filters.user')"
              prepend-inner-icon="mdi-account-search"
              hide-details
              clearable
              :custom-filter="minLengthFilter"
            />
          </v-col>

          <v-col cols="12" md="2">
            <v-autocomplete
              v-model="filters.action"
              :items="actionItems"
              :label="t('audit.filters.action')"
              prepend-inner-icon="mdi-flash"
              hide-details
              clearable
              :custom-filter="minLengthFilter"
            />
          </v-col>

          <v-col cols="12" md="2">
            <v-menu v-model="dateFromMenu" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  v-bind="menuProps"
                  :model-value="filters.dateFrom"
                  :label="t('audit.filters.dateFrom')"
                  prepend-inner-icon="mdi-calendar-start"
                  hide-details
                  clearable
                  readonly
                  @click:clear="filters.dateFrom = ''"
                />
              </template>
              <v-date-picker
                :model-value="filters.dateFrom ? new Date(filters.dateFrom) : undefined"
                @update:model-value="onDateFromPicked"
                color="primary"
              />
            </v-menu>
          </v-col>

          <v-col cols="12" md="2">
            <v-menu v-model="dateToMenu" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  v-bind="menuProps"
                  :model-value="filters.dateTo"
                  :label="t('audit.filters.dateTo')"
                  prepend-inner-icon="mdi-calendar-end"
                  hide-details
                  clearable
                  readonly
                  @click:clear="filters.dateTo = ''"
                />
              </template>
              <v-date-picker
                :model-value="filters.dateTo ? new Date(filters.dateTo) : undefined"
                @update:model-value="onDateToPicked"
                color="primary"
              />
            </v-menu>
          </v-col>

          <v-col cols="12" md="3" class="d-flex ga-2">
            <v-btn color="primary" @click="loadAuditLogs">{{ t('audit.filters.apply') }}</v-btn>
            <v-btn variant="outlined" @click="clearFilters">{{ t('audit.filters.clear') }}</v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card>
      <v-data-table
        :headers="headers"
        :items="filteredItems"
        :loading="loading"
        :density="layout.vuetifyDensity"
        :items-per-page="25"
        hover
      >
        <template #header.action="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.action" column-key="action" />
          </div>
        </template>

        <template #header.resource="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.resource" column-key="resource" />
          </div>
        </template>

        <template #item.userId="{ item }">
          {{ item.userId ? (userMap.get(item.userId) ?? `#${item.userId}`) : '—' }}
        </template>

        <template #item.createdAt="{ item }">
          {{ formatDateTime(item.createdAt) }}
        </template>

        <template #item.details="{ item }">
          <ActionChip
            v-if="item.details"
            :label="t('audit.viewDetails')"
            icon="mdi-code-json"
            color="secondary"
            :compact="layout.isCompact"
            @click="openDetails(item.details)"
          />
        </template>
      </v-data-table>
    </v-card>

    <v-dialog v-model="showDetailsDialog" max-width="600">
      <v-card>
        <v-card-title class="text-h6">{{ t('audit.viewDetails') }}</v-card-title>
        <v-card-text>
          <pre class="text-body-2" style="white-space: pre-wrap; word-break: break-word">{{ formattedDetails }}</pre>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDetailsDialog = false">{{ t('common.close') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { useAuditView } from '@features/auth/composables/useAuditView';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';

const {
  t,
  layout,
  loading,
  showDetailsDialog,
  dateFromMenu,
  dateToMenu,
  userMap,
  userItems,
  actionItems,
  minLengthFilter,
  filters,
  columnFilters,
  filteredItems,
  headers,
  formattedDetails,
  loadAuditLogs,
  formatDateTime,
  onDateFromPicked,
  onDateToPicked,
  clearFilters,
  openDetails,
} = useAuditView();
</script>

