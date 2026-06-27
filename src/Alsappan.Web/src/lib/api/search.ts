import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'

export type GlobalSearchResult = {
  entityType: string
  entityTypeLabel: string
  id: string
  label: string
  matchedField: string
  route: string
  summary?: string
}

export type GlobalSearchGroup = {
  entityType: string
  entityTypeLabel: string
  results: GlobalSearchResult[]
  route: string
}

export type GlobalSearchResponse = {
  groups: GlobalSearchGroup[]
  query: string
  totalItems: number
}

export type GlobalSearchContract = {
  entityTypes: Array<{
    entityType: string
    entityTypeLabel: string
    readPermission: string
  }>
  matchedFields: Array<{
    label: string
    value: string
  }>
}

export function globalSearch(
  client: ApiClient,
  query: string,
  options: { limit?: number; locale?: AppLocale } = {},
) {
  return client.get<GlobalSearchResponse>('/v1/search', {
    query: {
      limit: options.limit,
      locale: options.locale,
      query,
    },
  })
}

export function getGlobalSearchContract(client: ApiClient, locale?: AppLocale) {
  return client.get<GlobalSearchContract>('/v1/search/contract', {
    query: { locale },
  })
}
