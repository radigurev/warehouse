import { ref, computed } from 'vue';
import { useListView } from '@shared/composables/useListView';
import { searchProducts, deactivateProduct, reactivateProduct } from '@features/inventory/api/products';
import type { ProductDto, SearchProductsRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { useNotificationStore } from '@shared/stores/notification';
import { useI18n } from 'vue-i18n';

export function useProductsView() {
  const { t } = useI18n();
  const notification = useNotificationStore();

  const base = useListView<ProductDto, SearchProductsRequest>({
    fetchFn: searchProducts,
    filterFields: ['name', 'code', 'categoryName'],
    filterPathMap: { name: 'name', code: 'code', categoryName: 'category.name' },
    defaultSort: { field: 'name', descending: false },
    defaultParams: { includeDeleted: true } as Partial<SearchProductsRequest>,
    navigation: {
      createRoute: { name: 'product-create' },
      editRoute: (p: ProductDto) => ({ name: 'product-edit', params: { id: p.id } }),
      detailRoute: (p: ProductDto) => ({ name: 'product-detail', params: { id: p.id } }),
    },
  });

  const selectedProduct = base.selectedItem;
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const headers = computed(() => [
    { title: t('products.columns.code'), key: 'code', sortable: true },
    { title: t('products.columns.name'), key: 'name', sortable: true },
    { title: t('products.columns.category'), key: 'categoryName', sortable: false },
    { title: t('products.columns.unit'), key: 'unitOfMeasureName', sortable: false },
    { title: t('products.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('products.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

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
      await base.reload();
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
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    ...base,
    selectedProduct,
    showDeactivateDialog,
    deactivating,
    headers,
    loadProducts: base.reload,
    openDeactivateDialog,
    handleDeactivate,
    handleReactivate,
  };
}
