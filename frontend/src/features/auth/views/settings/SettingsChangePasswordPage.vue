<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <ChangePasswordDialog v-model="visible" :user-id="auth.userId ?? 0" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@shared/stores/auth';
import ChangePasswordDialog from '@features/auth/components/organisms/ChangePasswordDialog.vue';

const router = useRouter();
const auth = useAuthStore();
const visible = ref(true);

function goBack(): void {
  router.push({ name: 'settings' });
}
</script>
