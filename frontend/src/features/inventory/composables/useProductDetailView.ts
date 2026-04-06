import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getProductById, reactivateProduct, deactivateProduct } from '@features/inventory/api/products';
import { getBomByProductId, createBom, addBomLine, removeBomLine, deleteBom } from '@features/inventory/api/bom';
import { getAccessories, createAccessory, deleteAccessory } from '@features/inventory/api/product-accessories';
import { getSubstitutes, createSubstitute, deleteSubstitute } from '@features/inventory/api/product-substitutes';
import type {
  ProductDetailDto,
  BomDto,
  ProductAccessoryDto,
  ProductSubstituteDto,
  CreateBomRequest,
  AddBomLineRequest,
  CreateProductAccessoryRequest,
  CreateProductSubstituteRequest,
} from '@features/inventory/types/inventory';

export function useProductDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const product = ref<ProductDetailDto | null>(null);
  const bom = ref<BomDto | null>(null);
  const accessories = ref<ProductAccessoryDto[]>([]);
  const substitutes = ref<ProductSubstituteDto[]>([]);
  const loading = ref(false);
  const notFound = ref(false);
  const productId = Number(route.params.id);

  onMounted(() => loadProduct());

  async function loadProduct(): Promise<void> {
    if (!productId || productId <= 0) {
      router.push({ name: 'products' });
      return;
    }
    loading.value = true;
    try {
      product.value = await getProductById(productId);
      await loadRelatedData();
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }

  async function loadRelatedData(): Promise<void> {
    await Promise.all([loadBom(), loadAccessories(), loadSubstitutes()]);
  }

  async function loadBom(): Promise<void> {
    try {
      bom.value = await getBomByProductId(productId);
    } catch {
      bom.value = null;
    }
  }

  async function loadAccessories(): Promise<void> {
    try {
      accessories.value = await getAccessories(productId);
    } catch {
      accessories.value = [];
    }
  }

  async function loadSubstitutes(): Promise<void> {
    try {
      substitutes.value = await getSubstitutes(productId);
    } catch {
      substitutes.value = [];
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
    router.push({ name: 'products' });
  }

  async function handleReactivate(): Promise<void> {
    try {
      product.value = await reactivateProduct(productId);
      notification.success(t('products.reactivated') + ' \u2713');
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  async function handleDeactivate(): Promise<void> {
    try {
      await deactivateProduct(productId);
      notification.success(t('products.deactivated') + ' \u2713');
      await loadProduct();
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  async function handleCreateBom(request: CreateBomRequest): Promise<void> {
    await createBom(request);
    await loadBom();
  }

  async function handleAddBomLine(bomId: number, request: AddBomLineRequest): Promise<void> {
    await addBomLine(bomId, request);
    await loadBom();
  }

  async function handleRemoveBomLine(bomId: number, lineId: number): Promise<void> {
    await removeBomLine(bomId, lineId);
    await loadBom();
  }

  async function handleDeleteBom(bomId: number): Promise<void> {
    await deleteBom(bomId);
    await loadBom();
  }

  async function handleAddAccessory(request: CreateProductAccessoryRequest): Promise<void> {
    await createAccessory(request);
    await loadAccessories();
  }

  async function handleDeleteAccessory(accessoryId: number): Promise<void> {
    await deleteAccessory(accessoryId);
    await loadAccessories();
  }

  async function handleAddSubstitute(request: CreateProductSubstituteRequest): Promise<void> {
    await createSubstitute(request);
    await loadSubstitutes();
  }

  async function handleDeleteSubstitute(substituteId: number): Promise<void> {
    await deleteSubstitute(substituteId);
    await loadSubstitutes();
  }

  return {
    t,
    layout,
    product,
    bom,
    accessories,
    substitutes,
    loading,
    notFound,
    productId,
    loadProduct,
    formatDate,
    goBack,
    handleReactivate,
    handleDeactivate,
    handleCreateBom,
    handleAddBomLine,
    handleRemoveBomLine,
    handleDeleteBom,
    handleAddAccessory,
    handleDeleteAccessory,
    handleAddSubstitute,
    handleDeleteSubstitute,
  };
}
