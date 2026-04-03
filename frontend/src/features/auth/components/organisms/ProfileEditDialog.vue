<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="550" :title="t('settings.editProfile')" icon="mdi-account-edit" @back="cancel">
    <v-card-title class="text-h6">{{ t('settings.editProfile') }}</v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
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
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { updateUser } from '@features/auth/api/users';
import type { UserDetailDto } from '@features/auth/types/user';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  user: UserDetailDto | null;
  userId: number;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const formRef = ref();
const loading = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({ email: [] });

const form = reactive({
  email: '',
  firstName: '',
  lastName: '',
});

watch(visible, (val) => {
  if (val && props.user) {
    form.email = props.user.email;
    form.firstName = props.user.firstName;
    form.lastName = props.user.lastName;
  }
  fieldErrors.email = [];
}, { immediate: true });

const rules = {
  required: (v: string) => !!v || t('common.required'),
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
    await updateUser(props.userId, {
      email: form.email,
      firstName: form.firstName,
      lastName: form.lastName,
    });
    notification.success(t('settings.profileUpdated'));
    visible.value = false;
    emit('saved');
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode === 'DUPLICATE_EMAIL') {
      fieldErrors.email = [t('errors.DUPLICATE_EMAIL')];
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
