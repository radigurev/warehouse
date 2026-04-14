<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <SalesOrderFormDialog v-model="visible" :so-id="id" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import SalesOrderFormDialog from '@features/fulfillment/components/organisms/SalesOrderFormDialog.vue';

const route = useRoute();
const router = useRouter();
const visible = ref(true);
const id = computed(() => Number(route.params.id));

function goBack(): void {
  router.push({ name: 'sales-order-detail', params: { id: route.params.id } });
}
</script>
