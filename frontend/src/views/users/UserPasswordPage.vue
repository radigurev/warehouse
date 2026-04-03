<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <ChangePasswordDialog v-model="visible" :user-id="userId" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import ChangePasswordDialog from '@/components/organisms/ChangePasswordDialog.vue';

const router = useRouter();
const route = useRoute();
const visible = ref(true);
const userId = computed(() => Number(route.params.id) || 0);

function goBack(): void {
  router.push({ name: 'users' });
}
</script>
