<template>
  <FormWrapper v-model="visible" max-width="700" :title="t('transfers.title') + (transfer ? ` #${transfer.id}` : '')" icon="mdi-truck">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="transfer">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ transfer.sourceWarehouseName }} &rarr; {{ transfer.destinationWarehouseName }}</div>
          </div>
          <v-spacer />
          <v-chip :color="statusColor(transfer.status)" size="small" variant="flat">
            {{ t(`transfers.statuses.${transfer.status}`) }}
          </v-chip>
        </div>

        <!-- Info -->
        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('transfers.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('transfers.columns.sourceWarehouse') }}</div>
                <div>{{ transfer.sourceWarehouseName }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('transfers.columns.destinationWarehouse') }}</div>
                <div>{{ transfer.destinationWarehouseName }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('transfers.columns.createdAt') }}</div>
                <div>{{ formatDate(transfer.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="transfer.completedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('transfers.complete') }}</div>
                <div>{{ formatDate(transfer.completedAtUtc) }}</div>
              </v-col>
              <v-col v-if="transfer.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('transfers.form.notes') }}</div>
                <div class="text-body-2">{{ transfer.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <!-- Lines -->
        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-format-list-bulleted" class="mr-2" size="small" />
            {{ t('transfers.detail.lines') }}
          </div>
          <v-card-text v-if="transfer.lines.length === 0" class="text-medium-emphasis">
            {{ t('transfers.detail.noLines') }}
          </v-card-text>
          <div v-else class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('transfers.lines.product') }}</th>
                  <th class="text-end">{{ t('transfers.lines.quantity') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in transfer.lines" :key="line.id">
                  <td>{{ line.productName }}</td>
                  <td class="text-end">{{ line.quantity }}</td>
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
          {{ t('transfers.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getTransferById } from '@features/inventory/api/transfers';
import type { WarehouseTransferDetailDto } from '@features/inventory/types/inventory';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  transferId: number | null;
}>();

const transfer = ref<WarehouseTransferDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.transferId) {
    loading.value = true;
    notFound.value = false;
    transfer.value = null;
    try {
      transfer.value = await getTransferById(props.transferId);
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

function statusColor(status: string): string {
  switch (status) {
    case 'Draft': return 'warning';
    case 'Completed': return 'success';
    case 'Cancelled': return 'error';
    default: return 'grey';
  }
}

function openFullPage(): void {
  visible.value = false;
  router.push({ name: 'transfer-detail', params: { id: props.transferId! } });
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
