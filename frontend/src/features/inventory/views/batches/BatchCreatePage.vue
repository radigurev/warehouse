<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <BatchFormDialog v-model="visible" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import BatchFormDialog from '@features/inventory/components/organisms/BatchFormDialog.vue';

const router = useRouter();
const visible = ref(true);

function goBack(): void {
  router.push({ name: 'batches' });
}
</script>
