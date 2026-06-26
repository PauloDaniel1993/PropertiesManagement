import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type FilterPrimitive = boolean | number | string
export type FilterRange = {
  from?: string
  to?: string
}
export type FilterValue = FilterPrimitive | FilterPrimitive[] | FilterRange | null
export type FilterSet = Record<string, FilterValue>

export type FiltersState = {
  clearFilter: (scope: string, key: string) => void
  filtersByScope: Record<string, FilterSet>
  resetFilters: (scope?: string) => void
  setFilter: (scope: string, key: string, value: FilterValue) => void
  setFilters: (scope: string, filters: FilterSet) => void
}

function withoutFilter(filters: FilterSet, key: string) {
  const nextFilters = { ...filters }
  delete nextFilters[key]

  return nextFilters
}

export const useFiltersStore = create<FiltersState>()(
  persist(
    (set) => ({
      clearFilter: (scope, key) =>
        set((state) => ({
          filtersByScope: {
            ...state.filtersByScope,
            [scope]: withoutFilter(state.filtersByScope[scope] ?? {}, key),
          },
        })),
      filtersByScope: {},
      resetFilters: (scope) =>
        set((state) => {
          if (!scope) {
            return { filtersByScope: {} }
          }

          const nextFiltersByScope = { ...state.filtersByScope }
          delete nextFiltersByScope[scope]

          return { filtersByScope: nextFiltersByScope }
        }),
      setFilter: (scope, key, value) =>
        set((state) => ({
          filtersByScope: {
            ...state.filtersByScope,
            [scope]: {
              ...state.filtersByScope[scope],
              [key]: value,
            },
          },
        })),
      setFilters: (scope, filters) =>
        set((state) => ({
          filtersByScope: {
            ...state.filtersByScope,
            [scope]: filters,
          },
        })),
    }),
    {
      name: 'alsappan.filters',
      partialize: (state) => ({
        filtersByScope: state.filtersByScope,
      }),
    },
  ),
)
