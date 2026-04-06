<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <v-alert v-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="goBack">{{ t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>
    <UnitOfMeasureFormDialog v-else v-model="visible" :unit="unit" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { getUnitById } from '@features/inventory/api/units-of-measure';
import type { UnitOfMeasureDto } from '@features/inventory/types/inventory';
import UnitOfMeasureFormDialog from '@features/inventory/components/organisms/UnitOfMeasureFormDialog.vue';

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const visible = ref(true);
const unit = ref<UnitOfMeasureDto | null>(null);
const notFound = ref(false);

onMounted(async () => {
  const id = Number(route.params.id);
  if (!id || id <= 0) { router.push({ name: 'units-of-measure' }); return; }
  try {
    const detail = await getUnitById(id);
    unit.value = detail;
  } catch {
    notFound.value = true;
  }
});

function goBack(): void {
  router.push({ name: 'units-of-measure' });
}
</script>
