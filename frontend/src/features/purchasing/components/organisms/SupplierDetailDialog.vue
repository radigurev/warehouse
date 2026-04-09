<template>
  <FormWrapper v-model="visible" max-width="700" :title="supplier?.name ?? t('suppliers.viewDetails')" icon="mdi-domain">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="supplier">
      <v-card-text class="detail-dialog-content pt-4">
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ supplier.name }}</div>
            <div class="text-caption text-medium-emphasis">{{ supplier.code }}</div>
          </div>
          <v-spacer />
          <StatusChip :active="supplier.isActive" />
        </div>

        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('suppliers.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col v-if="supplier.taxId" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('suppliers.form.taxId') }}</div>
                <div>{{ supplier.taxId }}</div>
              </v-col>
              <v-col v-if="supplier.categoryName" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('suppliers.form.category') }}</div>
                <div>{{ supplier.categoryName }}</div>
              </v-col>
              <v-col v-if="supplier.paymentTermDays !== null" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('suppliers.detail.paymentTermDays') }}</div>
                <div>{{ supplier.paymentTermDays }} days</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('suppliers.columns.createdAt') }}</div>
                <div>{{ formatDate(supplier.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="supplier.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('suppliers.form.notes') }}</div>
                <div class="text-body-2">{{ supplier.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card v-if="supplier.addresses.length > 0" class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-map-marker" class="mr-2" size="small" />
            {{ t('suppliers.detail.addresses') }} ({{ supplier.addresses.length }})
          </div>
          <v-list density="compact">
            <v-list-item v-for="addr in supplier.addresses" :key="addr.id">
              <v-list-item-title>{{ addr.streetLine1 }}, {{ addr.city }}, {{ addr.countryCode }}</v-list-item-title>
              <v-list-item-subtitle>{{ addr.addressType }}</v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-card>

        <v-card v-if="supplier.phones.length > 0" class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-phone" class="mr-2" size="small" />
            {{ t('suppliers.detail.phones') }} ({{ supplier.phones.length }})
          </div>
          <v-list density="compact">
            <v-list-item v-for="phone in supplier.phones" :key="phone.id">
              <v-list-item-title>{{ phone.phoneNumber }}</v-list-item-title>
              <v-list-item-subtitle>{{ phone.phoneType }}</v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-card>

        <v-card v-if="supplier.emails.length > 0" class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-email" class="mr-2" size="small" />
            {{ t('suppliers.detail.emails') }} ({{ supplier.emails.length }})
          </div>
          <v-list density="compact">
            <v-list-item v-for="email in supplier.emails" :key="email.id">
              <v-list-item-title>{{ email.emailAddress }}</v-list-item-title>
              <v-list-item-subtitle>{{ email.emailType }}</v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-card>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="visible = false">{{ t('common.close') }}</v-btn>
        <v-btn color="primary" variant="flat" prepend-icon="mdi-open-in-new" @click="openFullPage">
          {{ t('suppliers.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getSupplierById } from '@features/purchasing/api/suppliers';
import type { SupplierDetailDto } from '@features/purchasing/types/purchasing';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import StatusChip from '@shared/components/atoms/StatusChip.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  supplierId: number | null;
}>();

const supplier = ref<SupplierDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.supplierId !== null) {
    loading.value = true;
    notFound.value = false;
    supplier.value = null;
    try {
      supplier.value = await getSupplierById(props.supplierId);
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
  router.push({ name: 'supplier-detail', params: { id: props.supplierId! } });
}
</script>

<style scoped>
.detail-dialog-content { background: #f1f5f9; }
.section-header { display: flex; align-items: center; padding: 12px 16px; font-size: 0.875rem; font-weight: 500; }
</style>
