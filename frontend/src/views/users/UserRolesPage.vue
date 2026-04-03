<template>
  <div>
    <v-btn variant="text" prepend-icon="mdi-arrow-left" class="mb-4" @click="goBack">
      {{ t('pageTitle.back') }}
    </v-btn>
    <UserRolesDialog v-model="visible" :user-id="userId" :user-name="userName" mode="page" @cancelled="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { getUserById } from '@/api/users';
import UserRolesDialog from '@/components/organisms/UserRolesDialog.vue';

const { t } = useI18n();
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
