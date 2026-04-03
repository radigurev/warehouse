<template>
  <div>
    <h1 class="text-h4 mb-4">{{ t('audit.title') }}</h1>

    <v-card class="mb-4">
      <v-card-text>
        <v-row align="center">
          <v-col cols="12" md="3">
            <v-text-field
              v-model="filters.userId"
              :label="t('audit.filters.user')"
              type="number"
              hide-details
              clearable
            />
          </v-col>

          <v-col cols="12" md="2">
            <v-text-field
              v-model="filters.action"
              :label="t('audit.filters.action')"
              hide-details
              clearable
            />
          </v-col>

          <v-col cols="12" md="2">
            <v-text-field
              v-model="filters.dateFrom"
              :label="t('audit.filters.dateFrom')"
              type="date"
              hide-details
              clearable
            />
          </v-col>

          <v-col cols="12" md="2">
            <v-text-field
              v-model="filters.dateTo"
              :label="t('audit.filters.dateTo')"
              type="date"
              hide-details
              clearable
            />
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
        :items="auditLogs"
        :loading="loading"
        :density="layout.vuetifyDensity"
        :items-per-page="25"
        hover
      >
        <template #item.createdAt="{ item }">
          {{ formatDateTime(item.createdAt) }}
        </template>

        <template #item.details="{ item }">
          <v-btn
            v-if="item.details"
            variant="text"
            size="small"
            icon="mdi-code-json"
            :title="t('audit.viewDetails')"
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
import { getAuditLogs } from '@/api/audit';
import type { AuditLogDto } from '@/types/audit';

const { t, locale } = useI18n();
const layout = useLayoutStore();
const notification = useNotificationStore();

const auditLogs = ref<AuditLogDto[]>([]);
const loading = ref(false);
const showDetailsDialog = ref(false);
const detailsJson = ref('');

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

onMounted(() => loadAuditLogs());

async function loadAuditLogs(): Promise<void> {
  loading.value = true;
  try {
    auditLogs.value = await getAuditLogs({
      userId: filters.userId || undefined,
      action: filters.action || undefined,
      dateFrom: filters.dateFrom || undefined,
      dateTo: filters.dateTo || undefined,
    });
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
