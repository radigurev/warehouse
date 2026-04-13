import { ref, type Ref } from 'vue';
import { buildFilterString } from '@shared/utils/buildFilterString';
import type { ColumnFilterState } from '@shared/types/filter';

export interface SearchParamsConfig<TSearch> {
  defaults: TSearch;
  filterPathMap?: Record<string, string>;
  onChange: () => void;
}

/**
 * Manages reactive search parameters with pagination, sorting, and filter integration.
 * Calls onChange whenever params change so the view can re-fetch.
 */
// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export function useSearchParams<TSearch extends {}>(
  config: SearchParamsConfig<TSearch>,
) {
  const params = ref<TSearch>({ ...config.defaults }) as Ref<TSearch>;

  function updatePage(page: number): void {
    params.value = { ...params.value, page } as TSearch;
    config.onChange();
  }

  function updatePageSize(size: number): void {
    params.value = { ...params.value, pageSize: size, page: 1 } as TSearch;
    config.onChange();
  }

  function applyFilters(columnFilters: Record<string, ColumnFilterState | null>): void {
    const filter = buildFilterString(columnFilters, config.filterPathMap);
    params.value = { ...params.value, filter, page: 1 } as TSearch;
    config.onChange();
  }

  function updateSort(field: string, descending: boolean): void {
    params.value = { ...params.value, sortBy: field, sortDescending: descending, page: 1 } as TSearch;
    config.onChange();
  }

  return { params, updatePage, updatePageSize, applyFilters, updateSort };
}
