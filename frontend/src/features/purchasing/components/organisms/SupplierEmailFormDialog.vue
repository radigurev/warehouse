<template>
  <v-dialog v-model="visible" max-width="500" persistent>
    <v-card>
      <v-card-title class="text-h6">
        <v-icon class="mr-2">mdi-email</v-icon>
        {{ isEdit ? t('supplierEmails.edit') : t('supplierEmails.create') }}
      </v-card-title>

      <v-card-text>
        <v-form ref="formRef" @submit.prevent="handleSubmit">
          <v-row dense>
            <v-col cols="12" md="6">
              <v-select
                v-model="form.emailType"
                :label="t('supplierEmails.form.emailType')"
                prepend-inner-icon="mdi-email-variant"
                density="compact"
                :items="emailTypes"
                :rules="[rules.required]"
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                v-model="form.emailAddress"
                :label="t('supplierEmails.form.emailAddress')"
                prepend-inner-icon="mdi-email"
                density="compact"
                type="email"
                :rules="[rules.required, rules.emailFormat]"
                :error-messages="fieldErrors.emailAddress"
                @update:model-value="fieldErrors.emailAddress = []"
              />
            </v-col>

            <v-col v-if="isEdit" cols="12" md="6">
              <v-switch
                v-model="form.isPrimary"
                :label="t('supplierEmails.form.isPrimary')"
                density="compact"
                color="primary"
                hide-details
              />
            </v-col>
          </v-row>
        </v-form>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="cancel">{{ t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" :loading="loading" @click="handleSubmit">
          {{ t('common.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { createSupplierEmail, updateSupplierEmail } from '@features/purchasing/api/supplier-contacts';
import type { SupplierEmailDto } from '@features/purchasing/types/purchasing';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  supplierId: number;
  email?: SupplierEmailDto | null;
}>();

const emit = defineEmits<{
  saved: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);

const emailTypes = ['General', 'Billing', 'Support'];

const fieldErrors = reactive<Record<string, string[]>>({
  emailAddress: [],
});

const form = reactive({
  emailType: 'General' as string,
  emailAddress: '',
  isPrimary: false,
});

function populateForm(): void {
  if (visible.value && props.email) {
    isEdit.value = true;
    form.emailType = props.email.emailType;
    form.emailAddress = props.email.emailAddress;
    form.isPrimary = props.email.isPrimary;
  } else if (visible.value) {
    isEdit.value = false;
    form.emailType = 'General';
    form.emailAddress = '';
    form.isPrimary = false;
  }
  fieldErrors.emailAddress = [];
}

watch(visible, populateForm);
watch(() => props.email, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  emailFormat: (v: string) => !v || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) || t('validation.emailFormat'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.email) {
      await updateSupplierEmail(props.supplierId, props.email.id, {
        emailType: form.emailType,
        emailAddress: form.emailAddress,
        isPrimary: form.isPrimary,
      });
      notification.success(t('supplierEmails.edit') + ' \u2713');
    } else {
      await createSupplierEmail(props.supplierId, {
        emailType: form.emailType,
        emailAddress: form.emailAddress,
      });
      notification.success(t('supplierEmails.create') + ' \u2713');
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
  if (errorCode === 'DUPLICATE_SUPPLIER_EMAIL') {
    fieldErrors.emailAddress = [err.response?.data?.detail || t('errors.DUPLICATE_SUPPLIER_EMAIL')];
  } else {
    notification.error(getApiErrorMessage(err, t));
  }
}

function cancel(): void {
  visible.value = false;
}
</script>
