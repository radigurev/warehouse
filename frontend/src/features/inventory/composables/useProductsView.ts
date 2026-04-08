import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchProducts, deactivateProduct, reactivateProduct } from '@features/inventory/api/products';
import type { ProductDto, SearchProductsRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useProductsView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const products = ref<ProductDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));
  const selectedProduct = ref<ProductDto | null>(null);
  const showFormDialog = ref(false);
  const showDetailDialog = ref(false);
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const searchParams = ref<SearchProductsRequest>({
    includeDeleted: true,
    sortBy: 'name',
    sortDescending: false,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(products, ['name', 'code', 'categoryName']);

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
    loadProducts();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('products.columns.code'), key: 'code', sortable: true },
    { title: t('products.columns.name'), key: 'name', sortable: true },
    { title: t('products.columns.category'), key: 'categoryName', sortable: false },
    { title: t('products.columns.unit'), key: 'unitOfMeasureName', sortable: false },
    { title: t('products.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('products.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  onMounted(() => loadProducts());

  async function loadProducts(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchProducts(searchParams.value);
      products.value = response.items;
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
      router.push({ name: 'product-create' });
    } else {
      selectedProduct.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(product: ProductDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'product-edit', params: { id: product.id } });
    } else {
      selectedProduct.value = product;
      showFormDialog.value = true;
    }
  }

  function handleDetail(product: ProductDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'product-detail', params: { id: product.id } });
    } else {
      selectedProduct.value = product;
      showDetailDialog.value = true;
    }
  }

  function openDeactivateDialog(product: ProductDto): void {
    selectedProduct.value = product;
    showDeactivateDialog.value = true;
  }

  async function handleDeactivate(): Promise<void> {
    if (!selectedProduct.value) return;
    deactivating.value = true;
    try {
      await deactivateProduct(selectedProduct.value.id);
      notification.success(t('products.deactivated') + ' \u2713');
      showDeactivateDialog.value = false;
      await loadProducts();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deactivating.value = false;
    }
  }

  async function handleReactivate(product: ProductDto): Promise<void> {
    try {
      await reactivateProduct(product.id);
      notification.success(t('products.reactivated') + ' \u2713');
      await loadProducts();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadProducts();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadProducts();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    selectedProduct,
    showFormDialog,
    showDetailDialog,
    showDeactivateDialog,
    deactivating,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadProducts,
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
