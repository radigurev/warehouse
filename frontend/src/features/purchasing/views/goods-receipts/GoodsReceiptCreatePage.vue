<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <GoodsReceiptFormDialog v-model="visible" :pre-selected-po-id="preSelectedPoId" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import GoodsReceiptFormDialog from '@features/purchasing/components/organisms/GoodsReceiptFormDialog.vue';

const route = useRoute();
const router = useRouter();
const visible = ref(true);

const preSelectedPoId = computed(() => {
  const poId = route.query.poId;
  return poId ? Number(poId) : null;
});

const returnToPoId = computed(() => {
  const returnTo = route.query.returnTo;
  return returnTo ? Number(returnTo) : null;
});

function goBack(): void {
  if (returnToPoId.value) {
    router.push({ name: 'purchase-order-detail', params: { id: returnToPoId.value } });
  } else {
    router.push({ name: 'goods-receipts' });
  }
}
</script>
