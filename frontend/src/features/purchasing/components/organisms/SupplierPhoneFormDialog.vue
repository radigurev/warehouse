<template>
  <v-dialog v-model="visible" max-width="500" persistent>
    <v-card>
      <v-card-title class="text-h6">
        <v-icon class="mr-2">mdi-phone</v-icon>
        {{ isEdit ? t('supplierPhones.edit') : t('supplierPhones.create') }}
      </v-card-title>

      <v-card-text>
        <v-form ref="formRef" @submit.prevent="handleSubmit">
          <v-row dense>
            <v-col cols="12" md="6">
              <v-select
                v-model="form.phoneType"
                :label="t('supplierPhones.form.phoneType')"
                prepend-inner-icon="mdi-phone-settings"
                density="compact"
                :items="phoneTypes"
                :rules="[rules.required]"
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field
                v-model="form.phoneNumber"
                :label="t('supplierPhones.form.phoneNumber')"
                prepend-inner-icon="mdi-phone"
                density="compact"
                :rules="[rules.required, rules.phoneLength]"
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field
                v-model="form.extension"
                :label="t('supplierPhones.form.extension')"
                prepend-inner-icon="mdi-phone-plus"
                density="compact"
                :rules="[rules.extensionLength]"
              />
            </v-col>

            <v-col v-if="isEdit" cols="12" md="6">
              <v-switch
                v-model="form.isPrimary"
                :label="t('supplierPhones.form.isPrimary')"
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
import { createSupplierPhone, updateSupplierPhone } from '@features/purchasing/api/supplier-contacts';
import type { SupplierPhoneDto } from '@features/purchasing/types/purchasing';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  supplierId: number;
  phone?: SupplierPhoneDto | null;
}>();

const emit = defineEmits<{
  saved: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);

const phoneTypes = ['Mobile', 'Landline', 'Fax'];

const form = reactive({
  phoneType: 'Mobile' as string,
  phoneNumber: '',
  extension: '',
  isPrimary: false,
});

function populateForm(): void {
  if (visible.value && props.phone) {
    isEdit.value = true;
    form.phoneType = props.phone.phoneType;
    form.phoneNumber = props.phone.phoneNumber;
    form.extension = props.phone.extension ?? '';
    form.isPrimary = props.phone.isPrimary;
  } else if (visible.value) {
    isEdit.value = false;
    form.phoneType = 'Mobile';
    form.phoneNumber = '';
    form.extension = '';
    form.isPrimary = false;
  }
}

watch(visible, populateForm);
watch(() => props.phone, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  phoneLength: (v: string) => !v || v.length <= 30 || t('validation.phoneLength'),
  extensionLength: (v: string) => !v || v.length <= 10 || t('validation.extensionLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.phone) {
      await updateSupplierPhone(props.supplierId, props.phone.id, {
        phoneType: form.phoneType,
        phoneNumber: form.phoneNumber,
        extension: form.extension || null,
        isPrimary: form.isPrimary,
      });
      notification.success(t('supplierPhones.edit') + ' \u2713');
    } else {
      await createSupplierPhone(props.supplierId, {
        phoneType: form.phoneType,
        phoneNumber: form.phoneNumber,
        extension: form.extension || null,
      });
      notification.success(t('supplierPhones.create') + ' \u2713');
    }
    visible.value = false;
    emit('saved');
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  } finally {
    loading.value = false;
  }
}

function cancel(): void {
  visible.value = false;
}
</script>
