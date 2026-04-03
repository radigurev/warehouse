import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export type DensityMode = 'compact' | 'comfortable';
export type FormDisplayMode = 'modal' | 'page';

function getStoredFormDisplayMode(): FormDisplayMode {
  const stored = localStorage.getItem('formDisplayMode');
  if (stored === 'modal' || stored === 'page') return stored;
  return 'modal';
}

export const useLayoutStore = defineStore('layout', () => {
  const storedMode = localStorage.getItem('densityMode') as DensityMode | null;
  const densityMode = ref<DensityMode>(storedMode || 'compact');
  const sidebarCollapsed = ref(false);
  const formDisplayMode = ref<FormDisplayMode>(getStoredFormDisplayMode());

  const isCompact = computed(() => densityMode.value === 'compact');
  const vuetifyDensity = computed(() => (densityMode.value === 'compact' ? 'compact' : 'default'));
  const isPageMode = computed(() => formDisplayMode.value === 'page');

  function toggleDensity(): void {
    densityMode.value = densityMode.value === 'compact' ? 'comfortable' : 'compact';
    localStorage.setItem('densityMode', densityMode.value);
  }

  function setFormDisplayMode(mode: FormDisplayMode): void {
    formDisplayMode.value = mode;
    localStorage.setItem('formDisplayMode', mode);
  }

  function toggleSidebar(): void {
    sidebarCollapsed.value = !sidebarCollapsed.value;
  }

  return {
    densityMode,
    sidebarCollapsed,
    formDisplayMode,
    isCompact,
    vuetifyDensity,
    isPageMode,
    toggleDensity,
    setFormDisplayMode,
    toggleSidebar,
  };
});
