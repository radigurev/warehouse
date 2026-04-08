<template>
  <FormWrapper v-model="visible" max-width="750" :title="customer?.name ?? t('customers.viewDetails')" icon="mdi-account-details">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="customer">
      <v-card-text class="detail-dialog-content pt-4">
        <!-- Header -->
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ customer.name }}</div>
            <div class="text-caption text-medium-emphasis">{{ customer.code }}</div>
          </div>
          <v-spacer />
          <StatusChip :active="customer.isActive" />
        </div>

        <!-- Customer Info -->
        <v-card class="mb-3">
          <v-card-title class="text-subtitle-1 font-weight-medium">
            <v-icon icon="mdi-information" class="mr-2" />
            {{ t('customers.detail.info') }}
          </v-card-title>
          <v-card-text>
            <v-row dense>
              <v-col v-if="customer.nativeLanguageName" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('customers.form.nativeLanguageName') }}</div>
                <div>{{ customer.nativeLanguageName }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('customers.form.taxId') }}</div>
                <div>{{ customer.taxId || '—' }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('customers.form.category') }}</div>
                <div>{{ customer.categoryName || '—' }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('customers.columns.createdAt') }}</div>
                <div>{{ formatDate(customer.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="customer.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('customers.form.notes') }}</div>
                <div class="text-body-2">{{ customer.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <!-- Addresses -->
        <v-card class="mb-3">
          <v-card-title class="text-subtitle-1 font-weight-medium">
            <v-icon icon="mdi-map-marker" class="mr-2" />
            {{ t('customers.detail.addresses') }} ({{ customer.addresses.length }})
          </v-card-title>
          <v-card-text v-if="customer.addresses.length === 0" class="text-medium-emphasis">
            {{ t('customers.detail.noAddresses') }}
          </v-card-text>
          <v-list v-else density="compact">
            <v-list-item v-for="addr in customer.addresses" :key="addr.id">
              <template #prepend>
                <v-chip size="small" :color="addr.isDefault ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ addr.addressType }}</v-chip>
              </template>
              <v-list-item-title>{{ addr.streetLine1 }}{{ addr.streetLine2 ? ', ' + addr.streetLine2 : '' }}</v-list-item-title>
              <v-list-item-subtitle>{{ addr.city }}{{ addr.stateProvince ? ', ' + addr.stateProvince : '' }} {{ addr.postalCode }} — {{ addr.countryCode }}</v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-card>

        <!-- Phones -->
        <v-card class="mb-3">
          <v-card-title class="text-subtitle-1 font-weight-medium">
            <v-icon icon="mdi-phone" class="mr-2" />
            {{ t('customers.detail.phones') }} ({{ customer.phones.length }})
          </v-card-title>
          <v-card-text v-if="customer.phones.length === 0" class="text-medium-emphasis">
            {{ t('customers.detail.noPhones') }}
          </v-card-text>
          <v-list v-else density="compact">
            <v-list-item v-for="phone in customer.phones" :key="phone.id">
              <template #prepend>
                <v-chip size="small" :color="phone.isPrimary ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ phone.phoneType }}</v-chip>
              </template>
              <v-list-item-title>{{ phone.phoneNumber }}{{ phone.extension ? ' ext. ' + phone.extension : '' }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card>

        <!-- Emails -->
        <v-card class="mb-3">
          <v-card-title class="text-subtitle-1 font-weight-medium">
            <v-icon icon="mdi-email" class="mr-2" />
            {{ t('customers.detail.emails') }} ({{ customer.emails.length }})
          </v-card-title>
          <v-card-text v-if="customer.emails.length === 0" class="text-medium-emphasis">
            {{ t('customers.detail.noEmails') }}
          </v-card-text>
          <v-list v-else density="compact">
            <v-list-item v-for="email in customer.emails" :key="email.id">
              <template #prepend>
                <v-chip size="small" :color="email.isPrimary ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ email.emailType }}</v-chip>
              </template>
              <v-list-item-title>{{ email.emailAddress }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card>

        <!-- Accounts -->
        <v-card class="mb-3">
          <v-card-title class="text-subtitle-1 font-weight-medium">
            <v-icon icon="mdi-bank" class="mr-2" />
            {{ t('customers.detail.accounts') }} ({{ customer.accounts.length }})
          </v-card-title>
          <v-card-text v-if="customer.accounts.length === 0" class="text-medium-emphasis">
            {{ t('customers.detail.noAccounts') }}
          </v-card-text>
          <v-list v-else density="compact">
            <v-list-item v-for="acc in customer.accounts" :key="acc.id">
              <template #prepend>
                <v-chip size="small" :color="acc.isPrimary ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ acc.currencyCode }}</v-chip>
              </template>
              <v-list-item-title>{{ acc.balance.toFixed(2) }} {{ acc.currencyCode }}</v-list-item-title>
              <v-list-item-subtitle v-if="acc.description">{{ acc.description }}</v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-card>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="visible = false">{{ t('common.close') }}</v-btn>
        <v-btn color="primary" variant="flat" prepend-icon="mdi-open-in-new" @click="openFullPage">
          {{ t('customers.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getCustomerById } from '@features/customers/api/customers';
import type { CustomerDetailDto } from '@features/customers/types/customer';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import StatusChip from '@shared/components/atoms/StatusChip.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  customerId: number | null;
}>();

const customer = ref<CustomerDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.customerId) {
    loading.value = true;
    notFound.value = false;
    customer.value = null;
    try {
      customer.value = await getCustomerById(props.customerId);
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

function openFullPage(): void {
  visible.value = false;
  router.push({ name: 'customer-detail', params: { id: props.customerId! } });
}
</script>

<style scoped>
.detail-dialog-content {
  background: #f1f5f9;
}
</style>
