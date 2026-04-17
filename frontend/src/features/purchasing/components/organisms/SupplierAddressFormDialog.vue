<template>
  <v-dialog v-model="visible" max-width="600" persistent>
    <v-card>
      <v-card-title class="text-h6">
        <v-icon class="mr-2">mdi-map-marker</v-icon>
        {{ isEdit ? t('supplierAddresses.edit') : t('supplierAddresses.create') }}
      </v-card-title>

      <v-card-text>
        <v-form ref="formRef" @submit.prevent="handleSubmit">
          <v-row dense>
            <v-col cols="12" md="6">
              <v-select
                v-model="form.addressType"
                :label="t('supplierAddresses.form.addressType')"
                prepend-inner-icon="mdi-home-variant"
                density="compact"
                :items="addressTypes"
                :rules="[rules.required]"
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                v-model="form.streetLine1"
                :label="t('supplierAddresses.form.streetLine1')"
                prepend-inner-icon="mdi-road-variant"
                density="compact"
                :rules="[rules.required, rules.streetLength]"
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                v-model="form.streetLine2"
                :label="t('supplierAddresses.form.streetLine2')"
                prepend-inner-icon="mdi-road-variant"
                density="compact"
                :rules="[rules.streetLength]"
              />
            </v-col>

            <v-col cols="12">
              <NomenclatureAddressFields
                v-model:country-code="form.countryCode"
                v-model:state-province="form.stateProvince"
                v-model:city="form.city"
                density="compact"
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field
                v-model="form.postalCode"
                :label="t('supplierAddresses.form.postalCode')"
                prepend-inner-icon="mdi-mailbox"
                density="compact"
                :rules="[rules.required, rules.postalCodeLength]"
              />
            </v-col>

            <v-col v-if="isEdit" cols="12" md="6">
              <v-switch
                v-model="form.isDefault"
                :label="t('supplierAddresses.form.isDefault')"
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
import { createSupplierAddress, updateSupplierAddress } from '@features/purchasing/api/supplier-contacts';
import NomenclatureAddressFields from '@shared/components/molecules/NomenclatureAddressFields.vue';
import type { SupplierAddressDto } from '@features/purchasing/types/purchasing';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  supplierId: number;
  address?: SupplierAddressDto | null;
}>();

const emit = defineEmits<{
  saved: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);

const addressTypes = ['Billing', 'Shipping', 'Both'];

const form = reactive({
  addressType: 'Billing' as string,
  streetLine1: '',
  streetLine2: '',
  city: '',
  stateProvince: '',
  postalCode: '',
  countryCode: '',
  isDefault: false,
});

function populateForm(): void {
  if (visible.value && props.address) {
    isEdit.value = true;
    form.addressType = props.address.addressType;
    form.streetLine1 = props.address.streetLine1;
    form.streetLine2 = props.address.streetLine2 ?? '';
    form.city = props.address.city;
    form.stateProvince = props.address.stateProvince ?? '';
    form.postalCode = props.address.postalCode;
    form.countryCode = props.address.countryCode;
    form.isDefault = props.address.isDefault;
  } else if (visible.value) {
    isEdit.value = false;
    form.addressType = 'Billing';
    form.streetLine1 = '';
    form.streetLine2 = '';
    form.city = '';
    form.stateProvince = '';
    form.postalCode = '';
    form.countryCode = '';
    form.isDefault = false;
  }
}

watch(visible, populateForm);
watch(() => props.address, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  streetLength: (v: string) => !v || v.length <= 200 || t('validation.streetLength'),
  postalCodeLength: (v: string) => !v || v.length <= 20 || t('validation.postalCodeLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.address) {
      await updateSupplierAddress(props.supplierId, props.address.id, {
        addressType: form.addressType,
        streetLine1: form.streetLine1,
        streetLine2: form.streetLine2 || null,
        city: form.city,
        stateProvince: form.stateProvince || null,
        postalCode: form.postalCode,
        countryCode: form.countryCode.toUpperCase(),
        isDefault: form.isDefault,
      });
      notification.success(t('supplierAddresses.edit') + ' \u2713');
    } else {
      await createSupplierAddress(props.supplierId, {
        addressType: form.addressType,
        streetLine1: form.streetLine1,
        streetLine2: form.streetLine2 || null,
        city: form.city,
        stateProvince: form.stateProvince || null,
        postalCode: form.postalCode,
        countryCode: form.countryCode.toUpperCase(),
      });
      notification.success(t('supplierAddresses.create') + ' \u2713');
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
