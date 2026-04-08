import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { getCategories, deleteCategory } from '@features/customers/api/categories';
import type { CustomerCategoryDto } from '@features/customers/types/customer';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useCategoriesView() {
  const { t } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const categories = ref<CustomerCategoryDto[]>([]);
  const loading = ref(false);
  const selectedCategory = ref<CustomerCategoryDto | null>(null);
  const showFormDialog = ref(false);
  const showDeleteDialog = ref(false);
  const deleting = ref(false);
  const page = ref(1);
  const pageSize = ref(25);
  const totalCount = ref(0);
  const totalPages = ref(0);

  const { columnFilters, filteredItems } = useColumnFilters(categories, ['name']);

  const headers = computed(() => [
    { title: t('categories.columns.name'), key: 'name', sortable: true },
    { title: t('categories.columns.description'), key: 'description', sortable: false },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  onMounted(() => loadCategories());

  async function loadCategories(): Promise<void> {
    loading.value = true;
    try {
      const response = await getCategories(page.value, pageSize.value);
      categories.value = response.items;
      totalCount.value = response.totalCount;
      totalPages.value = response.totalPages;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
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
      router.push({ name: 'category-create' });
    } else {
      selectedCategory.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(category: CustomerCategoryDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'category-edit', params: { id: category.id } });
    } else {
      selectedCategory.value = category;
      showFormDialog.value = true;
    }
  }

  function openDeleteDialog(category: CustomerCategoryDto): void {
    selectedCategory.value = category;
    showDeleteDialog.value = true;
  }

  async function handleDelete(): Promise<void> {
    if (!selectedCategory.value) return;
    deleting.value = true;
    try {
      await deleteCategory(selectedCategory.value.id);
      notification.success(t('categories.delete') + ' \u2713');
      showDeleteDialog.value = false;
      await loadCategories();
    } catch (err) {
      const axiosError = err as AxiosError<ProblemDetails>;
      const errorCode = axiosError.response?.data?.title;
      if (errorCode === 'CATEGORY_IN_USE') {
        notification.error(axiosError.response?.data?.detail || t('errors.CATEGORY_IN_USE'));
      } else {
        notification.error(getApiErrorMessage(err, t));
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
