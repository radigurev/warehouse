<template>
  <FormWrapper v-model="visible" max-width="800" :title="carrier?.name ?? t('carriers.viewDetails')" icon="mdi-truck-delivery">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="carrier">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ carrier.name }}</div>
            <div class="text-caption text-medium-emphasis">{{ carrier.code }}</div>
          </div>
          <v-spacer />
          <v-chip :color="carrier.isActive ? 'success' : 'grey'" size="small" label>
            {{ carrier.isActive ? t('common.active') : t('common.inactive') }}
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
                <div class="text-caption text-medium-emphasis">{{ t('carriers.form.code') }}</div>
                <div>{{ carrier.code }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('carriers.form.name') }}</div>
                <div>{{ carrier.name }}</div>
              </v-col>
              <v-col v-if="carrier.contactPhone" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('carriers.form.contactPhone') }}</div>
                <div>{{ carrier.contactPhone }}</div>
              </v-col>
              <v-col v-if="carrier.contactEmail" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('carriers.form.contactEmail') }}</div>
                <div>{{ carrier.contactEmail }}</div>
              </v-col>
              <v-col v-if="carrier.websiteUrl" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('carriers.form.websiteUrl') }}</div>
                <div><a :href="carrier.websiteUrl" target="_blank" rel="noopener">{{ carrier.websiteUrl }}</a></div>
              </v-col>
              <v-col v-if="carrier.trackingUrlTemplate" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('carriers.form.trackingUrlTemplate') }}</div>
                <div class="text-body-2">{{ carrier.trackingUrlTemplate }}</div>
              </v-col>
              <v-col v-if="carrier.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('carriers.form.notes') }}</div>
                <div class="text-body-2">{{ carrier.notes }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('carriers.columns.createdAtUtc') }}</div>
                <div>{{ formatDate(carrier.createdAtUtc) }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-speedometer" class="mr-2" size="small" />
            {{ t('carriers.serviceLevels.title') }}
          </div>
          <v-card-text v-if="carrier.serviceLevels.length === 0" class="text-medium-emphasis">
            {{ t('salesOrders.detail.noLines') }}
          </v-card-text>
          <div v-else class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('carriers.serviceLevels.columns.code') }}</th>
                  <th>{{ t('carriers.serviceLevels.columns.name') }}</th>
                  <th class="text-end">{{ t('carriers.serviceLevels.columns.estimatedDeliveryDays') }}</th>
                  <th class="text-end">{{ t('carriers.serviceLevels.columns.baseRate') }}</th>
                  <th class="text-end">{{ t('carriers.serviceLevels.columns.perKgRate') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="level in carrier.serviceLevels" :key="level.id">
                  <td>{{ level.code }}</td>
                  <td>{{ level.name }}</td>
                  <td class="text-end">{{ level.estimatedDeliveryDays ?? '—' }}</td>
                  <td class="text-end">{{ level.baseRate?.toFixed(2) ?? '—' }}</td>
                  <td class="text-end">{{ level.perKgRate?.toFixed(2) ?? '—' }}</td>
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
          {{ t('carriers.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getCarrierById } from '@features/fulfillment/api/carriers';
import type { CarrierDetailDto } from '@features/fulfillment/types/fulfillment';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  carrierId: number | null;
}>();

const carrier = ref<CarrierDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.carrierId !== null) {
    loading.value = true;
    notFound.value = false;
    carrier.value = null;
    try {
      carrier.value = await getCarrierById(props.carrierId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }
});

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
    year: 'numeric', month: 'short', day: 'numeric',
  });
}

function openFullPage(): void {
  visible.value = false;
  router.push({ name: 'carrier-detail', params: { id: props.carrierId! } });
}
</script>

<style scoped>
.detail-dialog-content { background: #f1f5f9; }
.section-header { display: flex; align-items: center; padding: 12px 16px; font-size: 0.875rem; font-weight: 500; }
.lines-table-wrapper { max-height: 260px; overflow-y: auto; }
</style>
