import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult } from './contracts'

export type NotificationStatusTone =
  | 'archived'
  | 'danger'
  | 'info'
  | 'neutral'
  | 'success'
  | 'warning'
  | string

export type NotificationStatusLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: NotificationStatusTone
}

export type NotificationOption = {
  description?: string
  isDisabled?: boolean
  label: string
  value: string
}

export type NotificationListFilters = {
  category?: string
  channel?: string
  includeArchived?: boolean
  isRead?: boolean | ''
  locale?: AppLocale
  page?: number
  pageSize?: number
  search?: string
  sort?: string
}

export type NotificationListItem = {
  archivedAt?: string
  category: NotificationStatusLabel
  channel: NotificationStatusLabel
  correlationId?: string
  createdAt: string
  deepLink?: string
  deliveryStatus: NotificationStatusLabel
  eventId: string
  eventName: string
  eventTypeLabel: string
  id: string
  isArchived: boolean
  isRead: boolean
  message: string
  occurredAt: string
  payload: Record<string, string>
  readAt?: string
  readState: NotificationStatusLabel<'read' | 'unread'>
  recipientUserId?: string
  subjectDisplayName?: string
  subjectEntityId: string
  subjectEntityType: string
  title: string
}

export type NotificationUnreadCount = {
  count: number
}

export type NotificationMarkAllReadResult = {
  updatedCount: number
}

export type NotificationPreference = {
  category: string
  categoryLabel: string
  channel: string
  channelLabel: string
  isEnabled: boolean
  isMandatory: boolean
}

export type NotificationPreferences = {
  isPersisted: boolean
  notice: string
  preferences: NotificationPreference[]
}

export type NotificationPreferenceUpdateRequest = {
  preferences: Array<{
    category: string
    channel: string
    isEnabled: boolean
  }>
}

type ApiNotificationStatusLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

type ApiNotificationListItem = {
  archivedAt?: string
  category: ApiNotificationStatusLabel
  channel: ApiNotificationStatusLabel
  correlationId?: string
  createdAt: string
  deepLink?: string
  deliveryStatus: ApiNotificationStatusLabel
  eventId: string
  eventName: string
  eventTypeLabel: string
  id: string
  isArchived?: boolean
  isRead?: boolean
  message: string
  occurredAt: string
  payload?: Record<string, string>
  readAt?: string
  readState: ApiNotificationStatusLabel<'read' | 'unread'>
  recipientUserId?: string
  subjectDisplayName?: string
  subjectEntityId: string
  subjectEntityType: string
  title: string
}

function mapStatus<TCode extends string>(
  status: ApiNotificationStatusLabel<TCode>,
): NotificationStatusLabel<TCode> {
  return {
    code: status.code,
    label: status.label,
    tone: status.tone,
  }
}

function mapNotification(item: ApiNotificationListItem): NotificationListItem {
  return {
    archivedAt: item.archivedAt,
    category: mapStatus(item.category),
    channel: mapStatus(item.channel),
    correlationId: item.correlationId,
    createdAt: item.createdAt,
    deepLink: item.deepLink,
    deliveryStatus: mapStatus(item.deliveryStatus),
    eventId: item.eventId,
    eventName: item.eventName,
    eventTypeLabel: item.eventTypeLabel,
    id: item.id,
    isArchived: item.isArchived ?? false,
    isRead: item.isRead ?? item.readState.code === 'read',
    message: item.message,
    occurredAt: item.occurredAt,
    payload: item.payload ?? {},
    readAt: item.readAt,
    readState: mapStatus(item.readState),
    recipientUserId: item.recipientUserId,
    subjectDisplayName: item.subjectDisplayName,
    subjectEntityId: item.subjectEntityId,
    subjectEntityType: item.subjectEntityType,
    title: item.title,
  }
}

export async function listNotifications(client: ApiClient, filters: NotificationListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiNotificationListItem>>('/v1/notifications', {
    query: {
      category: filters.category,
      channel: filters.channel,
      includeArchived: filters.includeArchived,
      isRead: filters.isRead,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      search: filters.search,
      sort: filters.sort,
    },
  })

  return {
    ...page,
    items: page.items.map(mapNotification),
  }
}

export function getNotificationUnreadCount(client: ApiClient) {
  return client.get<NotificationUnreadCount>('/v1/notifications/unread-count')
}

export async function markNotificationRead(
  client: ApiClient,
  notificationId: string,
  locale?: AppLocale,
) {
  const notification = await client.post<ApiNotificationListItem, Record<string, never>>(
    `/v1/notifications/${notificationId}/read`,
    {},
    { query: { locale } },
  )

  return mapNotification(notification)
}

export function markAllNotificationsRead(client: ApiClient) {
  return client.post<NotificationMarkAllReadResult, Record<string, never>>(
    '/v1/notifications/read-all',
    {},
  )
}

export function archiveNotification(client: ApiClient, notificationId: string) {
  return client.delete<void>(`/v1/notifications/${notificationId}`)
}

export function listNotificationCategoryOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<NotificationOption[]>('/v1/notifications/category-options', {
    query: { locale },
  })
}

export function listNotificationChannelOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<NotificationOption[]>('/v1/notifications/channel-options', {
    query: { locale },
  })
}

export function getNotificationPreferences(client: ApiClient, locale?: AppLocale) {
  return client.get<NotificationPreferences>('/v1/notifications/preferences', {
    query: { locale },
  })
}

export function updateNotificationPreferences(
  client: ApiClient,
  request: NotificationPreferenceUpdateRequest,
  locale?: AppLocale,
) {
  return client.put<NotificationPreferences, NotificationPreferenceUpdateRequest>(
    '/v1/notifications/preferences',
    request,
    { query: { locale } },
  )
}
