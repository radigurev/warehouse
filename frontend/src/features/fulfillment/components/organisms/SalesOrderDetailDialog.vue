<template>
  <FormWrapper v-model="visible" max-width="900" :title="so?.orderNumber ?? t('salesOrders.viewDetails')" icon="mdi-file-document-outline">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="so">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ so.orderNumber }}</div>
            <div class="text-caption text-medium-emphasis">{{ so.customerName }}</div>
          </div>
          <v-spacer />
          <v-chip :color="statusColor(so.status)" size="small" label>
            {{ t(`salesOrders.status.${so.status}`) }}
          </v-chip>
        </div>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('salesOrders.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.columns.warehouseName') }}</div>
                <div>{{ so.warehouseName }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.columns.totalAmount') }}</div>
                <div class="font-weight-medium">{{ so.totalAmount.toFixed(2) }}</div>
              </v-col>
              <v-col v-if="so.requestedShipDate" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.columns.requestedShipDate') }}</div>
                <div>{{ formatDate(so.requestedShipDate) }}</div>
              </v-col>
              <v-col v-if="so.carrierName" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.detail.carrier') }}</div>
                <div>{{ so.carrierName }}{{ so.carrierServiceLevelName ? ' — ' + so.carrierServiceLevelName : '' }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.columns.createdAtUtc') }}</div>
                <div>{{ formatDate(so.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="so.confirmedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.detail.confirmedAt') }}</div>
                <div>{{ formatDate(so.confirmedAtUtc) }}</div>
              </v-col>
              <v-col v-if="so.shippedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.detail.shippedAt') }}</div>
                <div>{{ formatDate(so.shippedAtUtc) }}</div>
              </v-col>
              <v-col v-if="so.completedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.detail.completedAt') }}</div>
                <div>{{ formatDate(so.completedAtUtc) }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.form.billingAddress') }}</div>
                <div>{{ so.billingStreetLine1 }}<span v-if="so.billingStreetLine2">, {{ so.billingStreetLine2 }}</span></div>
                <div>
                  {{ so.billingCity }}<span v-if="so.billingStateProvince">, {{ so.billingStateProvince }}</span> {{ so.billingPostalCode }}, {{ so.billingCountryName ?? so.billingCountryCode }}
                </div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.detail.shippingAddress') }}</div>
                <div>{{ so.shippingStreetLine1 }}<span v-if="so.shippingStreetLine2">, {{ so.shippingStreetLine2 }}</span></div>
                <div>
                  {{ so.shippingCity }}<span v-if="so.shippingStateProvince">, {{ so.shippingStateProvince }}</span> {{ so.shippingPostalCode }}, {{ so.shippingCountryName ?? so.shippingCountryCode }}
                </div>
              </v-col>
              <v-col v-if="so.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.form.notes') }}</div>
                <div class="text-body-2">{{ so.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-format-list-bulleted" class="mr-2" size="small" />
            {{ t('salesOrders.detail.lines') }}
          </div>
          <v-card-text v-if="(so.lines ?? []).length === 0" class="text-medium-emphasis">
            {{ t('salesOrders.detail.noLines') }}
          </v-card-text>
          <div v-else class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('salesOrders.lines.product') }}</th>
                  <th class="text-end">{{ t('salesOrders.lines.orderedQty') }}</th>
                  <th class="text-end">{{ t('salesOrders.lines.unitPrice') }}</th>
                  <th class="text-end">{{ t('salesOrders.lines.lineTotal') }}</th>
                  <th class="text-end">{{ t('salesOrders.lines.shippedQty') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in (so.lines ?? [])" :key="line.id">
                  <td>{{ line.productCode ? `${line.productCode} — ${line.productName}` : `#${line.productId}` }}</td>
                  <td class="text-end">{{ line.orderedQuantity }}</td>
                  <td class="text-end">{{ line.unitPrice.toFixed(2) }}</td>
                  <td class="text-end">{{ line.lineTotal.toFixed(2) }}</td>
                  <td class="text-end">{{ line.shippedQuantity }} / {{ line.orderedQuantity }}</td>
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
          {{ t('salesOrders.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getSalesOrderById } from '@features/fulfillment/api/sales-orders';
import type { SalesOrderDetailDto } from '@features/fulfillment/types/fulfillment';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  soId: number | null;
}>();

const so = ref<SalesOrderDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.soId !== null) {
    loading.value = true;
    notFound.value = false;
    so.value = null;
    try {
      so.value = await getSalesOrderById(props.soId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }
});

function statusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey', Confirmed: 'blue', Picking: 'amber', Packed: 'indigo',
    Shipped: 'teal', Completed: 'green', Cancelled: 'red',
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
  router.push({ name: 'sales-order-detail', params: { id: props.soId! } });
}
</script>

<style scoped>
.detail-dialog-content { background: #f1f5f9; }
.section-header { display: flex; align-items: center; padding: 12px 16px; font-size: 0.875rem; font-weight: 500; }
.lines-table-wrapper { max-height: 260px; overflow-y: auto; }
</style>
