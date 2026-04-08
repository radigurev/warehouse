<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="500" :title="isEdit ? t('categories.edit') : t('categories.create')" :icon="isEdit ? 'mdi-tag-edit' : 'mdi-tag-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('categories.edit') : t('categories.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('categories.form.name')"
              prepend-inner-icon="mdi-tag"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
              :error-messages="fieldErrors.name"
              @update:model-value="fieldErrors.name = []"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.description"
              :label="t('categories.form.description')"
              prepend-inner-icon="mdi-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.descriptionLength]"
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
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { createCategory, updateCategory } from '@features/customers/api/categories';
import type { CustomerCategoryDto } from '@features/customers/types/customer';
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
  category?: CustomerCategoryDto | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({
  name: [],
});

const form = reactive({
  name: '',
  description: '',
});

function populateForm(): void {
  if (visible.value && props.category) {
    isEdit.value = true;
    form.name = props.category.name;
    form.description = props.category.description ?? '';
  } else if (visible.value) {
    isEdit.value = false;
    form.name = '';
    form.description = '';
  }
  fieldErrors.name = [];
}

watch(visible, populateForm);
watch(() => props.category, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  nameLength: (v: string) => !v || v.length <= 100 || t('validation.categoryNameLength'),
  descriptionLength: (v: string) => !v || v.length <= 500 || t('validation.descriptionLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.category) {
      await updateCategory(props.category.id, {
        name: form.name,
        description: form.description || null,
      });
      notification.success(t('categories.edit') + ' \u2713');
    } else {
      await createCategory({
        name: form.name,
        description: form.description || null,
      });
      notification.success(t('categories.create') + ' \u2713');
    }
    visible.value = false;
    emit('saved');
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode === 'DUPLICATE_CATEGORY_NAME') {
      fieldErrors.name = [axiosError.response?.data?.detail || t('errors.DUPLICATE_CATEGORY_NAME')];
    } else {
      notification.error(getApiErrorMessage(err, t));
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
