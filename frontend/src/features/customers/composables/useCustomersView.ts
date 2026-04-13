import { ref } from 'vue';
import { useListView } from '@shared/composables/useListView';
import { searchCustomers, deactivateCustomer, reactivateCustomer } from '@features/customers/api/customers';
import type { CustomerDto, SearchCustomersRequest } from '@features/customers/types/customer';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { useNotificationStore } from '@shared/stores/notification';
import { useI18n } from 'vue-i18n';
import { computed } from 'vue';

export function useCustomersView() {
  const { t } = useI18n();
  const notification = useNotificationStore();

  const base = useListView<CustomerDto, SearchCustomersRequest>({
    fetchFn: searchCustomers,
    filterFields: ['name', 'code', 'categoryName'],
    filterPathMap: { name: 'name', code: 'code', categoryName: 'category.name' },
    defaultSort: { field: 'name', descending: false },
    defaultParams: { includeDeleted: true } as Partial<SearchCustomersRequest>,
    navigation: {
      createRoute: { name: 'customer-create' },
      editRoute: (c: CustomerDto) => ({ name: 'customer-edit', params: { id: c.id } }),
      detailRoute: (c: CustomerDto) => ({ name: 'customer-detail', params: { id: c.id } }),
    },
  });

  const selectedCustomer = base.selectedItem;
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const headers = computed(() => [
    { title: t('customers.columns.code'), key: 'code', sortable: true },
    { title: t('customers.columns.name'), key: 'name', sortable: true },
    { title: t('customers.columns.taxId'), key: 'taxId', sortable: false },
    { title: t('customers.columns.category'), key: 'categoryName', sortable: false },
    { title: t('customers.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('customers.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  function openDeactivateDialog(customer: CustomerDto): void {
    selectedCustomer.value = customer;
    showDeactivateDialog.value = true;
  }

  async function handleDeactivate(): Promise<void> {
    if (!selectedCustomer.value) return;
    deactivating.value = true;
    try {
      await deactivateCustomer(selectedCustomer.value.id);
      notification.success(t('customers.deactivated') + ' \u2713');
      showDeactivateDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deactivating.value = false;
    }
  }

  async function handleReactivate(customer: CustomerDto): Promise<void> {
    try {
      await reactivateCustomer(customer.id);
      notification.success(t('customers.reactivated') + ' \u2713');
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    ...base,
    selectedCustomer,
    showDeactivateDialog,
    deactivating,
    headers,
    loadCustomers: base.reload,
    openDeactivateDialog,
    handleDeactivate,
    handleReactivate,
  };
}
