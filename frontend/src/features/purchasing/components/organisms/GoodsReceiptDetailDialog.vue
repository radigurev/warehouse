<template>
  <FormWrapper v-model="visible" max-width="800" :title="receipt?.receiptNumber ?? t('goodsReceipts.viewDetails')" icon="mdi-package-down">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="receipt">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ receipt.receiptNumber }}</div>
            <div class="text-caption text-medium-emphasis">{{ receipt.purchaseOrderNumber }}</div>
          </div>
          <v-spacer />
          <v-chip :color="statusColor(receipt.status)" size="small" label>
            {{ receipt.status }}
          </v-chip>
        </div>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('goodsReceipts.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('goodsReceipts.columns.receivedAt') }}</div>
                <div>{{ formatDate(receipt.receivedAtUtc) }}</div>
              </v-col>
              <v-col v-if="receipt.completedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('goodsReceipts.detail.completedAt') }}</div>
                <div>{{ formatDate(receipt.completedAtUtc) }}</div>
              </v-col>
              <v-col v-if="receipt.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('goodsReceipts.form.notes') }}</div>
                <div class="text-body-2">{{ receipt.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-format-list-bulleted" class="mr-2" size="small" />
            {{ t('goodsReceipts.detail.lines') }}
          </div>
          <div class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th class="text-end">{{ t('goodsReceipts.lines.receivedQty') }}</th>
                  <th>{{ t('goodsReceipts.lines.batchNumber') }}</th>
                  <th>{{ t('goodsReceipts.lines.inspectionStatus') }}</th>
                  <th>{{ t('goodsReceipts.lines.manufacturingDate') }}</th>
                  <th>{{ t('goodsReceipts.lines.expiryDate') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in receipt.lines" :key="line.id">
                  <td class="text-end">{{ line.receivedQuantity }}</td>
                  <td>{{ line.batchNumber || '\u2014' }}</td>
                  <td>
                    <v-chip :color="inspectionColor(line.inspectionStatus)" size="small" label>
                      {{ line.inspectionStatus }}
                    </v-chip>
                  </td>
                  <td>{{ line.manufacturingDate || '\u2014' }}</td>
                  <td>{{ line.expiryDate || '\u2014' }}</td>
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
          {{ t('goodsReceipts.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getGoodsReceiptById } from '@features/purchasing/api/goods-receipts';
import type { GoodsReceiptDetailDto } from '@features/purchasing/types/purchasing';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  receiptId: number | null;
}>();

const receipt = ref<GoodsReceiptDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.receiptId !== null) {
    loading.value = true;
    notFound.value = false;
    receipt.value = null;
    try {
      receipt.value = await getGoodsReceiptById(props.receiptId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }
});

function statusColor(status: string): string {
  const map: Record<string, string> = {
    Open: 'orange', Completed: 'green',
  };
  return map[status] || 'grey';
}

function inspectionColor(status: string): string {
  const map: Record<string, string> = {
    Pending: 'grey', Accepted: 'green', Rejected: 'red', Quarantined: 'orange',
  };
  return map[status] || 'grey';
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
    year: 'numeric', month: 'short', day: 'numeric',
  });
}

function openFullPage(): void {
  visible.value = false;
  router.push({ name: 'goods-receipt-detail', params: { id: props.receiptId! } });
}
</script>

<style scoped>
.detail-dialog-content { background: #f1f5f9; }
.section-header { display: flex; align-items: center; padding: 12px 16px; font-size: 0.875rem; font-weight: 500; }
.lines-table-wrapper { max-height: 250px; overflow-y: auto; }
</style>
