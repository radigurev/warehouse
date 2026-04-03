import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export type DensityMode = 'compact' | 'comfortable';

export const useLayoutStore = defineStore('layout', () => {
  const storedMode = localStorage.getItem('densityMode') as DensityMode | null;
  const densityMode = ref<DensityMode>(storedMode || 'compact');
  const sidebarCollapsed = ref(false);

  const isCompact = computed(() => densityMode.value === 'compact');
  const vuetifyDensity = computed(() => (densityMode.value === 'compact' ? 'compact' : 'default'));

  function toggleDensity(): void {
    densityMode.value = densityMode.value === 'compact' ? 'comfortable' : 'compact';
    localStorage.setItem('densityMode', densityMode.value);
  }

  function toggleSidebar(): void {
    sidebarCollapsed.value = !sidebarCollapsed.value;
  }

  return {
    densityMode,
    sidebarCollapsed,
    isCompact,
    vuetifyDensity,
    toggleDensity,
    toggleSidebar,
  };
});
