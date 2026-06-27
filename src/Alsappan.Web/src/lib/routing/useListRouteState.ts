import { useEffect, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { FilterSet, FilterValue } from '../../stores/useFiltersStore'

type RouteFilterType = 'boolean' | 'number' | 'string'

export type RouteFilterDefinition = {
  filterKey: string
  params: readonly string[]
  type?: RouteFilterType
}

type UseListRouteStateOptions = {
  detailParams?: readonly string[]
  getAdditionalFilters?: (searchParams: URLSearchParams) => FilterSet
  onDetailIdChange?: (id: string | null) => void
  pageSize?: number
  routeFilters?: readonly RouteFilterDefinition[]
  scope: string
  setFilters: (scope: string, filters: FilterSet) => void
}

const trueValues = new Set(['1', 'sim', 'true', 'yes'])
const falseValues = new Set(['0', 'false', 'nao', 'no'])

function getRouteParam(
  searchParams: URLSearchParams,
  params: readonly string[],
  type: RouteFilterType,
) {
  for (const param of params) {
    if (!searchParams.has(param)) {
      continue
    }

    const value = searchParams.get(param)?.trim() ?? ''
    if (value || type === 'boolean') {
      return value
    }
  }

  return undefined
}

function parseRouteValue(value: string, type: RouteFilterType): FilterValue | undefined {
  if (type === 'boolean') {
    if (value === '') {
      return true
    }

    const normalized = value.toLocaleLowerCase('pt-BR')
    if (trueValues.has(normalized)) {
      return true
    }

    if (falseValues.has(normalized)) {
      return false
    }

    return undefined
  }

  if (type === 'number') {
    const numericValue = Number(value.replace(',', '.'))
    return Number.isFinite(numericValue) ? numericValue : undefined
  }

  return value || undefined
}

function getRouteFilters(
  searchParams: URLSearchParams,
  routeFilters: readonly RouteFilterDefinition[],
) {
  const filters: FilterSet = {}

  for (const routeFilter of routeFilters) {
    const type = routeFilter.type ?? 'string'
    const routeValue = getRouteParam(searchParams, routeFilter.params, type)

    if (routeValue === undefined) {
      continue
    }

    const filterValue = parseRouteValue(routeValue, type)

    if (filterValue !== undefined) {
      filters[routeFilter.filterKey] = filterValue
    }
  }

  return filters
}

function getDetailId(searchParams: URLSearchParams, detailParams: readonly string[] | undefined) {
  if (!detailParams) {
    return null
  }

  for (const param of detailParams) {
    const detailId = searchParams.get(param)?.trim()
    if (detailId) {
      return detailId
    }
  }

  return null
}

export function useListRouteState({
  detailParams,
  getAdditionalFilters,
  onDetailIdChange,
  pageSize = 10,
  routeFilters = [],
  scope,
  setFilters,
}: UseListRouteStateOptions) {
  const [searchParams] = useSearchParams()
  const routeSearch = searchParams.toString()
  const routeState = useMemo(() => {
    const currentSearchParams = new URLSearchParams(routeSearch)
    const filters = {
      ...(getAdditionalFilters?.(currentSearchParams) ?? {}),
      ...getRouteFilters(currentSearchParams, routeFilters),
    }

    return {
      detailId: getDetailId(currentSearchParams, detailParams),
      filters,
      hasFilters: Object.keys(filters).length > 0,
    }
  }, [detailParams, getAdditionalFilters, routeFilters, routeSearch])

  useEffect(() => {
    if (routeState.hasFilters) {
      setFilters(scope, {
        ...routeState.filters,
        page: 1,
        pageSize,
      })
    }

    onDetailIdChange?.(routeState.detailId)
  }, [
    onDetailIdChange,
    pageSize,
    routeState.detailId,
    routeState.filters,
    routeState.hasFilters,
    scope,
    setFilters,
  ])
}
