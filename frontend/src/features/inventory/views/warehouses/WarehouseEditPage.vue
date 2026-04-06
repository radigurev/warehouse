<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <v-alert v-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="goBack">{{ t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>
    <WarehouseFormDialog v-else v-model="visible" :warehouse="warehouse" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { getWarehouseById } from '@features/inventory/api/warehouses';
import type { WarehouseDto } from '@features/inventory/types/inventory';
import WarehouseFormDialog from '@features/inventory/components/organisms/WarehouseFormDialog.vue';

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const visible = ref(true);
const warehouse = ref<WarehouseDto | null>(null);
const notFound = ref(false);

onMounted(async () => {
  const id = Number(route.params.id);
  if (!id || id <= 0) { router.push({ name: 'warehouses' }); return; }
  try {
    const detail = await getWarehouseById(id);
    warehouse.value = detail;
  } catch {
    notFound.value = true;
  }
});

function goBack(): void {
  router.push({ name: 'warehouses' });
}
</script>
