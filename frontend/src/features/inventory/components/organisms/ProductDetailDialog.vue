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
          <v-card-title class="text-subtitle-1 font-weight-medium">
            <v-icon icon="mdi-information" class="mr-2" />
            {{ t('products.detail.info') }}
          </v-card-title>
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
import type { ProductDetailDto } from '@features/inventory/types/inventory';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import StatusChip from '@shared/components/atoms/StatusChip.vue';

const { t, locale } = useI18n();
const router = useRouter();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  productId: number | null;
}>();

const product = ref<ProductDetailDto | null>(null);
const loading = ref(false);
const notFound = ref(false);

watch(visible, async (open) => {
  if (open && props.productId) {
    loading.value = true;
    notFound.value = false;
    product.value = null;
    try {
      product.value = await getProductById(props.productId);
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
</style>
