<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <v-alert v-if="notFound" type="warning" variant="tonal" class="ma-4">
      {{ t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="goBack">{{ t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>
    <CustomerFormDialog v-else v-model="visible" :customer="customer" mode="page" @saved="goBack" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { getCustomerById } from '@features/customers/api/customers';
import type { CustomerDto } from '@features/customers/types/customer';
import CustomerFormDialog from '@features/customers/components/organisms/CustomerFormDialog.vue';

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const visible = ref(true);
const customer = ref<CustomerDto | null>(null);
const notFound = ref(false);

onMounted(async () => {
  const id = Number(route.params.id);
  if (!id || id <= 0) { router.push({ name: 'customers' }); return; }
  try {
    const detail = await getCustomerById(id);
    customer.value = detail;
  } catch {
    notFound.value = true;
  }
});

function goBack(): void {
  router.push({ name: 'customers' });
}
</script>
