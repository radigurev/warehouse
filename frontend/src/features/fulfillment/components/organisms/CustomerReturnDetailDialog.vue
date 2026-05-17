<template>
  <FormWrapper v-model="visible" max-width="900" :title="ret?.returnNumber ?? t('customerReturns.viewDetails')" icon="mdi-package-down">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="ret">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ ret.returnNumber }}</div>
            <div class="text-caption text-medium-emphasis">{{ ret.customerName }}</div>
          </div>
          <v-spacer />
          <v-chip :color="statusColor(ret.status)" size="small" label>
            {{ t(`customerReturns.status.${ret.status}`) }}
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
                <div class="text-caption text-medium-emphasis">{{ t('customerReturns.columns.reason') }}</div>
                <div>{{ ret.reason }}</div>
              </v-col>
              <v-col v-if="ret.salesOrderNumber" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('customerReturns.columns.salesOrderNumber') }}</div>
                <div>{{ ret.salesOrderNumber }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('customerReturns.columns.createdAtUtc') }}</div>
                <div>{{ formatDate(ret.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="ret.confirmedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('salesOrders.detail.confirmedAt') }}</div>
                <div>{{ formatDate(ret.confirmedAtUtc) }}</div>
              </v-col>
              <v-col v-if="ret.receivedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('customerReturns.receive') }}</div>
                <div>{{ formatDate(ret.receivedAtUtc) }}</div>
              </v-col>
              <v-col v-if="ret.closedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('customerReturns.close') }}</div>
                <div>{{ formatDate(ret.closedAtUtc) }}</div>
              </v-col>
              <v-col v-if="ret.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('customerReturns.form.notes') }}</div>
                <div class="text-body-2">{{ ret.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-format-list-bulleted" class="mr-2" size="small" />
            {{ t('customerReturns.detail.lines') }}
          </div>
          <v-card-text v-if="ret.lines.length === 0" class="text-medium-emphasis">
            {{ t('customerReturns.detail.noLines') }}
          </v-card-text>
          <div v-else class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('customerReturns.lines.product') }}</th>
                  <th>{{ t('customerReturns.lines.warehouse') }}</th>
                  <th>{{ t('customerReturns.lines.location') }}</th>
                  <th class="text-end">{{ t('customerReturns.lines.quantity') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in ret.lines" :key="line.id">
                  <td>{{ line.productCode }} — {{ line.productName }}</td>
                  <td>{{ line.warehouseName }}</td>
                  <td>{{ line.locationCode ?? '—' }}</td>
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
          {{ t('customerReturns.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getCustomerReturnById } from '@features/fulfillment/api/customer-returns';
import type { CustomerReturnDetailDto } from '@features/fulfillment/types/fulfillment';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  returnId: number | null;
}>();

const ret = ref<CustomerReturnDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.returnId !== null) {
    loading.value = true;
    notFound.value = false;
    ret.value = null;
    try {
      ret.value = await getCustomerReturnById(props.returnId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }
});

function statusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey', Confirmed: 'blue', Received: 'amber', Closed: 'green', Cancelled: 'red',
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
  router.push({ name: 'customer-return-detail', params: { id: props.returnId! } });
}
</script>

<style scoped>
.detail-dialog-content { background: #f1f5f9; }
.section-header { display: flex; align-items: center; padding: 12px 16px; font-size: 0.875rem; font-weight: 500; }
.lines-table-wrapper { max-height: 260px; overflow-y: auto; }
</style>
