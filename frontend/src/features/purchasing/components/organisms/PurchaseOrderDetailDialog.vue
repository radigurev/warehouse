<template>
  <FormWrapper v-model="visible" max-width="800" :title="po?.orderNumber ?? t('purchaseOrders.viewDetails')" icon="mdi-file-document-outline">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="po">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ po.orderNumber }}</div>
            <div class="text-caption text-medium-emphasis">{{ po.supplierName }}</div>
          </div>
          <v-spacer />
          <v-chip :color="statusColor(po.status)" size="small" label>
            {{ t(`purchaseOrders.status.${po.status}`) }}
          </v-chip>
        </div>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('purchaseOrders.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('purchaseOrders.columns.totalAmount') }}</div>
                <div class="font-weight-medium">{{ po.totalAmount.toFixed(2) }}</div>
              </v-col>
              <v-col v-if="po.expectedDeliveryDate" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('purchaseOrders.columns.expectedDelivery') }}</div>
                <div>{{ po.expectedDeliveryDate }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('purchaseOrders.columns.createdAt') }}</div>
                <div>{{ formatDate(po.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="po.confirmedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('purchaseOrders.detail.confirmedAt') }}</div>
                <div>{{ formatDate(po.confirmedAtUtc) }}</div>
              </v-col>
              <v-col v-if="po.closedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('purchaseOrders.detail.closedAt') }}</div>
                <div>{{ formatDate(po.closedAtUtc) }}</div>
              </v-col>
              <v-col v-if="po.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('purchaseOrders.form.notes') }}</div>
                <div class="text-body-2">{{ po.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-format-list-bulleted" class="mr-2" size="small" />
            {{ t('purchaseOrders.detail.lines') }}
          </div>
          <div class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('purchaseOrders.lines.product') }}</th>
                  <th class="text-end">{{ t('purchaseOrders.lines.orderedQuantity') }}</th>
                  <th class="text-end">{{ t('purchaseOrders.lines.unitPrice') }}</th>
                  <th class="text-end">{{ t('purchaseOrders.lines.lineTotal') }}</th>
                  <th class="text-end">{{ t('purchaseOrders.lines.receivedQuantity') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in po.lines" :key="line.id">
                  <td>{{ line.productId }}</td>
                  <td class="text-end">{{ line.orderedQuantity }}</td>
                  <td class="text-end">{{ line.unitPrice.toFixed(2) }}</td>
                  <td class="text-end">{{ line.lineTotal.toFixed(2) }}</td>
                  <td class="text-end">{{ line.receivedQuantity }} / {{ line.orderedQuantity }}</td>
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
          {{ t('purchaseOrders.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getPurchaseOrderById } from '@features/purchasing/api/purchase-orders';
import type { PurchaseOrderDetailDto } from '@features/purchasing/types/purchasing';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  poId: number | null;
}>();

const po = ref<PurchaseOrderDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.poId !== null) {
    loading.value = true;
    notFound.value = false;
    po.value = null;
    try {
      po.value = await getPurchaseOrderById(props.poId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }
});

function statusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey', Confirmed: 'blue', PartiallyReceived: 'orange',
    Received: 'green', Closed: 'blue-grey', Cancelled: 'red',
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
  router.push({ name: 'purchase-order-detail', params: { id: props.poId! } });
}
</script>

<style scoped>
.detail-dialog-content { background: #f1f5f9; }
.section-header { display: flex; align-items: center; padding: 12px 16px; font-size: 0.875rem; font-weight: 500; }
.lines-table-wrapper { max-height: 250px; overflow-y: auto; }
</style>
