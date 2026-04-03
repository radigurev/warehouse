<template>
  <v-dialog v-if="mode === 'dialog'" v-model="model" :max-width="maxWidth" persistent>
    <v-card>
      <slot />
    </v-card>
  </v-dialog>
  <v-card v-else :max-width="maxWidth">
    <slot />
  </v-card>
</template>

<script setup lang="ts">
const model = defineModel<boolean>({ required: true });

defineProps<{
  mode?: 'dialog' | 'page';
  maxWidth?: string | number;
}>();
</script>
