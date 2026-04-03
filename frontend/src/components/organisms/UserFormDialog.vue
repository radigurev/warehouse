<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="550">
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
          v-if="!isEdit"
          v-model="form.password"
          :label="t('users.form.password')"
          prepend-inner-icon="mdi-lock"
          :type="showPassword ? 'text' : 'password'"
          :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
          :rules="[rules.required, rules.passwordLength, rules.passwordComplexity]"
          @click:append-inner="showPassword = !showPassword"
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
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@/stores/notification';
import { createUser, updateUser } from '@/api/users';
import type { UserDto } from '@/types/user';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@/types/api';
import FormWrapper from '@/components/molecules/FormWrapper.vue';

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
const showPassword = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({
  username: [],
  email: [],
});

const form = reactive({
  username: '',
  email: '',
  password: '',
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
    form.password = '';
  } else if (val) {
    isEdit.value = false;
    form.username = '';
    form.email = '';
    form.password = '';
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
  passwordLength: (v: string) => v.length >= 8 || t('validation.passwordLength'),
  passwordComplexity: (v: string) =>
    (/[a-z]/.test(v) && /[A-Z]/.test(v) && /\d/.test(v)) || t('validation.passwordComplexity'),
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
    } else {
      await createUser({
        username: form.username,
        email: form.email,
        password: form.password,
        firstName: form.firstName,
        lastName: form.lastName,
      });
      notification.success(t('users.create') + ' ✓');
    }
    visible.value = false;
    emit('saved');
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

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
