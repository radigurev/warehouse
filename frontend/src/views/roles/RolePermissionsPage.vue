<template>
  <div class="d-flex flex-column" style="flex: 1; overflow: hidden">
    <RolePermissionsDialog v-model="visible" :role-id="roleId" :role-name="roleName" mode="page" @cancelled="goBack" @back="goBack" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { getRoleById } from '@/api/roles';
import RolePermissionsDialog from '@/components/organisms/RolePermissionsDialog.vue';

const router = useRouter();
const route = useRoute();
const visible = ref(true);
const roleId = computed(() => Number(route.params.id) || 0);
const roleName = ref('');

onMounted(async () => {
  try {
    const role = await getRoleById(roleId.value);
    roleName.value = role.name;
  } catch {
    router.push({ name: 'roles' });
  }
});

function goBack(): void {
  router.push({ name: 'roles' });
}
</script>
