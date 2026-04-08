<template>
  <FormWrapper v-model="visible" max-width="700" :title="product?.name ?? t('products.viewDetails')" icon="mdi-package-variant-closed">
    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
    </v-alert>

    <template v-else-if="product">
      <v-card-text class="detail-dialog-content pt-4">
        <!-- Header -->
        <div class="d-flex align-center mb-4">
          <div>
            <div class="text-h6">{{ product.name }}</div>
            <div class="text-caption text-medium-emphasis">{{ product.code }}</div>
          </div>
          <v-spacer />
          <StatusChip :active="product.isActive" />
        </div>

        <!-- Product Info -->
        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-information" class="mr-2" size="small" />
            {{ t('products.detail.info') }}
          </div>
          <v-card-text>
            <v-row dense>
              <v-col v-if="product.sku" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('products.form.sku') }}</div>
                <div>{{ product.sku }}</div>
              </v-col>
              <v-col v-if="product.barcode" cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('products.form.barcode') }}</div>
                <div>{{ product.barcode }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('products.form.category') }}</div>
                <div>{{ product.categoryName || '—' }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('products.form.unitOfMeasure') }}</div>
                <div>{{ product.unitOfMeasureName }}</div>
              </v-col>
              <v-col cols="12" md="6">
                <div class="text-caption text-medium-emphasis">{{ t('products.columns.createdAt') }}</div>
                <div>{{ formatDate(product.createdAtUtc) }}</div>
              </v-col>
              <v-col v-if="product.description" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('products.form.description') }}</div>
                <div class="text-body-2">{{ product.description }}</div>
              </v-col>
              <v-col v-if="product.notes" cols="12">
                <div class="text-caption text-medium-emphasis">{{ t('products.form.notes') }}</div>
                <div class="text-body-2">{{ product.notes }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <!-- BOM Section -->
        <v-card class="mb-3">
          <div class="section-header">
            <v-icon icon="mdi-sitemap" class="mr-2" size="small" />
            {{ t('products.detail.bom') }}
            <span v-if="bom?.name" class="text-caption text-medium-emphasis ml-2">({{ bom.name }})</span>
          </div>
          <v-card-text v-if="!bom || bom.lines.length === 0" class="text-medium-emphasis">
            {{ t('products.detail.noBom') }}
          </v-card-text>
          <div v-else class="bom-table-wrapper">
            <v-table density="compact">
              <thead>
                <tr>
                  <th>{{ t('products.form.name') }}</th>
                  <th class="text-end">{{ t('products.detail.bomQuantity') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in bom.lines" :key="line.id">
                  <td>{{ line.childProductName }}</td>
                  <td class="text-end">{{ line.quantity }}</td>
                </tr>
              </tbody>
            </v-table>
          </div>
        </v-card>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="visible = false">{{ t('common.close') }}</v-btn>
        <v-btn color="primary" variant="flat" prepend-icon="mdi-open-in-new" @click="openFullPage">
          {{ t('products.viewDetails') }}
        </v-btn>
      </v-card-actions>
    </template>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getProductById } from '@features/inventory/api/products';
import { getBomByProductId } from '@features/inventory/api/bom';
import type { ProductDetailDto, BomDto } from '@features/inventory/types/inventory';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import StatusChip from '@shared/components/atoms/StatusChip.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  productId: number | null;
}>();

const product = ref<ProductDetailDto | null>(null);
const bom = ref<BomDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.productId) {
    loading.value = true;
    notFound.value = false;
    product.value = null;
    bom.value = null;
    try {
      product.value = await getProductById(props.productId);
      try {
        bom.value = await getBomByProductId(props.productId);
      } catch {
        // BOM fetch failure should not block displaying the product
      }
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }
});

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function openFullPage(): void {
  visible.value = false;
  router.push({ name: 'product-detail', params: { id: props.productId! } });
}
</script>

<style scoped>
.detail-dialog-content {
  background: #f1f5f9;
}

.section-header {
  display: flex;
  align-items: center;
  padding: 12px 16px;
  font-size: 0.875rem;
  font-weight: 500;
}

.bom-table-wrapper {
  max-height: 200px;
  overflow-y: auto;
}
</style>
