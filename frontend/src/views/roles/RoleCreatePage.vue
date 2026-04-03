<template>
  <div>
    <v-btn variant="text" prepend-icon="mdi-arrow-left" class="mb-4" @click="goBack">
      {{ t('pageTitle.back') }}
    </v-btn>
    <RoleFormDialog v-model="visible" mode="page" @saved="goBack" @cancelled="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import RoleFormDialog from '@/components/organisms/RoleFormDialog.vue';

const { t } = useI18n();
const router = useRouter();
const visible = ref(true);

function goBack(): void {
  router.push({ name: 'roles' });
}
</script>
