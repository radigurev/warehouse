import { reactive, computed, type Ref } from 'vue';
import type { ColumnFilterState } from '@/types/filter';

function matchesFilter(value: string, filter: ColumnFilterState): boolean {
  if (!filter.value) return true;
  const v = value.toLowerCase();
  const f = filter.value.toLowerCase();

  switch (filter.operator) {
    case 'contains': return v.includes(f);
    case 'startsWith': return v.startsWith(f);
    case 'endsWith': return v.endsWith(f);
    case 'equals': return v === f;
    case 'notEquals': return v !== f;
    default: return true;
  }
}

export function useColumnFilters<T>(
  items: Ref<T[]>,
  filterableKeys: string[],
) {
  const columnFilters = reactive<Record<string, ColumnFilterState | null>>(
    Object.fromEntries(filterableKeys.map((k) => [k, null])),
  );

  const filteredItems = computed<T[]>(() => {
    let result = items.value;

    for (const [key, filter] of Object.entries(columnFilters)) {
      if (filter && filter.value) {
        result = result.filter((item) =>
          matchesFilter(String((item as Record<string, unknown>)[key] ?? ''), filter),
        );
      }
    }

    return result;
  });

  return { columnFilters, filteredItems };
}
