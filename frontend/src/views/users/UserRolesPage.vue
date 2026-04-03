<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <UserRolesDialog v-model="visible" :user-id="userId" :user-name="userName" mode="page" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { getUserById } from '@/api/users';
import UserRolesDialog from '@/components/organisms/UserRolesDialog.vue';

const router = useRouter();
const route = useRoute();
const visible = ref(true);
const userId = computed(() => Number(route.params.id) || 0);
const userName = ref('');

onMounted(async () => {
  try {
    const user = await getUserById(userId.value);
    userName.value = user.username;
  } catch {
    router.push({ name: 'users' });
  }
});

function goBack(): void {
  router.push({ name: 'users' });
}
</script>
