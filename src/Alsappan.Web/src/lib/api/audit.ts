import type { ApiClient } from './client'
import type { ApiPagedResult } from './contracts'

export type AuditCategoryCode = 'Mutation' | 'Security' | 'System'

export type AuditCategoryLabel = {
  code: AuditCategoryCode | string
  label: string
  tone?: string
}

export type AuditEntry = {
  action: string
  actionLabel: string
  actorDisplayName: string
  actorKind: string
  actorUserId?: string
  category: AuditCategoryLabel
  changedFields: Record<string, string>
  context: Record<string, string>
  correlationId?: string
  id: string
  occurredAt: string
  targetDisplayName: string
  targetEntityId: string
  targetEntityType: string
}

export type AuditListFilters = {
  action?: string
  actor?: string
  category?: string
  entityId?: string
  entityType?: string
  from?: string
  page?: number
  pageSize?: number
  search?: string
  sort?: string
  to?: string
}

export function listAuditEntries(
  client: ApiClient,
  filters: AuditListFilters = {},
  locale?: string,
) {
  return client.get<ApiPagedResult<AuditEntry>>('/v1/audit', {
    query: {
      action: filters.action,
      actor: filters.actor,
      category: filters.category,
      entityId: filters.entityId,
      entityType: filters.entityType,
      from: filters.from,
      locale,
      page: filters.page,
      pageSize: filters.pageSize,
      search: filters.search,
      sort: filters.sort,
      to: filters.to,
    },
  })
}

export function getAuditEntry(client: ApiClient, entryId: string, locale?: string) {
  return client.get<AuditEntry>(`/v1/audit/${entryId}`, { query: { locale } })
}

export function listAuditCategoryOptions(client: ApiClient, locale?: string) {
  return client.get<AuditCategoryLabel[]>('/v1/audit/category-options', { query: { locale } })
}
