import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { searchCategories, deleteCategory } from '@features/inventory/api/product-categories';
import type { ProductCategoryDto } from '@features/inventory/types/inventory';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

export function useProductCategoriesView() {
  const { t } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const categories = ref<ProductCategoryDto[]>([]);
  const loading = ref(false);
  const selectedCategory = ref<ProductCategoryDto | null>(null);
  const showFormDialog = ref(false);
  const showDeleteDialog = ref(false);
  const deleting = ref(false);
  const page = ref(1);
  const pageSize = ref(25);
  const totalCount = ref(0);
  const totalPages = ref(0);

  const { columnFilters, filteredItems } = useColumnFilters(categories, ['name', 'parentCategoryName']);

  const headers = computed(() => [
    { title: t('productCategories.columns.name'), key: 'name', sortable: true },
    { title: t('productCategories.columns.description'), key: 'description', sortable: false },
    { title: t('productCategories.columns.parentCategory'), key: 'parentCategoryName', sortable: false },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  onMounted(() => loadCategories());

  async function loadCategories(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchCategories(page.value, pageSize.value);
      categories.value = response.items;
      totalCount.value = response.totalCount;
      totalPages.value = response.totalPages;
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    } finally {
      loading.value = false;
    }
  }

  function handlePageChange(newPage: number): void {
    page.value = newPage;
    loadCategories();
  }

  function handlePageSizeChange(newSize: number): void {
    pageSize.value = newSize;
    page.value = 1;
    loadCategories();
  }

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'product-category-create' });
    } else {
      selectedCategory.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(category: ProductCategoryDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'product-category-edit', params: { id: category.id } });
    } else {
      selectedCategory.value = category;
      showFormDialog.value = true;
    }
  }

  function openDeleteDialog(category: ProductCategoryDto): void {
    selectedCategory.value = category;
    showDeleteDialog.value = true;
  }

  async function handleDelete(): Promise<void> {
    if (!selectedCategory.value) return;
    deleting.value = true;
    try {
      await deleteCategory(selectedCategory.value.id);
      notification.success(t('productCategories.delete') + ' \u2713');
      showDeleteDialog.value = false;
      await loadCategories();
    } catch (err) {
      const axiosError = err as AxiosError<ProblemDetails>;
      const errorCode = axiosError.response?.data?.title;
      if (errorCode === 'PRODUCT_CATEGORY_IN_USE') {
        notification.error(axiosError.response?.data?.detail || t('errors.PRODUCT_CATEGORY_IN_USE'));
      } else {
        notification.error(t('errors.UNEXPECTED_ERROR'));
      }
    } finally {
      deleting.value = false;
    }
  }

  return {
    t,
    layout,
    loading,
    selectedCategory,
    showFormDialog,
    showDeleteDialog,
    deleting,
    page,
    pageSize,
    totalCount,
    totalPages,
    columnFilters,
    filteredItems,
    headers,
    loadCategories,
    handlePageChange,
    handlePageSizeChange,
    handleCreate,
    handleEdit,
    openDeleteDialog,
    handleDelete,
  };
}
