<template>
  <FormWrapper v-model="visible" max-width="700" :title="t('adjustments.title') + (adjustment ? ` #${adjustment.id}` : '')" icon="mdi-pencil-ruler">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="adjustment">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div class="text-h6">{{ adjustment.reason }}</div>
          <v-spacer />
          <v-chip :color="statusColor(adjustment.status)" size="small" variant="flat">
            {{ translateStatus(adjustment.status) }}
          </v-chip>
        </div>

        <!-- Info -->
        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('adjustments.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('adjustments.columns.status') }}</div>
                <div>
                  <v-chip :color="statusColor(adjustment.status)" size="small" variant="flat">
                    {{ translateStatus(adjustment.status) }}
                  </v-chip>
                </div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('adjustments.columns.createdAt') }}</div>
                <div>{{ formatDate(adjustment.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="adjustment.approvedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('adjustments.approve') }}</div>
                <div>{{ formatDate(adjustment.approvedAtUtc) }}</div>
              </v-col>
              <v-col v-if="adjustment.appliedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('adjustments.apply') }}</div>
                <div>{{ formatDate(adjustment.appliedAtUtc) }}</div>
              </v-col>
              <v-col v-if="adjustment.rejectedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('adjustments.reject') }}</div>
                <div>{{ formatDate(adjustment.rejectedAtUtc) }}</div>
              </v-col>
              <v-col v-if="adjustment.rejectionReason" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('adjustments.form.rejectionReason') }}</div>
                <div class="text-body-2">{{ adjustment.rejectionReason }}</div>
              </v-col>
              <v-col v-if="adjustment.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('adjustments.form.notes') }}</div>
                <div class="text-body-2">{{ adjustment.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <!-- Lines -->
        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-format-list-bulleted" class="mr-2" size="small" />
            {{ t('adjustments.detail.lines') }}
          </div>
          <v-card-text v-if="adjustment.lines.length === 0" class="text-medium-emphasis">
            {{ t('adjustments.detail.noLines') }}
          </v-card-text>
          <div v-else class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('adjustments.lines.product') }}</th>
                  <th>{{ t('adjustments.lines.location') }}</th>
                  <th class="text-end">{{ t('adjustments.lines.expected') }}</th>
                  <th class="text-end">{{ t('adjustments.lines.actual') }}</th>
                  <th class="text-end">{{ t('adjustments.lines.variance') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in adjustment.lines" :key="line.id">
                  <td>{{ line.productName }}</td>
                  <td>{{ line.locationName || '\u2014' }}</td>
                  <td class="text-end">{{ line.expectedQuantity }}</td>
                  <td class="text-end">{{ line.actualQuantity }}</td>
                  <td class="text-end">
                    <span :class="varianceColor(line.actualQuantity - line.expectedQuantity)">
                      {{ (line.actualQuantity - line.expectedQuantity) > 0 ? '+' : '' }}{{ line.actualQuantity - line.expectedQuantity }}
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
          {{ t('adjustments.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getAdjustmentById } from '@features/inventory/api/adjustments';
import type { InventoryAdjustmentDetailDto } from '@features/inventory/types/inventory';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  adjustmentId: number | null;
}>();

const adjustment = ref<InventoryAdjustmentDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.adjustmentId) {
    loading.value = true;
    notFound.value = false;
    adjustment.value = null;
    try {
      adjustment.value = await getAdjustmentById(props.adjustmentId);
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
  const key = `adjustments.statuses.${status}`;
  const translated = t(key);
  return translated !== key ? translated : status;
}

function statusColor(status: string): string {
  switch (status) {
    case 'Pending': return 'warning';
    case 'Approved': return 'info';
    case 'Rejected': return 'error';
    case 'Applied': return 'success';
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
  router.push({ name: 'adjustment-detail', params: { id: props.adjustmentId! } });
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
