import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getCustomerById, reactivateCustomer, deactivateCustomer } from '@features/customers/api/customers';
import {
  createAddress, updateAddress, deleteAddress,
  createPhone, updatePhone, deletePhone,
  createEmail, updateEmail, deleteEmail,
  createAccount, updateAccount, deactivateAccount, mergeAccounts,
} from '@features/customers/api/contacts';
import type {
  CustomerDetailDto,
  CreateAddressRequest, UpdateAddressRequest,
  CreatePhoneRequest, UpdatePhoneRequest,
  CreateEmailRequest, UpdateEmailRequest,
  CreateAccountRequest, UpdateAccountRequest,
  MergeAccountsRequest,
} from '@features/customers/types/customer';

export function useCustomerDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const customer = ref<CustomerDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const customerId = Number(route.params.id);

  onMounted(() => loadCustomer());

  async function loadCustomer(): Promise<void> {
    if (!customerId || customerId <= 0) {
      router.push({ name: 'customers' });
      return;
    }
    loading.value = true;
    try {
      customer.value = await getCustomerById(customerId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  function goBack(): void {
    router.push({ name: 'customers' });
  }

  async function handleReactivate(): Promise<void> {
    try {
      customer.value = await reactivateCustomer(customerId);
      notification.success(t('customers.reactivated') + ' \u2713');
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  async function handleDeactivate(): Promise<void> {
    try {
      await deactivateCustomer(customerId);
      notification.success(t('customers.deactivated') + ' \u2713');
      await loadCustomer();
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  // --- Address CRUD ---
  async function handleCreateAddress(request: CreateAddressRequest): Promise<void> {
    await createAddress(customerId, request);
    await loadCustomer();
  }

  async function handleUpdateAddress(addressId: number, request: UpdateAddressRequest): Promise<void> {
    await updateAddress(customerId, addressId, request);
    await loadCustomer();
  }

  async function handleDeleteAddress(addressId: number): Promise<void> {
    await deleteAddress(customerId, addressId);
    await loadCustomer();
  }

  // --- Phone CRUD ---
  async function handleCreatePhone(request: CreatePhoneRequest): Promise<void> {
    await createPhone(customerId, request);
    await loadCustomer();
  }

  async function handleUpdatePhone(phoneId: number, request: UpdatePhoneRequest): Promise<void> {
    await updatePhone(customerId, phoneId, request);
    await loadCustomer();
  }

  async function handleDeletePhone(phoneId: number): Promise<void> {
    await deletePhone(customerId, phoneId);
    await loadCustomer();
  }

  // --- Email CRUD ---
  async function handleCreateEmail(request: CreateEmailRequest): Promise<void> {
    await createEmail(customerId, request);
    await loadCustomer();
  }

  async function handleUpdateEmail(emailId: number, request: UpdateEmailRequest): Promise<void> {
    await updateEmail(customerId, emailId, request);
    await loadCustomer();
  }

  async function handleDeleteEmail(emailId: number): Promise<void> {
    await deleteEmail(customerId, emailId);
    await loadCustomer();
  }

  // --- Account CRUD ---
  async function handleCreateAccount(request: CreateAccountRequest): Promise<void> {
    await createAccount(customerId, request);
    await loadCustomer();
  }

  async function handleUpdateAccount(accountId: number, request: UpdateAccountRequest): Promise<void> {
    await updateAccount(customerId, accountId, request);
    await loadCustomer();
  }

  async function handleDeactivateAccount(accountId: number): Promise<void> {
    await deactivateAccount(customerId, accountId);
    await loadCustomer();
  }

  async function handleMergeAccounts(request: MergeAccountsRequest): Promise<void> {
    await mergeAccounts(customerId, request);
    await loadCustomer();
  }

  return {
    t,
    layout,
    customer,
    loading,
    notFound,
    customerId,
    loadCustomer,
    formatDate,
    goBack,
    handleReactivate,
    handleDeactivate,
    handleCreateAddress, handleUpdateAddress, handleDeleteAddress,
    handleCreatePhone, handleUpdatePhone, handleDeletePhone,
    handleCreateEmail, handleUpdateEmail, handleDeleteEmail,
    handleCreateAccount, handleUpdateAccount, handleDeactivateAccount, handleMergeAccounts,
  };
}
