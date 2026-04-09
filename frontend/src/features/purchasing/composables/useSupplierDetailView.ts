import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getSupplierById, deactivateSupplier, reactivateSupplier } from '@features/purchasing/api/suppliers';
import {
  createSupplierAddress, updateSupplierAddress, deleteSupplierAddress,
  createSupplierPhone, updateSupplierPhone, deleteSupplierPhone,
  createSupplierEmail, updateSupplierEmail, deleteSupplierEmail,
} from '@features/purchasing/api/supplier-contacts';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type {
  SupplierDetailDto,
  CreateSupplierAddressRequest, UpdateSupplierAddressRequest,
  CreateSupplierPhoneRequest, UpdateSupplierPhoneRequest,
  CreateSupplierEmailRequest, UpdateSupplierEmailRequest,
} from '@features/purchasing/types/purchasing';

export function useSupplierDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const supplier = ref<SupplierDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const supplierId = Number(route.params.id);

  onMounted(() => loadSupplier());

  async function loadSupplier(): Promise<void> {
    if (!supplierId || supplierId <= 0) {
      router.push({ name: 'suppliers' });
      return;
    }
    loading.value = true;
    try {
      supplier.value = await getSupplierById(supplierId);
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
    router.push({ name: 'suppliers' });
  }

  async function handleReactivate(): Promise<void> {
    try {
      supplier.value = await reactivateSupplier(supplierId);
      notification.success(t('suppliers.reactivated') + ' \u2713');
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleDeactivate(): Promise<void> {
    try {
      await deactivateSupplier(supplierId);
      notification.success(t('suppliers.deactivated') + ' \u2713');
      router.push({ name: 'suppliers' });
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleCreateAddress(request: CreateSupplierAddressRequest): Promise<void> {
    await createSupplierAddress(supplierId, request);
    await loadSupplier();
  }

  async function handleUpdateAddress(addressId: number, request: UpdateSupplierAddressRequest): Promise<void> {
    await updateSupplierAddress(supplierId, addressId, request);
    await loadSupplier();
  }

  async function handleDeleteAddress(addressId: number): Promise<void> {
    await deleteSupplierAddress(supplierId, addressId);
    await loadSupplier();
  }

  async function handleCreatePhone(request: CreateSupplierPhoneRequest): Promise<void> {
    await createSupplierPhone(supplierId, request);
    await loadSupplier();
  }

  async function handleUpdatePhone(phoneId: number, request: UpdateSupplierPhoneRequest): Promise<void> {
    await updateSupplierPhone(supplierId, phoneId, request);
    await loadSupplier();
  }

  async function handleDeletePhone(phoneId: number): Promise<void> {
    await deleteSupplierPhone(supplierId, phoneId);
    await loadSupplier();
  }

  async function handleCreateEmail(request: CreateSupplierEmailRequest): Promise<void> {
    await createSupplierEmail(supplierId, request);
    await loadSupplier();
  }

  async function handleUpdateEmail(emailId: number, request: UpdateSupplierEmailRequest): Promise<void> {
    await updateSupplierEmail(supplierId, emailId, request);
    await loadSupplier();
  }

  async function handleDeleteEmail(emailId: number): Promise<void> {
    await deleteSupplierEmail(supplierId, emailId);
    await loadSupplier();
  }

  return {
    t,
    layout,
    supplier,
    loading,
    notFound,
    supplierId,
    loadSupplier,
    formatDate,
    goBack,
    handleReactivate,
    handleDeactivate,
    handleCreateAddress, handleUpdateAddress, handleDeleteAddress,
    handleCreatePhone, handleUpdatePhone, handleDeletePhone,
    handleCreateEmail, handleUpdateEmail, handleDeleteEmail,
  };
}
