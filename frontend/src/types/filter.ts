export type FilterOperator = 'contains' | 'startsWith' | 'endsWith' | 'equals' | 'notEquals';

export interface ColumnFilterState {
  operator: FilterOperator;
  value: string;
}
