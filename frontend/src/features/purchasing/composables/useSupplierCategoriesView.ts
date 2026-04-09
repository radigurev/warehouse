import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { getAllSupplierCategories, deleteSupplierCategory } from '@features/purchasing/api/supplier-categories';
import type { SupplierCategoryDto } from '@features/purchasing/types/purchasing';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useSupplierCategoriesView() {
  const { t } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const categories = ref<SupplierCategoryDto[]>([]);
  const loading = ref(false);
  const selectedCategory = ref<SupplierCategoryDto | null>(null);
  const showFormDialog = ref(false);
  const showDeleteDialog = ref(false);
  const deleting = ref(false);

  const { columnFilters, filteredItems } = useColumnFilters(categories, ['name']);

  const headers = computed(() => [
    { title: t('supplierCategories.columns.name'), key: 'name', sortable: true },
    { title: t('supplierCategories.columns.description'), key: 'description', sortable: false },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  onMounted(() => loadCategories());

  async function loadCategories(): Promise<void> {
    loading.value = true;
    try {
      categories.value = await getAllSupplierCategories();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      loading.value = false;
    }
  }

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'supplier-category-create' });
    } else {
      selectedCategory.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(category: SupplierCategoryDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'supplier-category-edit', params: { id: category.id } });
    } else {
      selectedCategory.value = category;
      showFormDialog.value = true;
    }
  }

  function openDeleteDialog(category: SupplierCategoryDto): void {
    selectedCategory.value = category;
    showDeleteDialog.value = true;
  }

  async function handleDelete(): Promise<void> {
    if (!selectedCategory.value) return;
    deleting.value = true;
    try {
      await deleteSupplierCategory(selectedCategory.value.id);
      notification.success(t('supplierCategories.deleted') + ' \u2713');
      showDeleteDialog.value = false;
      await loadCategories();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
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
    columnFilters,
    filteredItems,
    headers,
    loadCategories,
    handleCreate,
    handleEdit,
    openDeleteDialog,
    handleDelete,
  };
}
