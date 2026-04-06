<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <ProductFormDialog v-model="visible" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import ProductFormDialog from '@features/inventory/components/organisms/ProductFormDialog.vue';

const router = useRouter();
const visible = ref(true);

function goBack(): void {
  router.push({ name: 'products' });
}
</script>
