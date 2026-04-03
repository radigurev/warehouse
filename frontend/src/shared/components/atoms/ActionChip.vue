<template>
  <v-chip
    :size="compact ? 'x-small' : 'small'"
    variant="tonal"
    :color="color"
    :prepend-icon="icon"
    class="mr-1"
    @click="$emit('click')"
  >
    {{ label }}
  </v-chip>
</template>

<script setup lang="ts">
defineProps<{
  label: string;
  icon: string;
  color?: string;
  compact?: boolean;
}>();

defineEmits<{
  click: [];
}>();
</script>
