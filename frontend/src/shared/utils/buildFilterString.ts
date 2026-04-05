import type { ColumnFilterState, FilterOperator } from '@shared/types/filter';

const operatorMap: Record<FilterOperator, string> = {
  contains: 'cn',
  startsWith: 'sw',
  endsWith: 'ew',
  equals: 'eq',
  notEquals: 'nq',
};

export function buildFilterString(
  filters: Record<string, ColumnFilterState | null>,
  pathMap?: Record<string, string>,
): string | undefined {
  const clauses: string[] = [];

  for (const [key, filter] of Object.entries(filters)) {
    if (!filter?.value) continue;

    const path = pathMap?.[key] ?? key;
    const op = operatorMap[filter.operator];
    const escaped = filter.value.replace(/'/g, "\\'");

    clauses.push(`(${path},${op},'${escaped}')`);
  }

  return clauses.length > 0 ? clauses.join('and') : undefined;
}
