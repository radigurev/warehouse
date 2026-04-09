<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <PurchaseOrderFormDialog v-model="visible" :po-id="id" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import PurchaseOrderFormDialog from '@features/purchasing/components/organisms/PurchaseOrderFormDialog.vue';

const route = useRoute();
const router = useRouter();
const visible = ref(true);
const id = computed(() => Number(route.params.id));

function goBack(): void {
  router.push({ name: 'purchase-order-detail', params: { id: route.params.id } });
}
</script>
