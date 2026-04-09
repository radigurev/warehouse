import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchSuppliers, deactivateSupplier, reactivateSupplier } from '@features/purchasing/api/suppliers';
import type { SupplierDto, SearchSuppliersRequest } from '@features/purchasing/types/purchasing';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useSuppliersView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const suppliers = ref<SupplierDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));
  const selectedSupplier = ref<SupplierDto | null>(null);
  const showFormDialog = ref(false);
  const showDetailDialog = ref(false);
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const searchParams = ref<SearchSuppliersRequest>({
    includeDeleted: true,
    sortBy: 'name',
    sortDescending: false,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(suppliers, ['name', 'code', 'taxId']);

  const filterPathMap: Record<string, string> = {
    name: 'name',
    code: 'code',
    taxId: 'taxId',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadSuppliers();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('suppliers.columns.code'), key: 'code', sortable: true },
    { title: t('suppliers.columns.name'), key: 'name', sortable: true },
    { title: t('suppliers.columns.taxId'), key: 'taxId', sortable: false },
    { title: t('suppliers.columns.category'), key: 'categoryName', sortable: false },
    { title: t('suppliers.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('suppliers.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  onMounted(() => loadSuppliers());

  async function loadSuppliers(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchSuppliers(searchParams.value);
      suppliers.value = response.items;
      totalCount.value = response.totalCount;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
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
      router.push({ name: 'supplier-create' });
    } else {
      selectedSupplier.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(supplier: SupplierDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'supplier-edit', params: { id: supplier.id } });
    } else {
      selectedSupplier.value = supplier;
      showFormDialog.value = true;
    }
  }

  function handleDetail(supplier: SupplierDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'supplier-detail', params: { id: supplier.id } });
    } else {
      selectedSupplier.value = supplier;
      showDetailDialog.value = true;
    }
  }

  function openDeactivateDialog(supplier: SupplierDto): void {
    selectedSupplier.value = supplier;
    showDeactivateDialog.value = true;
  }

  async function handleDeactivate(): Promise<void> {
    if (!selectedSupplier.value) return;
    deactivating.value = true;
    try {
      await deactivateSupplier(selectedSupplier.value.id);
      notification.success(t('suppliers.deactivated') + ' \u2713');
      showDeactivateDialog.value = false;
      await loadSuppliers();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deactivating.value = false;
    }
  }

  async function handleReactivate(supplier: SupplierDto): Promise<void> {
    try {
      await reactivateSupplier(supplier.id);
      notification.success(t('suppliers.reactivated') + ' \u2713');
      await loadSuppliers();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadSuppliers();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadSuppliers();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    selectedSupplier,
    showFormDialog,
    showDetailDialog,
    showDeactivateDialog,
    deactivating,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadSuppliers,
    formatDate,
    handleCreate,
    handleEdit,
    handleDetail,
    openDeactivateDialog,
    handleDeactivate,
    handleReactivate,
    handlePageChange,
    handlePageSizeChange,
  };
}
