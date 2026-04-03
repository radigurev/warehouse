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
import { ref, reactive, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLayoutStore } from '@/stores/layout';
import { useNotificationStore } from '@/stores/notification';
import { useColumnFilters } from '@/composables/useColumnFilters';
import { getAuditLogs } from '@/api/audit';
import { getUsers } from '@/api/users';
import type { AuditLogDto } from '@/types/audit';
import type { UserDto } from '@/types/user';
import ActionChip from '@/components/atoms/ActionChip.vue';
import ColumnFilter from '@/components/molecules/ColumnFilter.vue';

const { t, locale } = useI18n();
const layout = useLayoutStore();
const notification = useNotificationStore();

const auditLogs = ref<AuditLogDto[]>([]);
const userMap = ref<Map<number, string>>(new Map());
const loading = ref(false);
const showDetailsDialog = ref(false);
const dateFromMenu = ref(false);
const dateToMenu = ref(false);
const detailsJson = ref('');

const { columnFilters, filteredItems } = useColumnFilters(auditLogs, ['action', 'resource']);

const userItems = computed(() =>
  Array.from(userMap.value.entries()).map(([id, username]) => ({
    title: username,
    value: id,
  })),
);

const actionItems = computed(() => {
  const unique = new Set(auditLogs.value.map((log) => log.action));
  return Array.from(unique).sort();
});

function minLengthFilter(itemText: string, queryText: string): boolean {
  if (queryText.length < 2) return false;
  return itemText.toLowerCase().includes(queryText.toLowerCase());
}

const filters = reactive({
  userId: null as number | null,
  action: '',
  dateFrom: '',
  dateTo: '',
});

const headers = computed(() => [
  { title: t('audit.columns.dateTime'), key: 'createdAt', sortable: true },
  { title: t('audit.columns.user'), key: 'userId', sortable: true },
  { title: t('audit.columns.action'), key: 'action', sortable: true },
  { title: t('audit.columns.resource'), key: 'resource', sortable: true },
  { title: t('audit.columns.details'), key: 'details', sortable: false },
  { title: t('audit.columns.ipAddress'), key: 'ipAddress', sortable: false },
]);

const formattedDetails = computed(() => {
  try {
    return JSON.stringify(JSON.parse(detailsJson.value), null, 2);
  } catch {
    return detailsJson.value;
  }
});

onMounted(async () => {
  await loadUsers();
  await loadAuditLogs();
});

async function loadUsers(): Promise<void> {
  try {
    const response = await getUsers(1, 1000, true);
    for (const user of response.items) {
      userMap.value.set(user.id, user.username);
    }
  } catch {
    // Non-critical — will fall back to showing IDs
  }
}

async function loadAuditLogs(): Promise<void> {
  loading.value = true;
  try {
    const response = await getAuditLogs({
      userId: filters.userId || undefined,
      action: filters.action || undefined,
      fromDate: filters.dateFrom || undefined,
      toDate: filters.dateTo || undefined,
    });
    auditLogs.value = response.items;
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    loading.value = false;
  }
}

function formatDateTime(dateStr: string): string {
  return new Date(dateStr).toLocaleString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatDateValue(date: unknown): string {
  if (date instanceof Date) {
    return date.toISOString().split('T')[0];
  }
  return '';
}

function onDateFromPicked(value: unknown): void {
  filters.dateFrom = formatDateValue(value);
  dateFromMenu.value = false;
}

function onDateToPicked(value: unknown): void {
  filters.dateTo = formatDateValue(value);
  dateToMenu.value = false;
}

function clearFilters(): void {
  filters.userId = null;
  filters.action = '';
  filters.dateFrom = '';
  filters.dateTo = '';
  loadAuditLogs();
}

function openDetails(details: string): void {
  detailsJson.value = details;
  showDetailsDialog.value = true;
}
</script>

