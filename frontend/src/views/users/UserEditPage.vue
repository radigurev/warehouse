<template>
  <div>
    <v-btn variant="text" prepend-icon="mdi-arrow-left" class="mb-4" @click="goBack">
      {{ t('pageTitle.back') }}
    </v-btn>
    <v-alert v-if="notFound" type="warning" variant="tonal" class="mb-4">
      {{ t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="goBack">{{ t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>
    <UserFormDialog v-else v-model="visible" :user="user" mode="page" @saved="goBack" @cancelled="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { getUserById } from '@/api/users';
import type { UserDto } from '@/types/user';
import UserFormDialog from '@/components/organisms/UserFormDialog.vue';

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const visible = ref(true);
const user = ref<UserDto | null>(null);
const notFound = ref(false);

onMounted(async () => {
  const id = Number(route.params.id);
  if (!id || id <= 0) { router.push({ name: 'users' }); return; }
  try {
    const detail = await getUserById(id);
    user.value = detail;
  } catch {
    notFound.value = true;
  }
});

function goBack(): void {
  router.push({ name: 'users' });
}
</script>
