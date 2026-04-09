<template>
  <FormWrapper v-model="visible" max-width="700" :title="session?.name ?? t('stocktake.viewDetails')" icon="mdi-clipboard-check">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="session">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div class="text-h6">{{ session.name }}</div>
          <v-spacer />
          <v-chip :color="statusColor(session.status)" size="small" variant="flat">
            {{ translateStatus(session.status) }}
          </v-chip>
        </div>

        <!-- Info -->
        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('stocktake.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('stocktake.columns.warehouse') }}</div>
                <div>{{ session.warehouseName }}</div>
              </v-col>
              <v-col v-if="session.zoneName" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('stocktake.columns.zone') }}</div>
                <div>{{ session.zoneName }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('stocktake.columns.createdAt') }}</div>
                <div>{{ formatDate(session.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="session.startedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('stocktake.start') }}</div>
                <div>{{ formatDate(session.startedAtUtc) }}</div>
              </v-col>
              <v-col v-if="session.completedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('stocktake.complete') }}</div>
                <div>{{ formatDate(session.completedAtUtc) }}</div>
              </v-col>
              <v-col v-if="session.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('stocktake.form.notes') }}</div>
                <div class="text-body-2">{{ session.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <!-- Counts -->
        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-counter" class="mr-2" size="small" />
            {{ t('stocktake.detail.counts') }}
          </div>
          <v-card-text v-if="session.counts.length === 0" class="text-medium-emphasis">
            {{ t('stocktake.detail.noCounts') }}
          </v-card-text>
          <div v-else class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('stocktake.counts.product') }}</th>
                  <th>{{ t('stocktake.counts.location') }}</th>
                  <th class="text-end">{{ t('stocktake.counts.expected') }}</th>
                  <th class="text-end">{{ t('stocktake.counts.actual') }}</th>
                  <th class="text-end">{{ t('stocktake.counts.variance') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="count in session.counts" :key="count.id">
                  <td>{{ count.productName }}</td>
                  <td>{{ count.locationName || '\u2014' }}</td>
                  <td class="text-end">{{ count.expectedQuantity }}</td>
                  <td class="text-end">{{ count.actualQuantity }}</td>
                  <td class="text-end">
                    <span :class="varianceColor(count.variance)">
                      {{ count.variance > 0 ? '+' : '' }}{{ count.variance }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </v-table>
          </div>
        </v-card>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="visible = false">{{ t('common.close') }}</v-btn>
        <v-btn color="primary" variant="flat" prepend-icon="mdi-open-in-new" @click="openFullPage">
          {{ t('stocktake.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getSessionById } from '@features/inventory/api/stocktake';
import type { StocktakeSessionDetailDto } from '@features/inventory/types/inventory';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  sessionId: number | null;
}>();

const session = ref<StocktakeSessionDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.sessionId) {
    loading.value = true;
    notFound.value = false;
    session.value = null;
    try {
      session.value = await getSessionById(props.sessionId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }
});

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function translateStatus(status: string): string {
  const key = `stocktake.statuses.${status}`;
  const translated = t(key);
  return translated !== key ? translated : status;
}

function statusColor(status: string): string {
  switch (status) {
    case 'Draft': return 'default';
    case 'InProgress': return 'info';
    case 'Completed': return 'success';
    case 'Cancelled': return 'error';
    default: return 'default';
  }
}

function varianceColor(variance: number): string {
  if (variance < 0) return 'text-error';
  if (variance > 0) return 'text-success';
  return '';
}

function openFullPage(): void {
  visible.value = false;
  router.push({ name: 'stocktake-detail', params: { id: props.sessionId! } });
}
</script>

<style scoped>
.detail-dialog-content {
  background: #f1f5f9;
}

.section-header {
  display: flex;
  align-items: center;
  padding: 12px 16px;
  font-size: 0.875rem;
  font-weight: 500;
}

.lines-table-wrapper {
  max-height: 250px;
  overflow-y: auto;
}
</style>
