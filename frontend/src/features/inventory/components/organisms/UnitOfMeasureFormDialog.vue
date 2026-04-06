<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="500" :title="isEdit ? t('unitsOfMeasure.edit') : t('unitsOfMeasure.create')" :icon="isEdit ? 'mdi-ruler' : 'mdi-ruler'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('unitsOfMeasure.edit') : t('unitsOfMeasure.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-if="!isEdit" v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.code"
              :label="t('unitsOfMeasure.form.code')"
              prepend-inner-icon="mdi-identifier"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.codeFormat, rules.codeLength]"
              :error-messages="fieldErrors.code"
              @update:model-value="fieldErrors.code = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('unitsOfMeasure.form.name')"
              prepend-inner-icon="mdi-ruler"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
              :error-messages="fieldErrors.name"
              @update:model-value="fieldErrors.name = []"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.description"
              :label="t('unitsOfMeasure.form.description')"
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
import { createUnit, updateUnit } from '@features/inventory/api/units-of-measure';
import type { UnitOfMeasureDto } from '@features/inventory/types/inventory';
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
  unit?: UnitOfMeasureDto | null;
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
  code: [],
  name: [],
});

const form = reactive({
  code: '',
  name: '',
  description: '',
});

function populateForm(): void {
  if (visible.value && props.unit) {
    isEdit.value = true;
    form.code = props.unit.code;
    form.name = props.unit.name;
    form.description = props.unit.description ?? '';
  } else if (visible.value) {
    isEdit.value = false;
    form.code = '';
    form.name = '';
    form.description = '';
  }
  fieldErrors.code = [];
  fieldErrors.name = [];
}

watch(visible, populateForm);
watch(() => props.unit, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  nameLength: (v: string) => !v || v.length <= 100 || t('validation.categoryNameLength'),
  codeFormat: (v: string) => !v || /^[a-zA-Z0-9-]*$/.test(v) || t('validation.codeFormat'),
  codeLength: (v: string) => !v || v.length <= 20 || t('validation.codeLength'),
  descriptionLength: (v: string) => !v || v.length <= 500 || t('validation.descriptionLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.unit) {
      await updateUnit(props.unit.id, {
        name: form.name,
        description: form.description || null,
      });
      notification.success(t('unitsOfMeasure.edit') + ' \u2713');
    } else {
      await createUnit({
        code: form.code,
        name: form.name,
        description: form.description || null,
      });
      notification.success(t('unitsOfMeasure.create') + ' \u2713');
    }
    visible.value = false;
    emit('saved');
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode === 'DUPLICATE_UNIT_CODE') {
      fieldErrors.code = [axiosError.response?.data?.detail || t('errors.DUPLICATE_UNIT_CODE')];
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
