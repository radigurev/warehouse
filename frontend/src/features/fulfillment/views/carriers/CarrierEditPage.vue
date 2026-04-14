<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <CarrierFormDialog v-model="visible" :carrier-id="id" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import CarrierFormDialog from '@features/fulfillment/components/organisms/CarrierFormDialog.vue';

const route = useRoute();
const router = useRouter();
const visible = ref(true);
const id = computed(() => Number(route.params.id));

function goBack(): void {
  router.push({ name: 'carrier-detail', params: { id: route.params.id } });
}
</script>
