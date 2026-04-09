<template>
  <FormWrapper v-model="visible" max-width="700" :title="ret?.returnNumber ?? t('supplierReturns.viewDetails')" icon="mdi-package-up">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="ret">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ ret.returnNumber }}</div>
            <div class="text-caption text-medium-emphasis">{{ ret.supplierName }}</div>
          </div>
          <v-spacer />
          <v-chip :color="statusColor(ret.status)" size="small" label>
            {{ t(`supplierReturns.status.${ret.status}`) }}
          </v-chip>
        </div>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('supplierReturns.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('supplierReturns.form.reason') }}</div>
                <div>{{ ret.reason }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('supplierReturns.columns.createdAt') }}</div>
                <div>{{ formatDate(ret.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="ret.confirmedAtUtc" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('supplierReturns.detail.confirmedAt') }}</div>
                <div>{{ formatDate(ret.confirmedAtUtc) }}</div>
              </v-col>
              <v-col v-if="ret.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('supplierReturns.form.notes') }}</div>
                <div class="text-body-2">{{ ret.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-format-list-bulleted" class="mr-2" size="small" />
            {{ t('supplierReturns.detail.lines') }}
          </div>
          <div class="lines-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('supplierReturns.lines.product') }}</th>
                  <th class="text-end">{{ t('supplierReturns.lines.quantity') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in ret.lines" :key="line.id">
                  <td>{{ line.productId }}</td>
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
          {{ t('supplierReturns.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getSupplierReturnById } from '@features/purchasing/api/supplier-returns';
import type { SupplierReturnDetailDto } from '@features/purchasing/types/purchasing';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  returnId: number | null;
}>();

const ret = ref<SupplierReturnDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.returnId !== null) {
    loading.value = true;
    notFound.value = false;
    ret.value = null;
    try {
      ret.value = await getSupplierReturnById(props.returnId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }
});

function statusColor(status: string): string {
  const map: Record<string, string> = { Draft: 'grey', Confirmed: 'blue', Cancelled: 'red' };
  return map[status] || 'grey';
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
    year: 'numeric', month: 'short', day: 'numeric',
  });
}

function openFullPage(): void {
  visible.value = false;
  router.push({ name: 'supplier-return-detail', params: { id: props.returnId! } });
}
</script>

<style scoped>
.detail-dialog-content { background: #f1f5f9; }
.section-header { display: flex; align-items: center; padding: 12px 16px; font-size: 0.875rem; font-weight: 500; }
.lines-table-wrapper { max-height: 250px; overflow-y: auto; }
</style>
