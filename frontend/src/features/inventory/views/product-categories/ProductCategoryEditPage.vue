<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <v-alert v-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="goBack">{{ t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>
    <ProductCategoryFormDialog v-else v-model="visible" :category="category" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { getCategoryById } from '@features/inventory/api/product-categories';
import type { ProductCategoryDto } from '@features/inventory/types/inventory';
import ProductCategoryFormDialog from '@features/inventory/components/organisms/ProductCategoryFormDialog.vue';

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const visible = ref(true);
const category = ref<ProductCategoryDto | null>(null);
const notFound = ref(false);

onMounted(async () => {
  const id = Number(route.params.id);
  if (!id || id <= 0) { router.push({ name: 'product-categories' }); return; }
  try {
    category.value = await getCategoryById(id);
  } catch {
    notFound.value = true;
  }
});

function goBack(): void {
  router.push({ name: 'product-categories' });
}
</script>
