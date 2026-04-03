<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="550" :title="isEdit ? t('users.edit') : t('users.create')" :icon="isEdit ? 'mdi-account-edit' : 'mdi-account-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('users.edit') : t('users.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-text-field
          v-if="!isEdit"
          v-model="form.username"
          :label="t('users.form.username')"
          prepend-inner-icon="mdi-account"
          :rules="[rules.required, rules.usernameLength, rules.usernameFormat]"
          :error-messages="fieldErrors.username"
          @update:model-value="fieldErrors.username = []"
        />

        <v-text-field
          v-model="form.email"
          :label="t('users.form.email')"
          prepend-inner-icon="mdi-email"
          :rules="[rules.required, rules.emailFormat, rules.emailLength]"
          :error-messages="fieldErrors.email"
          @update:model-value="fieldErrors.email = []"
        />

        <v-text-field
          v-model="form.firstName"
          :label="t('users.form.firstName')"
          prepend-inner-icon="mdi-badge-account-horizontal"
          :rules="[rules.required, rules.firstNameLength]"
        />

        <v-text-field
          v-model="form.lastName"
          :label="t('users.form.lastName')"
          prepend-inner-icon="mdi-badge-account"
          :rules="[rules.required, rules.lastNameLength]"
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

  <v-dialog v-model="showPasswordDialog" max-width="450" persistent>
    <v-card>
      <div class="d-flex align-center pa-3" style="background: #334155; color: white">
        <v-icon icon="mdi-shield-key" class="mr-2" />
        <span class="text-subtitle-1 font-weight-medium">{{ t('users.generatedPassword') }}</span>
        <v-spacer />
        <v-btn icon="mdi-close" variant="text" size="small" color="white" @click="closePasswordDialog" />
      </div>
      <v-card-text>
        <v-alert type="warning" variant="tonal" density="compact" class="mb-4">
          {{ t('users.generatedPasswordHint') }}
        </v-alert>
        <v-text-field
          :model-value="generatedPassword"
          :label="t('users.form.password')"
          prepend-inner-icon="mdi-lock"
          append-inner-icon="mdi-content-copy"
          readonly
          @click:append-inner="copyPassword"
        />
      </v-card-text>
      <v-card-actions style="background: #334155">
        <v-spacer />
        <v-btn color="white" variant="flat" @click="closePasswordDialog">{{ t('common.close') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { createUser, updateUser } from '@features/auth/api/users';
import type { UserDto } from '@features/auth/types/user';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  user?: UserDto | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const showPasswordDialog = ref(false);
const generatedPassword = ref('');
const fieldErrors = reactive<Record<string, string[]>>({
  username: [],
  email: [],
});

const form = reactive({
  username: '',
  email: '',
  firstName: '',
  lastName: '',
});

watch(visible, (val) => {
  if (val && props.user) {
    isEdit.value = true;
    form.username = props.user.username;
    form.email = props.user.email;
    form.firstName = props.user.firstName;
    form.lastName = props.user.lastName;
  } else if (val) {
    isEdit.value = false;
    form.username = '';
    form.email = '';
    form.firstName = '';
    form.lastName = '';
  }
  fieldErrors.username = [];
  fieldErrors.email = [];
});

const rules = {
  required: (v: string) => !!v || t('common.required'),
  usernameLength: (v: string) => (v.length >= 3 && v.length <= 50) || t('validation.usernameLength'),
  usernameFormat: (v: string) => /^[a-zA-Z0-9_]+$/.test(v) || t('validation.usernameFormat'),
  emailFormat: (v: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) || t('validation.emailInvalid'),
  emailLength: (v: string) => v.length <= 256 || t('validation.emailLength'),
  firstNameLength: (v: string) => v.length <= 100 || t('validation.firstNameLength'),
  lastNameLength: (v: string) => v.length <= 100 || t('validation.lastNameLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.user) {
      await updateUser(props.user.id, {
        email: form.email,
        firstName: form.firstName,
        lastName: form.lastName,
      });
      notification.success(t('users.edit') + ' ✓');
      visible.value = false;
      emit('saved');
    } else {
      const response = await createUser({
        username: form.username,
        email: form.email,
        firstName: form.firstName,
        lastName: form.lastName,
      });
      generatedPassword.value = response.generatedPassword;
      visible.value = false;
      showPasswordDialog.value = true;
    }
  } catch (err) {
    handleApiError(err as AxiosError<ProblemDetails>);
  } finally {
    loading.value = false;
  }
}

function handleApiError(err: AxiosError<ProblemDetails>): void {
  const errorCode = err.response?.data?.title;
  if (errorCode === 'DUPLICATE_USERNAME') {
    fieldErrors.username = [t('errors.DUPLICATE_USERNAME')];
  } else if (errorCode === 'DUPLICATE_EMAIL') {
    fieldErrors.email = [t('errors.DUPLICATE_EMAIL')];
  } else if (errorCode) {
    const key = `errors.${errorCode}`;
    const translated = t(key);
    notification.error(translated !== key ? translated : t('errors.UNEXPECTED_ERROR'));
  } else {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  }
}

function copyPassword(): void {
  navigator.clipboard.writeText(generatedPassword.value);
  notification.success(t('users.passwordCopied'));
}

function closePasswordDialog(): void {
  showPasswordDialog.value = false;
  generatedPassword.value = '';
  emit('saved');
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
