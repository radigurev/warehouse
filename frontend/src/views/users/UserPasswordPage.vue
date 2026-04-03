<template>
  <div>
    <v-btn variant="text" prepend-icon="mdi-arrow-left" class="mb-4" @click="goBack">
      {{ t('pageTitle.back') }}
    </v-btn>
    <ChangePasswordDialog v-model="visible" :user-id="userId" mode="page" @saved="goBack" @cancelled="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import ChangePasswordDialog from '@/components/organisms/ChangePasswordDialog.vue';

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const visible = ref(true);
const userId = computed(() => Number(route.params.id) || 0);

function goBack(): void {
  router.push({ name: 'users' });
}
</script>
