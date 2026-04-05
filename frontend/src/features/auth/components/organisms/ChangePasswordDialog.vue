<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="450" :title="t('users.changePassword')" icon="mdi-lock-reset" @back="cancel">
    <v-card-title class="text-h6">{{ t('users.changePassword') }}</v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.currentPassword"
              :label="t('users.passwordForm.currentPassword')"
              prepend-inner-icon="mdi-lock"
              :density="layout.vuetifyDensity"
              type="password"
              :rules="[rules.required]"
              :error-messages="fieldErrors.currentPassword"
              @update:model-value="fieldErrors.currentPassword = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.newPassword"
              :label="t('users.passwordForm.newPassword')"
              prepend-inner-icon="mdi-lock-reset"
              :density="layout.vuetifyDensity"
              :type="showNew ? 'text' : 'password'"
              :append-inner-icon="showNew ? 'mdi-eye-off' : 'mdi-eye'"
              :rules="[rules.required, rules.passwordLength, rules.passwordComplexity]"
              @click:append-inner="showNew = !showNew"
            />
          </v-col>
        </v-row>
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
import { useNotificationStore } from '@shared/stores/notification';
import { changePassword } from '@features/auth/api/users';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  userId: number;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const formRef = ref();
const loading = ref(false);
const showNew = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({
  currentPassword: [],
});

const form = reactive({
  currentPassword: '',
  newPassword: '',
});

watch(visible, (val) => {
  if (val) {
    form.currentPassword = '';
    form.newPassword = '';
    fieldErrors.currentPassword = [];
  }
});

const rules = {
  required: (v: string) => !!v || t('common.required'),
  passwordLength: (v: string) => v.length >= 8 || t('validation.passwordLength'),
  passwordComplexity: (v: string) =>
    (/[a-z]/.test(v) && /[A-Z]/.test(v) && /\d/.test(v)) || t('validation.passwordComplexity'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    await changePassword(props.userId, {
      currentPassword: form.currentPassword,
      newPassword: form.newPassword,
    });
    notification.success(t('users.changePassword') + ' ✓');
    visible.value = false;
    emit('saved');
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode === 'INVALID_CURRENT_PASSWORD') {
      fieldErrors.currentPassword = [t('errors.INVALID_CURRENT_PASSWORD')];
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
