import type { ApiClient } from './client'
import type { ApiPagedResult } from './contracts'

export const timelineEntityTypes = [
  'property',
  'resident',
  'contract',
  'payment',
  'utilityAccount',
  'document',
  'pet',
  'vehicle',
  'occurrence',
  'inspection',
  'identityUser',
] as const

export type TimelineEntityType = (typeof timelineEntityTypes)[number]

export type TimelineFilters = {
  actorUserId?: string
  entityType?: TimelineEntityType | ''
  eventType?: string
  from?: string
  page?: number
  pageSize?: number
  relatedEntityId?: string
  relatedEntityType?: TimelineEntityType | ''
  to?: string
}

export type TimelineActor = {
  displayName?: string
  kind: string
  kindLabel: string
  userId?: string
}

export type TimelineEntityReference = {
  displayName?: string
  entityId: string
  entityType: TimelineEntityType | string
  entityTypeLabel: string
  route?: string
}

export type TimelineEntry = {
  actor: TimelineActor
  correlationId?: string
  data: Record<string, string>
  display: {
    eventLabel: string
    summary: string
  }
  eventId: string
  eventType: string
  eventTypeLabel: string
  id: string
  moduleName: string
  occurredAt: string
  relatedEntities: TimelineEntityReference[]
  route?: string
  subject: TimelineEntityReference
}

export function listTimeline(client: ApiClient, filters: TimelineFilters = {}, locale?: string) {
  return client.get<ApiPagedResult<TimelineEntry>>('/v1/timeline', {
    query: {
      actorUserId: filters.actorUserId,
      entityType: filters.entityType,
      eventType: filters.eventType,
      from: filters.from,
      locale,
      page: filters.page,
      pageSize: filters.pageSize,
      relatedEntityId: filters.relatedEntityId,
      relatedEntityType: filters.relatedEntityType,
      to: filters.to,
    },
  })
}

export function listEntityTimeline(
  client: ApiClient,
  entityType: TimelineEntityType | string,
  entityId: string,
  filters: Omit<TimelineFilters, 'entityType'> = {},
  locale?: string,
) {
  return client.get<ApiPagedResult<TimelineEntry>>(
    `/v1/timeline/entities/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`,
    {
      query: {
        actorUserId: filters.actorUserId,
        eventType: filters.eventType,
        from: filters.from,
        locale,
        page: filters.page,
        pageSize: filters.pageSize,
        relatedEntityId: filters.relatedEntityId,
        relatedEntityType: filters.relatedEntityType,
        to: filters.to,
      },
    },
  )
}
