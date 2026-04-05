<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="500" :title="t('permissions.create')" icon="mdi-key-plus" @back="cancel">
    <v-card-title class="text-h6">{{ t('permissions.create') }}</v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.resource"
              :label="t('permissions.form.resource')"
              prepend-inner-icon="mdi-cube-outline"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.resourceFormat, rules.resourceLength]"
              :error-messages="fieldErrors.resource"
              @update:model-value="fieldErrors.resource = []"
              hint="e.g. users, inventory.products"
              persistent-hint
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.action"
              :label="t('permissions.form.action')"
              prepend-inner-icon="mdi-cog"
              :density="layout.vuetifyDensity"
              :items="actionItems"
              :rules="[rules.required]"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.description"
              :label="t('permissions.form.description')"
              prepend-inner-icon="mdi-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.descriptionLength]"
              rows="2"
              variant="outlined"
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
import { ref, reactive, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { createPermission } from '@features/auth/api/permissions';
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

defineProps<{
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const formRef = ref();
const loading = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({ resource: [] });

const form = reactive({
  resource: '',
  action: '',
  description: '',
});

const actionItems = computed(() => [
  { title: t('permissions.actions.read'), value: 'read' },
  { title: t('permissions.actions.write'), value: 'write' },
  { title: t('permissions.actions.update'), value: 'update' },
  { title: t('permissions.actions.delete'), value: 'delete' },
  { title: t('permissions.actions.all'), value: 'all' },
]);

watch(visible, (val) => {
  if (val) {
    form.resource = '';
    form.action = '';
    form.description = '';
    fieldErrors.resource = [];
  }
});

const rules = {
  required: (v: string) => !!v || t('common.required'),
  resourceFormat: (v: string) => /^[a-z0-9.]+$/.test(v) || t('validation.resourceFormat'),
  resourceLength: (v: string) => v.length <= 100 || t('validation.resourceLength'),
  descriptionLength: (v: string) => v.length <= 500 || t('validation.descriptionLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    const description = form.description || null;
    await createPermission({ resource: form.resource, action: form.action, description });
    notification.success(t('permissions.create') + ' ✓');
    visible.value = false;
    emit('saved');
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode === 'DUPLICATE_PERMISSION') {
      fieldErrors.resource = [t('errors.DUPLICATE_PERMISSION')];
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
