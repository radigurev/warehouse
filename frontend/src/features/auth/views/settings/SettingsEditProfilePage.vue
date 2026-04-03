<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <ProfileEditDialog v-model="visible" :user="profile" :user-id="auth.userId ?? 0" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@shared/stores/auth';
import { getUserById } from '@features/auth/api/users';
import type { UserDetailDto } from '@features/auth/types/user';
import ProfileEditDialog from '@features/auth/components/organisms/ProfileEditDialog.vue';

const router = useRouter();
const auth = useAuthStore();
const visible = ref(true);
const profile = ref<UserDetailDto | null>(null);

onMounted(async () => {
  if (!auth.userId) { router.push({ name: 'settings' }); return; }
  try {
    profile.value = await getUserById(auth.userId);
  } catch {
    router.push({ name: 'settings' });
  }
});

function goBack(): void {
  router.push({ name: 'settings' });
}
</script>
