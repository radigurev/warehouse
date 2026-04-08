<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="600" :title="isEdit ? t('customers.edit') : t('customers.create')" :icon="isEdit ? 'mdi-account-edit' : 'mdi-account-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('customers.edit') : t('customers.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('customers.form.name')"
              prepend-inner-icon="mdi-domain"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
              :error-messages="fieldErrors.name"
              @update:model-value="fieldErrors.name = []"
            />
          </v-col>

          <v-col v-if="!isEdit" v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.code"
              :label="t('customers.form.code')"
              prepend-inner-icon="mdi-identifier"
              :density="layout.vuetifyDensity"
              :hint="t('customers.form.codeHint')"
              persistent-hint
              :rules="[rules.codeFormat, rules.codeLength]"
              :error-messages="fieldErrors.code"
              @update:model-value="fieldErrors.code = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.nativeLanguageName"
              :label="t('customers.form.nativeLanguageName')"
              prepend-inner-icon="mdi-translate"
              :density="layout.vuetifyDensity"
              :rules="[rules.nativeNameLength]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.taxId"
              :label="t('customers.form.taxId')"
              prepend-inner-icon="mdi-card-account-details"
              :density="layout.vuetifyDensity"
              :rules="[rules.taxIdLength]"
              :error-messages="fieldErrors.taxId"
              @update:model-value="fieldErrors.taxId = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.categoryId"
              :label="t('customers.form.category')"
              prepend-inner-icon="mdi-tag"
              :density="layout.vuetifyDensity"
              :items="categories"
              item-title="name"
              item-value="id"
              clearable
              :loading="categoriesLoading"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('customers.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.notesLength]"
              rows="3"
              auto-grow
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
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { createCustomer, updateCustomer, getCustomerById } from '@features/customers/api/customers';
import { getAllCategories } from '@features/customers/api/categories';
import type { CustomerDto, CustomerCategoryDto } from '@features/customers/types/customer';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  customer?: CustomerDto | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const categories = ref<CustomerCategoryDto[]>([]);
const categoriesLoading = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({
  name: [],
  code: [],
  taxId: [],
});

const form = reactive({
  name: '',
  code: '',
  nativeLanguageName: '',
  taxId: '',
  categoryId: null as number | null,
  notes: '',
});

async function loadCategories(): Promise<void> {
  categoriesLoading.value = true;
  try {
    categories.value = await getAllCategories();
  } catch {
    // silent — categories dropdown will be empty
  } finally {
    categoriesLoading.value = false;
  }
}

onMounted(() => loadCategories());

async function populateForm(): Promise<void> {
  if (visible.value && props.customer) {
    isEdit.value = true;
    try {
      const detail = await getCustomerById(props.customer.id);
      form.name = detail.name;
      form.code = detail.code;
      form.nativeLanguageName = detail.nativeLanguageName ?? '';
      form.taxId = detail.taxId ?? '';
      form.categoryId = detail.categoryId;
      form.notes = detail.notes ?? '';
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  } else if (visible.value) {
    isEdit.value = false;
    form.name = '';
    form.code = '';
    form.nativeLanguageName = '';
    form.taxId = '';
    form.categoryId = null;
    form.notes = '';
  }
  fieldErrors.name = [];
  fieldErrors.code = [];
  fieldErrors.taxId = [];
}

watch(visible, populateForm);
watch(() => props.customer, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  nameLength: (v: string) => !v || v.length <= 200 || t('validation.customerNameLength'),
  codeFormat: (v: string) => !v || /^[a-zA-Z0-9-]*$/.test(v) || t('validation.codeFormat'),
  codeLength: (v: string) => !v || v.length <= 20 || t('validation.codeLength'),
  nativeNameLength: (v: string) => !v || v.length <= 200 || t('validation.nativeNameLength'),
  taxIdLength: (v: string) => !v || v.length <= 50 || t('validation.taxIdLength'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.customer) {
      await updateCustomer(props.customer.id, {
        name: form.name,
        nativeLanguageName: form.nativeLanguageName || null,
        taxId: form.taxId || null,
        categoryId: form.categoryId,
        notes: form.notes || null,
      });
      notification.success(t('customers.edit') + ' \u2713');
    } else {
      await createCustomer({
        name: form.name,
        code: form.code || null,
        nativeLanguageName: form.nativeLanguageName || null,
        taxId: form.taxId || null,
        categoryId: form.categoryId,
        notes: form.notes || null,
      });
      notification.success(t('customers.create') + ' \u2713');
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
  if (errorCode === 'DUPLICATE_CUSTOMER_CODE') {
    fieldErrors.code = [err.response?.data?.detail || t('errors.DUPLICATE_CUSTOMER_CODE')];
  } else if (errorCode === 'DUPLICATE_TAX_ID') {
    fieldErrors.taxId = [err.response?.data?.detail || t('errors.DUPLICATE_TAX_ID')];
  } else if (errorCode === 'CATEGORY_NOT_FOUND') {
    notification.error(t('errors.CATEGORY_NOT_FOUND'));
  } else if (errorCode) {
    const key = `errors.${errorCode}`;
    const translated = t(key);
    notification.error(translated !== key ? translated : t('errors.UNEXPECTED_ERROR'));
  } else {
    notification.error(getApiErrorMessage(err, t));
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
