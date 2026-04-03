<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <UserFormDialog v-model="visible" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import UserFormDialog from '@/components/organisms/UserFormDialog.vue';

const { t } = useI18n();
const router = useRouter();
const visible = ref(true);

function goBack(): void {
  router.push({ name: 'users' });
}
</script>
