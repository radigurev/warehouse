import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchCustomers, deactivateCustomer, reactivateCustomer } from '@features/customers/api/customers';
import type { CustomerDto, SearchCustomersRequest } from '@features/customers/types/customer';

export function useCustomersView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const customers = ref<CustomerDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const selectedCustomer = ref<CustomerDto | null>(null);
  const showFormDialog = ref(false);
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const searchParams = ref<SearchCustomersRequest>({
    includeDeleted: false,
    sortBy: 'name',
    sortDescending: false,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(customers, ['name', 'code', 'categoryName']);

  const filterPathMap: Record<string, string> = {
    name: 'name',
    code: 'code',
    categoryName: 'category.name',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadCustomers();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('customers.columns.code'), key: 'code', sortable: true },
    { title: t('customers.columns.name'), key: 'name', sortable: true },
    { title: t('customers.columns.taxId'), key: 'taxId', sortable: false },
    { title: t('customers.columns.category'), key: 'categoryName', sortable: false },
    { title: t('customers.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('customers.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  onMounted(() => loadCustomers());

  async function loadCustomers(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchCustomers(searchParams.value);
      customers.value = response.items;
      totalCount.value = response.totalCount;
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
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

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'customer-create' });
    } else {
      selectedCustomer.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(customer: CustomerDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'customer-edit', params: { id: customer.id } });
    } else {
      selectedCustomer.value = customer;
      showFormDialog.value = true;
    }
  }

  function handleDetail(customer: CustomerDto): void {
    router.push({ name: 'customer-detail', params: { id: customer.id } });
  }

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
      await loadCustomers();
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    } finally {
      deactivating.value = false;
    }
  }

  async function handleReactivate(customer: CustomerDto): Promise<void> {
    try {
      await reactivateCustomer(customer.id);
      notification.success(t('customers.reactivated') + ' \u2713');
      await loadCustomers();
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    selectedCustomer,
    showFormDialog,
    showDeactivateDialog,
    deactivating,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadCustomers,
    formatDate,
    handleCreate,
    handleEdit,
    handleDetail,
    openDeactivateDialog,
    handleDeactivate,
    handleReactivate,
  };
}
