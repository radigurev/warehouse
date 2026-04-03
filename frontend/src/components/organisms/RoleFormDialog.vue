<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="500">
    <v-card-title class="text-h6">
      {{ isEdit ? t('roles.edit') : t('roles.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-text-field
          v-model="form.name"
          :label="t('roles.form.name')"
          prepend-inner-icon="mdi-shield-account"
          :rules="[rules.required, rules.nameLength]"
          :error-messages="fieldErrors.name"
          @update:model-value="fieldErrors.name = []"
        />

        <v-textarea
          v-model="form.description"
          :label="t('roles.form.description')"
          prepend-inner-icon="mdi-text"
          :rules="[rules.descriptionLength]"
          rows="3"
          variant="outlined"
        />
      </v-form>
    </v-card-text>

    <v-card-actions>
      <v-spacer />
      <v-btn variant="text" @click="cancel">{{ t('common.cancel') }}</v-btn>
      <v-btn color="primary" variant="flat" @click="handleSubmit" :loading="loading">
        {{ t('common.save') }}
      </v-btn>
    </v-card-actions>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@/stores/notification';
import { createRole, updateRole } from '@/api/roles';
import type { RoleDto } from '@/types/role';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@/types/api';
import FormWrapper from '@/components/molecules/FormWrapper.vue';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  role?: RoleDto | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({ name: [] });

const form = reactive({
  name: '',
  description: '',
});

watch(visible, (val) => {
  if (val && props.role) {
    isEdit.value = true;
    form.name = props.role.name;
    form.description = props.role.description || '';
  } else if (val) {
    isEdit.value = false;
    form.name = '';
    form.description = '';
  }
  fieldErrors.name = [];
});

const rules = {
  required: (v: string) => !!v || t('common.required'),
  nameLength: (v: string) => (v.length >= 2 && v.length <= 50) || t('validation.roleNameLength'),
  descriptionLength: (v: string) => v.length <= 500 || t('validation.roleDescriptionLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    const description = form.description || null;
    if (isEdit.value && props.role) {
      await updateRole(props.role.id, { name: form.name, description });
      notification.success(t('roles.edit') + ' ✓');
    } else {
      await createRole({ name: form.name, description });
      notification.success(t('roles.create') + ' ✓');
    }
    visible.value = false;
    emit('saved');
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode === 'DUPLICATE_ROLE_NAME') {
      fieldErrors.name = [t('errors.DUPLICATE_ROLE_NAME')];
    } else {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  } finally {
    loading.value = false;
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
