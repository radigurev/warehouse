<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="500" :title="isEdit ? t('productCategories.edit') : t('productCategories.create')" :icon="isEdit ? 'mdi-tag-edit' : 'mdi-tag-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('productCategories.edit') : t('productCategories.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('productCategories.form.name')"
              prepend-inner-icon="mdi-tag"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
              :error-messages="fieldErrors.name"
              @update:model-value="fieldErrors.name = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.parentCategoryId"
              :label="t('productCategories.form.parentCategory')"
              prepend-inner-icon="mdi-tag-multiple"
              :density="layout.vuetifyDensity"
              :items="availableParentCategories"
              item-title="name"
              item-value="id"
              clearable
              :loading="parentCategoriesLoading"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.description"
              :label="t('productCategories.form.description')"
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
import { ref, reactive, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { getAllCategories, createCategory, updateCategory } from '@features/inventory/api/product-categories';
import type { ProductCategoryDto } from '@features/inventory/types/inventory';
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
  category?: ProductCategoryDto | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const allCategories = ref<ProductCategoryDto[]>([]);
const parentCategoriesLoading = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({
  name: [],
});

const form = reactive({
  name: '',
  description: '',
  parentCategoryId: null as number | null,
});

const availableParentCategories = computed(() => {
  if (!isEdit.value || !props.category) return allCategories.value;
  return allCategories.value.filter((c) => c.id !== props.category!.id);
});

async function loadParentCategories(): Promise<void> {
  parentCategoriesLoading.value = true;
  try {
    allCategories.value = await getAllCategories();
  } catch {
    // silent — dropdown will be empty
  } finally {
    parentCategoriesLoading.value = false;
  }
}

onMounted(() => loadParentCategories());

function populateForm(): void {
  if (visible.value && props.category) {
    isEdit.value = true;
    form.name = props.category.name;
    form.description = props.category.description ?? '';
    form.parentCategoryId = props.category.parentCategoryId;
  } else if (visible.value) {
    isEdit.value = false;
    form.name = '';
    form.description = '';
    form.parentCategoryId = null;
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
        parentCategoryId: form.parentCategoryId,
      });
      notification.success(t('productCategories.edit') + ' \u2713');
    } else {
      await createCategory({
        name: form.name,
        description: form.description || null,
        parentCategoryId: form.parentCategoryId,
      });
      notification.success(t('productCategories.create') + ' \u2713');
    }
    visible.value = false;
    emit('saved');
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode === 'DUPLICATE_PRODUCT_CATEGORY_NAME') {
      fieldErrors.name = [axiosError.response?.data?.detail || t('errors.DUPLICATE_PRODUCT_CATEGORY_NAME')];
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
