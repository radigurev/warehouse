import { computed, type ComputedRef, type Ref } from 'vue';
import { useRouter } from 'vue-router';
import type { RouteLocationRaw } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';

export interface NavigationStrategy<TItem> {
  navigateToCreate(): void;
  navigateToEdit(item: TItem): void;
  navigateToDetail(item: TItem): void;
}

export interface NavigationRoutes<TItem> {
  create?: RouteLocationRaw;
  edit?: (item: TItem) => RouteLocationRaw;
  detail?: (item: TItem) => RouteLocationRaw;
}

export interface NavigationDialogs<TItem> {
  selectedItem: Ref<TItem | null>;
  showFormDialog: Ref<boolean>;
  showDetailDialog: Ref<boolean>;
}

export interface NavigationConfig<TItem> {
  routes: NavigationRoutes<TItem>;
  dialogs: NavigationDialogs<TItem>;
}

/**
 * Returns a reactive navigation strategy that switches between page mode
 * (router.push) and dialog mode (set refs) based on the layout store.
 */
export function useNavigationStrategy<TItem>(
  config: NavigationConfig<TItem>,
): ComputedRef<NavigationStrategy<TItem>> {
  const router = useRouter();
  const layout = useLayoutStore();

  return computed<NavigationStrategy<TItem>>(() => {
    if (layout.isPageMode) {
      return {
        navigateToCreate: () => {
          if (config.routes.create) router.push(config.routes.create);
        },
        navigateToEdit: (item: TItem) => {
          if (config.routes.edit) router.push(config.routes.edit(item));
        },
        navigateToDetail: (item: TItem) => {
          if (config.routes.detail) router.push(config.routes.detail(item));
        },
      };
    }

    return {
      navigateToCreate: () => {
        config.dialogs.selectedItem.value = null;
        config.dialogs.showFormDialog.value = true;
      },
      navigateToEdit: (item: TItem) => {
        config.dialogs.selectedItem.value = item;
        config.dialogs.showFormDialog.value = true;
      },
      navigateToDetail: (item: TItem) => {
        config.dialogs.selectedItem.value = item;
        config.dialogs.showDetailDialog.value = true;
      },
    };
  });
}
