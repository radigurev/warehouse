import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getWarehouseById, deactivateWarehouse, reactivateWarehouse } from '@features/inventory/api/warehouses';
import { createZone, updateZone, deleteZone } from '@features/inventory/api/zones';
import { searchLocations, createLocation, updateLocation, deleteLocation } from '@features/inventory/api/storage-locations';
import type {
  WarehouseDetailDto,
  StorageLocationDto,
  CreateZoneRequest,
  UpdateZoneRequest,
  CreateStorageLocationRequest,
  UpdateStorageLocationRequest,
} from '@features/inventory/types/inventory';

export function useWarehouseDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const warehouse = ref<WarehouseDetailDto | null>(null);
  const locations = ref<StorageLocationDto[]>([]);
  const loading = ref(false);
  const notFound = ref(false);
  const warehouseId = Number(route.params.id);

  onMounted(() => {
    loadWarehouse();
    loadLocations();
  });

  async function loadWarehouse(): Promise<void> {
    if (!warehouseId || warehouseId <= 0) {
      router.push({ name: 'warehouses' });
      return;
    }
    loading.value = true;
    try {
      warehouse.value = await getWarehouseById(warehouseId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }

  async function loadLocations(): Promise<void> {
    if (!warehouseId || warehouseId <= 0) return;
    try {
      const response = await searchLocations({
        warehouseId,
        page: 1,
        pageSize: 500,
      });
      locations.value = response.items;
    } catch {
      // silent — locations section will show empty
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
    router.push({ name: 'warehouses' });
  }

  async function handleReactivate(): Promise<void> {
    try {
      await reactivateWarehouse(warehouseId);
      notification.success(t('warehouses.reactivated'));
      await loadWarehouse();
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  async function handleDeactivate(): Promise<void> {
    try {
      await deactivateWarehouse(warehouseId);
      notification.success(t('warehouses.deactivated'));
      router.push({ name: 'warehouses' });
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  // --- Zone CRUD ---
  async function handleCreateZone(request: CreateZoneRequest): Promise<void> {
    await createZone(request);
    await loadWarehouse();
  }

  async function handleUpdateZone(zoneId: number, request: UpdateZoneRequest): Promise<void> {
    await updateZone(zoneId, request);
    await loadWarehouse();
  }

  async function handleDeleteZone(zoneId: number): Promise<void> {
    await deleteZone(zoneId);
    await loadWarehouse();
    await loadLocations();
  }

  // --- Storage Location CRUD ---
  async function handleCreateLocation(request: CreateStorageLocationRequest): Promise<void> {
    await createLocation(request);
    await loadLocations();
  }

  async function handleUpdateLocation(locationId: number, request: UpdateStorageLocationRequest): Promise<void> {
    await updateLocation(locationId, request);
    await loadLocations();
  }

  async function handleDeleteLocation(locationId: number): Promise<void> {
    await deleteLocation(locationId);
    await loadLocations();
  }

  return {
    t,
    layout,
    warehouse,
    locations,
    loading,
    notFound,
    warehouseId,
    loadWarehouse,
    loadLocations,
    formatDate,
    goBack,
    handleReactivate,
    handleDeactivate,
    handleCreateZone, handleUpdateZone, handleDeleteZone,
    handleCreateLocation, handleUpdateLocation, handleDeleteLocation,
  };
}
