<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.supplier">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.supplier.name }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.supplier.code }}</div>
        </div>
        <v-spacer />
        <StatusChip :active="vm.supplier.isActive" class="mr-2" />
        <v-btn v-if="!vm.supplier.isActive" color="success" variant="tonal" prepend-icon="mdi-domain-plus" @click="showReactivateDialog = true">
          {{ vm.t('suppliers.reactivate') }}
        </v-btn>
        <v-btn v-else color="error" variant="tonal" prepend-icon="mdi-domain-remove" @click="showDeactivateDialog = true">
          {{ vm.t('suppliers.deactivate') }}
        </v-btn>
      </div>

      <!-- Supplier Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('suppliers.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('suppliers.columns.code') }}</div>
              <div>{{ vm.supplier.code }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('suppliers.columns.taxId') }}</div>
              <div>{{ vm.supplier.taxId || '\u2014' }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('suppliers.columns.category') }}</div>
              <div>{{ vm.supplier.categoryName || '\u2014' }}</div>
            </v-col>
            <v-col v-if="vm.supplier.paymentTermDays" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('suppliers.detail.paymentTermDays') }}</div>
              <div>{{ vm.supplier.paymentTermDays }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('suppliers.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.supplier.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.supplier.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('suppliers.form.notes') }}</div>
              <div class="text-body-2">{{ vm.supplier.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Addresses -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="d-flex align-center text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-map-marker" class="mr-2" />
          {{ vm.t('suppliers.detail.addresses') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showAddressForm = true">
            {{ vm.t('common.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.supplier.addresses.length === 0" class="text-medium-emphasis">
          {{ vm.t('suppliers.detail.noAddresses') }}
        </v-card-text>
        <v-list v-else :density="vm.layout.vuetifyDensity">
          <v-list-item v-for="addr in vm.supplier.addresses" :key="addr.id">
            <template #prepend>
              <v-chip size="small" :color="addr.isDefault ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ addr.addressType }}</v-chip>
            </template>
            <v-list-item-title>{{ addr.streetLine1 }}{{ addr.streetLine2 ? ', ' + addr.streetLine2 : '' }}</v-list-item-title>
            <v-list-item-subtitle>{{ addr.city }}{{ addr.stateProvince ? ', ' + addr.stateProvince : '' }} {{ addr.postalCode }} — {{ addr.countryCode }}</v-list-item-subtitle>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="handleDeleteAddress(addr.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>

      <!-- Phones -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="d-flex align-center text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-phone" class="mr-2" />
          {{ vm.t('suppliers.detail.phones') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showPhoneForm = true">
            {{ vm.t('common.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.supplier.phones.length === 0" class="text-medium-emphasis">
          {{ vm.t('suppliers.detail.noPhones') }}
        </v-card-text>
        <v-list v-else :density="vm.layout.vuetifyDensity">
          <v-list-item v-for="phone in vm.supplier.phones" :key="phone.id">
            <template #prepend>
              <v-chip size="small" :color="phone.isPrimary ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ phone.phoneType }}</v-chip>
            </template>
            <v-list-item-title>{{ phone.phoneNumber }}{{ phone.extension ? ' ext. ' + phone.extension : '' }}</v-list-item-title>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="handleDeletePhone(phone.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>

      <!-- Emails -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="d-flex align-center text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-email" class="mr-2" />
          {{ vm.t('suppliers.detail.emails') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showEmailForm = true">
            {{ vm.t('common.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.supplier.emails.length === 0" class="text-medium-emphasis">
          {{ vm.t('suppliers.detail.noEmails') }}
        </v-card-text>
        <v-list v-else :density="vm.layout.vuetifyDensity">
          <v-list-item v-for="email in vm.supplier.emails" :key="email.id">
            <template #prepend>
              <v-chip size="small" :color="email.isPrimary ? 'primary' : 'default'" variant="tonal" class="mr-2">{{ email.emailType }}</v-chip>
            </template>
            <v-list-item-title>{{ email.emailAddress }}</v-list-item-title>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="handleDeleteEmail(email.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>

      <!-- Purchase Orders -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-clipboard-list" class="mr-2" />
          {{ vm.t('suppliers.detail.purchaseOrders') }}
        </v-card-title>
        <v-card-text v-if="supplierOrders.length === 0" class="text-medium-emphasis">
          {{ vm.t('suppliers.detail.noPurchaseOrders') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('purchaseOrders.columns.orderNumber') }}</th>
              <th>{{ vm.t('purchaseOrders.columns.status') }}</th>
              <th class="text-end">{{ vm.t('purchaseOrders.columns.totalAmount') }}</th>
              <th>{{ vm.t('purchaseOrders.columns.createdAt') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="order in supplierOrders" :key="order.id" class="cursor-pointer" @click="goToOrder(order.id)">
              <td>{{ order.orderNumber }}</td>
              <td>
                <v-chip :color="poStatusColor(order.status)" size="small" label>
                  {{ vm.t(`purchaseOrders.status.${order.status}`) }}
                </v-chip>
              </td>
              <td class="text-end">{{ order.totalAmount.toFixed(2) }}</td>
              <td>{{ vm.formatDate(order.createdAtUtc) }}</td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Add Address Dialog -->
    <FormWrapper v-model="showAddressForm" max-width="500" :title="vm.t('suppliers.detail.addAddress')" icon="mdi-map-marker-plus">
      <v-card-text>
        <v-form ref="addressFormRef" @submit.prevent="submitAddress">
          <v-select v-model="addressForm.addressType" :label="vm.t('suppliers.detail.addressType')" :items="['Billing', 'Shipping', 'Both']" :density="vm.layout.vuetifyDensity" :rules="[requiredRule]" />
          <v-text-field v-model="addressForm.streetLine1" :label="vm.t('suppliers.detail.street1')" :density="vm.layout.vuetifyDensity" :rules="[requiredRule]" />
          <v-text-field v-model="addressForm.streetLine2" :label="vm.t('suppliers.detail.street2')" :density="vm.layout.vuetifyDensity" />
          <NomenclatureAddressFields
            v-model:country-code="addressForm.countryCode"
            v-model:state-province="addressForm.stateProvince"
            v-model:city="addressForm.city"
            :density="vm.layout.vuetifyDensity"
          />
          <v-text-field v-model="addressForm.postalCode" :label="vm.t('suppliers.detail.postalCode')" :density="vm.layout.vuetifyDensity" :rules="[requiredRule]" />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showAddressForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitAddress">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Add Phone Dialog -->
    <FormWrapper v-model="showPhoneForm" max-width="400" :title="vm.t('suppliers.detail.addPhone')" icon="mdi-phone-plus">
      <v-card-text>
        <v-form ref="phoneFormRef" @submit.prevent="submitPhone">
          <v-select v-model="phoneForm.phoneType" :label="vm.t('suppliers.detail.phoneType')" :items="['Mobile', 'Landline', 'Fax']" :density="vm.layout.vuetifyDensity" :rules="[requiredRule]" />
          <v-text-field v-model="phoneForm.phoneNumber" :label="vm.t('suppliers.detail.phoneNumber')" :density="vm.layout.vuetifyDensity" :rules="[requiredRule]" />
          <v-text-field v-model="phoneForm.extension" :label="vm.t('suppliers.detail.extension')" :density="vm.layout.vuetifyDensity" />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showPhoneForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitPhone">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Add Email Dialog -->
    <FormWrapper v-model="showEmailForm" max-width="400" :title="vm.t('suppliers.detail.addEmail')" icon="mdi-email-plus">
      <v-card-text>
        <v-form ref="emailFormRef" @submit.prevent="submitEmail">
          <v-select v-model="emailForm.emailType" :label="vm.t('suppliers.detail.emailType')" :items="['General', 'Billing', 'Support']" :density="vm.layout.vuetifyDensity" :rules="[requiredRule]" />
          <v-text-field v-model="emailForm.emailAddress" :label="vm.t('suppliers.detail.emailAddress')" type="email" :density="vm.layout.vuetifyDensity" :rules="[requiredRule, emailRule]" />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showEmailForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitEmail">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Deactivate Confirmation -->
    <ConfirmDialog
      v-model="showDeactivateDialog"
      :title="vm.t('suppliers.deactivate')"
      :message="vm.t('suppliers.deactivateConfirm', { name: vm.supplier?.name })"
      :confirm-text="vm.t('suppliers.deactivate')"
      color="error"
      icon="mdi-domain-remove"
      :loading="deactivating"
      @confirm="handleDeactivate"
    />

    <!-- Reactivate Confirmation -->
    <ConfirmDialog
      v-model="showReactivateDialog"
      :title="vm.t('suppliers.reactivate')"
      :message="vm.t('suppliers.reactivateConfirm', { name: vm.supplier?.name })"
      :confirm-text="vm.t('suppliers.reactivate')"
      color="success"
      icon="mdi-domain-plus"
      :loading="reactivating"
      @confirm="handleReactivate"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useSupplierDetailView } from '@features/purchasing/composables/useSupplierDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import { searchPurchaseOrders } from '@features/purchasing/api/purchase-orders';
import type { PurchaseOrderDto } from '@features/purchasing/types/purchasing';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import NomenclatureAddressFields from '@shared/components/molecules/NomenclatureAddressFields.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

const router = useRouter();
const notification = useNotificationStore();
const vm = reactive(useSupplierDetailView());

const showDeactivateDialog = ref(false);
const showReactivateDialog = ref(false);
const deactivating = ref(false);
const reactivating = ref(false);

const showAddressForm = ref(false);
const showPhoneForm = ref(false);
const showEmailForm = ref(false);

const addressFormRef = ref();
const phoneFormRef = ref();
const emailFormRef = ref();

const supplierOrders = ref<PurchaseOrderDto[]>([]);

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

const requiredRule = (v: string) => !!v || vm.t('common.required');
const emailRule = (v: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) || vm.t('validation.emailInvalid');

onMounted(async () => {
  try {
    const response = await searchPurchaseOrders({
      supplierId: vm.supplierId,
      sortDescending: true,
      page: 1,
      pageSize: 100,
    });
    supplierOrders.value = response.items;
  } catch {
    // silent — purchase orders tab will be empty
  }
});

function poStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    PartiallyReceived: 'orange',
    Received: 'green',
    Closed: 'blue-grey',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}

function goToOrder(orderId: number): void {
  router.push({ name: 'purchase-order-detail', params: { id: orderId } });
}

async function handleDeactivate(): Promise<void> {
  deactivating.value = true;
  try {
    await vm.handleDeactivate();
    showDeactivateDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    deactivating.value = false;
  }
}

async function handleReactivate(): Promise<void> {
  reactivating.value = true;
  try {
    await vm.handleReactivate();
    showReactivateDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    reactivating.value = false;
  }
}

async function handleDeleteAddress(addressId: number): Promise<void> {
  try {
    await vm.handleDeleteAddress(addressId);
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

async function handleDeletePhone(phoneId: number): Promise<void> {
  try {
    await vm.handleDeletePhone(phoneId);
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

async function handleDeleteEmail(emailId: number): Promise<void> {
  try {
    await vm.handleDeleteEmail(emailId);
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

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
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
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
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
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
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
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
</script>
