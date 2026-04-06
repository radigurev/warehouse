import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { searchUnits, deleteUnit } from '@features/inventory/api/units-of-measure';
import type { UnitOfMeasureDto } from '@features/inventory/types/inventory';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

export function useUnitsOfMeasureView() {
  const { t } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const units = ref<UnitOfMeasureDto[]>([]);
  const loading = ref(false);
  const selectedUnit = ref<UnitOfMeasureDto | null>(null);
  const showFormDialog = ref(false);
  const showDeleteDialog = ref(false);
  const deleting = ref(false);
  const page = ref(1);
  const pageSize = ref(25);
  const totalCount = ref(0);
  const totalPages = ref(0);

  const { columnFilters, filteredItems } = useColumnFilters(units, ['code', 'name']);

  const headers = computed(() => [
    { title: t('unitsOfMeasure.columns.code'), key: 'code', sortable: true },
    { title: t('unitsOfMeasure.columns.name'), key: 'name', sortable: true },
    { title: t('unitsOfMeasure.columns.description'), key: 'description', sortable: false },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  onMounted(() => loadUnits());

  async function loadUnits(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchUnits(page.value, pageSize.value);
      units.value = response.items;
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
    loadUnits();
  }

  function handlePageSizeChange(newSize: number): void {
    pageSize.value = newSize;
    page.value = 1;
    loadUnits();
  }

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'unit-create' });
    } else {
      selectedUnit.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(unit: UnitOfMeasureDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'unit-edit', params: { id: unit.id } });
    } else {
      selectedUnit.value = unit;
      showFormDialog.value = true;
    }
  }

  function openDeleteDialog(unit: UnitOfMeasureDto): void {
    selectedUnit.value = unit;
    showDeleteDialog.value = true;
  }

  async function handleDelete(): Promise<void> {
    if (!selectedUnit.value) return;
    deleting.value = true;
    try {
      await deleteUnit(selectedUnit.value.id);
      notification.success(t('unitsOfMeasure.delete') + ' \u2713');
      showDeleteDialog.value = false;
      await loadUnits();
    } catch (err) {
      const axiosError = err as AxiosError<ProblemDetails>;
      const errorCode = axiosError.response?.data?.title;
      if (errorCode === 'UNIT_IN_USE') {
        notification.error(axiosError.response?.data?.detail || t('errors.UNIT_IN_USE'));
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
    selectedUnit,
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
    loadUnits,
    handlePageChange,
    handlePageSizeChange,
    handleCreate,
    handleEdit,
    openDeleteDialog,
    handleDelete,
  };
}
