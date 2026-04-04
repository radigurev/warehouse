<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.customer">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.customer.name }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.customer.code }}</div>
        </div>
        <v-spacer />
        <StatusChip :active="vm.customer.isActive" class="mr-2" />
        <v-btn v-if="!vm.customer.isActive" color="success" variant="tonal" prepend-icon="mdi-account-check" @click="vm.handleReactivate">
          {{ vm.t('customers.reactivate') }}
        </v-btn>
      </div>

      <!-- Customer Info Card -->
      <v-card class="mb-4">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('customers.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customers.form.taxId') }}</div>
              <div>{{ vm.customer.taxId || '—' }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customers.form.category') }}</div>
              <div>{{ vm.customer.categoryName || '—' }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customers.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.customer.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.customer.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customers.form.notes') }}</div>
              <div class="text-body-2">{{ vm.customer.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Addresses -->
      <v-card class="mb-4">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-map-marker" class="mr-2" />
          {{ vm.t('customers.detail.addresses') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showAddressForm = true">
            {{ vm.t('common.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.customer.addresses.length === 0" class="text-medium-emphasis">
          {{ vm.t('customers.detail.noAddresses') }}
        </v-card-text>
        <v-list v-else density="compact">
          <v-list-item v-for="addr in vm.customer.addresses" :key="addr.id">
            <template #prepend>
              <v-chip size="small" :color="addr.isDefault ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ addr.addressType }}</v-chip>
            </template>
            <v-list-item-title>{{ addr.streetLine1 }}{{ addr.streetLine2 ? ', ' + addr.streetLine2 : '' }}</v-list-item-title>
            <v-list-item-subtitle>{{ addr.city }}{{ addr.stateProvince ? ', ' + addr.stateProvince : '' }} {{ addr.postalCode }} — {{ addr.countryCode }}</v-list-item-subtitle>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="vm.handleDeleteAddress(addr.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>

      <!-- Phones -->
      <v-card class="mb-4">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-phone" class="mr-2" />
          {{ vm.t('customers.detail.phones') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showPhoneForm = true">
            {{ vm.t('common.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.customer.phones.length === 0" class="text-medium-emphasis">
          {{ vm.t('customers.detail.noPhones') }}
        </v-card-text>
        <v-list v-else density="compact">
          <v-list-item v-for="phone in vm.customer.phones" :key="phone.id">
            <template #prepend>
              <v-chip size="small" :color="phone.isPrimary ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ phone.phoneType }}</v-chip>
            </template>
            <v-list-item-title>{{ phone.phoneNumber }}{{ phone.extension ? ' ext. ' + phone.extension : '' }}</v-list-item-title>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="vm.handleDeletePhone(phone.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>

      <!-- Emails -->
      <v-card class="mb-4">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-email" class="mr-2" />
          {{ vm.t('customers.detail.emails') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showEmailForm = true">
            {{ vm.t('common.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.customer.emails.length === 0" class="text-medium-emphasis">
          {{ vm.t('customers.detail.noEmails') }}
        </v-card-text>
        <v-list v-else density="compact">
          <v-list-item v-for="email in vm.customer.emails" :key="email.id">
            <template #prepend>
              <v-chip size="small" :color="email.isPrimary ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ email.emailType }}</v-chip>
            </template>
            <v-list-item-title>{{ email.emailAddress }}</v-list-item-title>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="vm.handleDeleteEmail(email.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>

      <!-- Accounts -->
      <v-card class="mb-4">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-bank" class="mr-2" />
          {{ vm.t('customers.detail.accounts') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showAccountForm = true">
            {{ vm.t('common.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.customer.accounts.length === 0" class="text-medium-emphasis">
          {{ vm.t('customers.detail.noAccounts') }}
        </v-card-text>
        <v-list v-else density="compact">
          <v-list-item v-for="acc in vm.customer.accounts" :key="acc.id">
            <template #prepend>
              <v-chip size="small" :color="acc.isPrimary ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ acc.currencyCode }}</v-chip>
            </template>
            <v-list-item-title>{{ acc.balance.toFixed(2) }} {{ acc.currencyCode }}</v-list-item-title>
            <v-list-item-subtitle v-if="acc.description">{{ acc.description }}</v-list-item-subtitle>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="vm.handleDeactivateAccount(acc.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>
    </template>

    <!-- Add Address Dialog -->
    <v-dialog v-model="showAddressForm" max-width="500" persistent>
      <v-card>
        <v-card-title>{{ vm.t('customers.detail.addAddress') }}</v-card-title>
        <v-card-text>
          <v-form ref="addressFormRef" @submit.prevent="submitAddress">
            <v-select v-model="addressForm.addressType" :label="vm.t('customers.detail.addressType')" :items="['Billing', 'Shipping', 'Both']" :rules="[requiredRule]" />
            <v-text-field v-model="addressForm.streetLine1" :label="vm.t('customers.detail.street1')" :rules="[requiredRule]" />
            <v-text-field v-model="addressForm.streetLine2" :label="vm.t('customers.detail.street2')" />
            <v-text-field v-model="addressForm.city" :label="vm.t('customers.detail.city')" :rules="[requiredRule]" />
            <v-text-field v-model="addressForm.stateProvince" :label="vm.t('customers.detail.state')" />
            <v-text-field v-model="addressForm.postalCode" :label="vm.t('customers.detail.postalCode')" :rules="[requiredRule]" />
            <v-text-field v-model="addressForm.countryCode" :label="vm.t('customers.detail.countryCode')" :rules="[requiredRule, countryCodeRule]" maxlength="2" />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showAddressForm = false">{{ vm.t('common.cancel') }}</v-btn>
          <v-btn color="primary" variant="flat" @click="submitAddress">{{ vm.t('common.save') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Add Phone Dialog -->
    <v-dialog v-model="showPhoneForm" max-width="400" persistent>
      <v-card>
        <v-card-title>{{ vm.t('customers.detail.addPhone') }}</v-card-title>
        <v-card-text>
          <v-form ref="phoneFormRef" @submit.prevent="submitPhone">
            <v-select v-model="phoneForm.phoneType" :label="vm.t('customers.detail.phoneType')" :items="['Mobile', 'Landline', 'Fax']" :rules="[requiredRule]" />
            <v-text-field v-model="phoneForm.phoneNumber" :label="vm.t('customers.detail.phoneNumber')" :rules="[requiredRule]" />
            <v-text-field v-model="phoneForm.extension" :label="vm.t('customers.detail.extension')" />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showPhoneForm = false">{{ vm.t('common.cancel') }}</v-btn>
          <v-btn color="primary" variant="flat" @click="submitPhone">{{ vm.t('common.save') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Add Email Dialog -->
    <v-dialog v-model="showEmailForm" max-width="400" persistent>
      <v-card>
        <v-card-title>{{ vm.t('customers.detail.addEmail') }}</v-card-title>
        <v-card-text>
          <v-form ref="emailFormRef" @submit.prevent="submitEmail">
            <v-select v-model="emailForm.emailType" :label="vm.t('customers.detail.emailType')" :items="['General', 'Billing', 'Support']" :rules="[requiredRule]" />
            <v-text-field v-model="emailForm.emailAddress" :label="vm.t('customers.detail.emailAddress')" type="email" :rules="[requiredRule, emailRule]" />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showEmailForm = false">{{ vm.t('common.cancel') }}</v-btn>
          <v-btn color="primary" variant="flat" @click="submitEmail">{{ vm.t('common.save') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Add Account Dialog -->
    <v-dialog v-model="showAccountForm" max-width="400" persistent>
      <v-card>
        <v-card-title>{{ vm.t('customers.detail.addAccount') }}</v-card-title>
        <v-card-text>
          <v-form ref="accountFormRef" @submit.prevent="submitAccount">
            <v-text-field v-model="accountForm.currencyCode" :label="vm.t('customers.detail.currencyCode')" :rules="[requiredRule, currencyCodeRule]" maxlength="3" />
            <v-text-field v-model="accountForm.description" :label="vm.t('customers.detail.accountDescription')" />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showAccountForm = false">{{ vm.t('common.cancel') }}</v-btn>
          <v-btn color="primary" variant="flat" @click="submitAccount">{{ vm.t('common.save') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useCustomerDetailView } from '@features/customers/composables/useCustomerDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import StatusChip from '@shared/components/atoms/StatusChip.vue';

const notification = useNotificationStore();
const vm = reactive(useCustomerDetailView());

// --- Inline form state ---
const showAddressForm = ref(false);
const showPhoneForm = ref(false);
const showEmailForm = ref(false);
const showAccountForm = ref(false);

const addressFormRef = ref();
const phoneFormRef = ref();
const emailFormRef = ref();
const accountFormRef = ref();

const addressForm = reactive({
  addressType: 'Billing',
  streetLine1: '',
  streetLine2: '',
  city: '',
  stateProvince: '',
  postalCode: '',
  countryCode: '',
});

const phoneForm = reactive({
  phoneType: 'Mobile',
  phoneNumber: '',
  extension: '',
});

const emailForm = reactive({
  emailType: 'General',
  emailAddress: '',
});

const accountForm = reactive({
  currencyCode: '',
  description: '',
});

const requiredRule = (v: string) => !!v || vm.t('common.required');
const countryCodeRule = (v: string) => /^[A-Z]{2}$/.test(v) || vm.t('validation.countryCodeFormat');
const currencyCodeRule = (v: string) => /^[A-Z]{3}$/.test(v) || vm.t('validation.currencyCodeFormat');
const emailRule = (v: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) || vm.t('validation.emailInvalid');

async function submitAddress(): Promise<void> {
  const { valid } = await addressFormRef.value.validate();
  if (!valid) return;
  try {
    await vm.handleCreateAddress({
      addressType: addressForm.addressType,
      streetLine1: addressForm.streetLine1,
      streetLine2: addressForm.streetLine2 || null,
      city: addressForm.city,
      stateProvince: addressForm.stateProvince || null,
      postalCode: addressForm.postalCode,
      countryCode: addressForm.countryCode,
    });
    showAddressForm.value = false;
    resetAddressForm();
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  }
}

async function submitPhone(): Promise<void> {
  const { valid } = await phoneFormRef.value.validate();
  if (!valid) return;
  try {
    await vm.handleCreatePhone({
      phoneType: phoneForm.phoneType,
      phoneNumber: phoneForm.phoneNumber,
      extension: phoneForm.extension || null,
    });
    showPhoneForm.value = false;
    resetPhoneForm();
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  }
}

async function submitEmail(): Promise<void> {
  const { valid } = await emailFormRef.value.validate();
  if (!valid) return;
  try {
    await vm.handleCreateEmail({
      emailType: emailForm.emailType,
      emailAddress: emailForm.emailAddress,
    });
    showEmailForm.value = false;
    resetEmailForm();
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  }
}

async function submitAccount(): Promise<void> {
  const { valid } = await accountFormRef.value.validate();
  if (!valid) return;
  try {
    await vm.handleCreateAccount({
      currencyCode: accountForm.currencyCode,
      description: accountForm.description || null,
    });
    showAccountForm.value = false;
    resetAccountForm();
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  }
}

function resetAddressForm(): void {
  addressForm.addressType = 'Billing';
  addressForm.streetLine1 = '';
  addressForm.streetLine2 = '';
  addressForm.city = '';
  addressForm.stateProvince = '';
  addressForm.postalCode = '';
  addressForm.countryCode = '';
}

function resetPhoneForm(): void {
  phoneForm.phoneType = 'Mobile';
  phoneForm.phoneNumber = '';
  phoneForm.extension = '';
}

function resetEmailForm(): void {
  emailForm.emailType = 'General';
  emailForm.emailAddress = '';
}

function resetAccountForm(): void {
  accountForm.currencyCode = '';
  accountForm.description = '';
}
</script>
