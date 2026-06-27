import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult, ApiSelectOption } from './contracts'

export const occurrenceTypes = [
  'maintenance',
  'complaint',
  'incident',
  'request',
  'security',
  'noise',
  'other',
] as const

export type OccurrenceType = (typeof occurrenceTypes)[number]

export const occurrencePriorities = ['low', 'medium', 'high', 'urgent'] as const

export type OccurrencePriority = (typeof occurrencePriorities)[number]

export const occurrenceStatuses = [
  'open',
  'assigned',
  'in-progress',
  'waiting',
  'resolved',
  'cancelled',
  'archived',
] as const

export type OccurrenceStatus = (typeof occurrenceStatuses)[number]

export type OccurrenceLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

export type OccurrenceEntitySummary = {
  description?: string
  id: string
  name: string
  route?: string
}

export type OccurrenceUserSummary = {
  displayName: string
  email?: string
  id: string
}

export type OccurrenceListFilters = {
  assignedUserId?: string
  contractId?: string
  dateFrom?: string
  dateTo?: string
  includeArchived?: boolean
  locale?: AppLocale
  page?: number
  pageSize?: number
  priority?: OccurrencePriority | ''
  propertyId?: string
  residentId?: string
  search?: string
  sort?: string
  status?: OccurrenceStatus | ''
  type?: OccurrenceType | ''
  unresolvedOnly?: boolean
}

export type OccurrenceListItem = {
  assignedUser?: OccurrenceUserSummary
  concurrencyToken?: string
  contract?: OccurrenceEntitySummary
  createdAt: string
  description: string
  dueDate?: string
  id: string
  isArchived: boolean
  isUnresolved: boolean
  priority: OccurrenceLabel<OccurrencePriority>
  property?: OccurrenceEntitySummary
  resident?: OccurrenceEntitySummary
  status: OccurrenceLabel<OccurrenceStatus>
  title: string
  type: OccurrenceLabel<OccurrenceType>
  updatedAt?: string
}

export type OccurrenceDocument = {
  createdAt: string
  documentId: string
  label?: string
  route: string
}

export type OccurrenceComment = {
  authorDisplayName?: string
  authorUserId?: string
  body: string
  createdAt: string
  id: string
  isInternal: boolean
}

export type OccurrenceStatusHistory = {
  actorDisplayName?: string
  actorUserId?: string
  createdAt: string
  id: string
  newStatus: OccurrenceLabel<OccurrenceStatus>
  notes?: string
  previousStatus?: OccurrenceLabel<OccurrenceStatus>
}

export type OccurrencePriorityHistory = {
  actorDisplayName?: string
  actorUserId?: string
  createdAt: string
  id: string
  newPriority: OccurrenceLabel<OccurrencePriority>
  notes?: string
  previousPriority?: OccurrenceLabel<OccurrencePriority>
}

export type OccurrenceAssignmentHistory = {
  actorDisplayName?: string
  actorUserId?: string
  createdAt: string
  id: string
  newAssignedUser?: OccurrenceUserSummary
  notes?: string
  previousAssignedUser?: OccurrenceUserSummary
}

export type OccurrenceDetail = OccurrenceListItem & {
  archivedAt?: string
  attachments: OccurrenceDocument[]
  assignmentHistory: OccurrenceAssignmentHistory[]
  auditRoute: string
  cancelledAt?: string
  cancelledByUserId?: string
  cancellationNotes?: string
  comments: OccurrenceComment[]
  priorityHistory: OccurrencePriorityHistory[]
  resolutionNotes?: string
  resolvedAt?: string
  resolvedByUserId?: string
  statusHistory: OccurrenceStatusHistory[]
  timelineRoute: string
}

export type OccurrenceFormRequest = {
  assignedUserId?: string
  concurrencyToken?: string
  contractId?: string
  description: string
  dueDate?: string
  priority: OccurrencePriority
  propertyId?: string
  residentId?: string
  title: string
  type: OccurrenceType
}

type ApiOccurrenceLabel = {
  code: string
  label: string
  tone?: string
}

type ApiOccurrenceListItem = Omit<OccurrenceListItem, 'priority' | 'status' | 'type'> & {
  priority: ApiOccurrenceLabel
  status: ApiOccurrenceLabel
  type: ApiOccurrenceLabel
}

type ApiOccurrenceDetail = Omit<
  OccurrenceDetail,
  'priority' | 'priorityHistory' | 'status' | 'statusHistory' | 'type'
> & {
  priority: ApiOccurrenceLabel
  priorityHistory: Array<
    Omit<OccurrencePriorityHistory, 'newPriority' | 'previousPriority'> & {
      newPriority: ApiOccurrenceLabel
      previousPriority?: ApiOccurrenceLabel
    }
  >
  status: ApiOccurrenceLabel
  statusHistory: Array<
    Omit<OccurrenceStatusHistory, 'newStatus' | 'previousStatus'> & {
      newStatus: ApiOccurrenceLabel
      previousStatus?: ApiOccurrenceLabel
    }
  >
  type: ApiOccurrenceLabel
}

type ApiOccurrenceCreateRequest = {
  assignedUserId?: string
  contractId?: string
  description: string
  dueDate?: string
  priority: string
  propertyId?: string
  residentId?: string
  title: string
  type: string
}

type ApiOccurrenceUpdateRequest = Omit<
  ApiOccurrenceCreateRequest,
  'assignedUserId' | 'priority'
> & {
  concurrencyToken?: string
}

function isOccurrenceType(value: string): value is OccurrenceType {
  return occurrenceTypes.includes(value as OccurrenceType)
}

function isOccurrencePriority(value: string): value is OccurrencePriority {
  return occurrencePriorities.includes(value as OccurrencePriority)
}

function isOccurrenceStatus(value: string): value is OccurrenceStatus {
  return occurrenceStatuses.includes(value as OccurrenceStatus)
}

function mapType(label: ApiOccurrenceLabel): OccurrenceLabel<OccurrenceType> {
  return isOccurrenceType(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'other', label: label.label, tone: label.tone }
}

function mapPriority(label: ApiOccurrenceLabel): OccurrenceLabel<OccurrencePriority> {
  return isOccurrencePriority(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'medium', label: label.label, tone: label.tone }
}

function mapStatus(label: ApiOccurrenceLabel): OccurrenceLabel<OccurrenceStatus> {
  return isOccurrenceStatus(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'open', label: label.label, tone: label.tone }
}

function mapListItem(item: ApiOccurrenceListItem): OccurrenceListItem {
  const archivedAt = (item as { archivedAt?: string }).archivedAt
  const isArchived = item.isArchived ?? (item.status.code === 'archived' || Boolean(archivedAt))

  return {
    ...item,
    isArchived,
    priority: mapPriority(item.priority),
    status: mapStatus(item.status),
    type: mapType(item.type),
  }
}

function mapDetail(item: ApiOccurrenceDetail): OccurrenceDetail {
  return {
    ...mapListItem(item),
    archivedAt: item.archivedAt,
    attachments: item.attachments ?? [],
    assignmentHistory: item.assignmentHistory ?? [],
    auditRoute: item.auditRoute,
    cancelledAt: item.cancelledAt,
    cancelledByUserId: item.cancelledByUserId,
    cancellationNotes: item.cancellationNotes,
    comments: item.comments ?? [],
    priorityHistory: (item.priorityHistory ?? []).map((history) => ({
      ...history,
      newPriority: mapPriority(history.newPriority),
      previousPriority: history.previousPriority
        ? mapPriority(history.previousPriority)
        : undefined,
    })),
    resolutionNotes: item.resolutionNotes,
    resolvedAt: item.resolvedAt,
    resolvedByUserId: item.resolvedByUserId,
    statusHistory: (item.statusHistory ?? []).map((history) => ({
      ...history,
      newStatus: mapStatus(history.newStatus),
      previousStatus: history.previousStatus ? mapStatus(history.previousStatus) : undefined,
    })),
    timelineRoute: item.timelineRoute,
  }
}

function cleanText(value: string | undefined) {
  const trimmedValue = value?.trim()

  return trimmedValue && trimmedValue.length > 0 ? trimmedValue : undefined
}

function toCreateRequest(request: OccurrenceFormRequest): ApiOccurrenceCreateRequest {
  return {
    assignedUserId: cleanText(request.assignedUserId),
    contractId: cleanText(request.contractId),
    description: request.description,
    dueDate: cleanText(request.dueDate),
    priority: request.priority,
    propertyId: cleanText(request.propertyId),
    residentId: cleanText(request.residentId),
    title: request.title,
    type: request.type,
  }
}

function toUpdateRequest(request: OccurrenceFormRequest): ApiOccurrenceUpdateRequest {
  return {
    concurrencyToken: request.concurrencyToken,
    contractId: cleanText(request.contractId),
    description: request.description,
    dueDate: cleanText(request.dueDate),
    propertyId: cleanText(request.propertyId),
    residentId: cleanText(request.residentId),
    title: request.title,
    type: request.type,
  }
}

function mapSelectOption<TValue extends string>(
  option: ApiSelectOption<TValue> & { code?: TValue; isDisabled?: boolean; tone?: string },
): ApiSelectOption<TValue> {
  return {
    disabled: option.disabled ?? option.isDisabled,
    label: option.label,
    value: option.value ?? option.code ?? ('' as TValue),
  }
}

export async function listOccurrences(client: ApiClient, filters: OccurrenceListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiOccurrenceListItem>>('/v1/occurrences', {
    query: {
      assignedUserId: filters.assignedUserId,
      contractId: filters.contractId,
      dateFrom: filters.dateFrom,
      dateTo: filters.dateTo,
      includeArchived: filters.includeArchived,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      priority: filters.priority,
      propertyId: filters.propertyId,
      residentId: filters.residentId,
      search: filters.search,
      sort: filters.sort,
      status: filters.status,
      type: filters.type,
      unresolvedOnly: filters.unresolvedOnly,
    },
  })

  return {
    ...page,
    items: page.items.map(mapListItem),
  }
}

export async function getOccurrence(client: ApiClient, occurrenceId: string, locale?: AppLocale) {
  const occurrence = await client.get<ApiOccurrenceDetail>(`/v1/occurrences/${occurrenceId}`, {
    query: { locale },
  })

  return mapDetail(occurrence)
}

export async function createOccurrence(
  client: ApiClient,
  request: OccurrenceFormRequest,
  locale?: AppLocale,
) {
  const occurrence = await client.post<ApiOccurrenceDetail, ApiOccurrenceCreateRequest>(
    '/v1/occurrences',
    toCreateRequest(request),
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function updateOccurrence(
  client: ApiClient,
  occurrenceId: string,
  request: OccurrenceFormRequest,
  locale?: AppLocale,
) {
  const occurrence = await client.put<ApiOccurrenceDetail, ApiOccurrenceUpdateRequest>(
    `/v1/occurrences/${occurrenceId}`,
    toUpdateRequest(request),
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function assignOccurrence(
  client: ApiClient,
  occurrenceId: string,
  assignedUserId?: string,
  notes?: string,
  locale?: AppLocale,
) {
  const occurrence = await client.post<
    ApiOccurrenceDetail,
    { assignedUserId?: string; notes?: string }
  >(
    `/v1/occurrences/${occurrenceId}/assign`,
    { assignedUserId: cleanText(assignedUserId), notes: cleanText(notes) },
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function changeOccurrencePriority(
  client: ApiClient,
  occurrenceId: string,
  priority: OccurrencePriority,
  notes?: string,
  locale?: AppLocale,
) {
  const occurrence = await client.post<ApiOccurrenceDetail, { priority: string; notes?: string }>(
    `/v1/occurrences/${occurrenceId}/priority`,
    { notes: cleanText(notes), priority },
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function changeOccurrenceStatus(
  client: ApiClient,
  occurrenceId: string,
  status: Exclude<OccurrenceStatus, 'archived' | 'cancelled' | 'resolved'>,
  notes?: string,
  locale?: AppLocale,
) {
  const occurrence = await client.post<ApiOccurrenceDetail, { notes?: string; status: string }>(
    `/v1/occurrences/${occurrenceId}/status`,
    { notes: cleanText(notes), status },
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function resolveOccurrence(
  client: ApiClient,
  occurrenceId: string,
  resolutionNotes: string,
  locale?: AppLocale,
) {
  const occurrence = await client.post<ApiOccurrenceDetail, { resolutionNotes: string }>(
    `/v1/occurrences/${occurrenceId}/resolve`,
    { resolutionNotes },
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function cancelOccurrence(
  client: ApiClient,
  occurrenceId: string,
  notes?: string,
  locale?: AppLocale,
) {
  const occurrence = await client.post<ApiOccurrenceDetail, { notes?: string }>(
    `/v1/occurrences/${occurrenceId}/cancel`,
    { notes: cleanText(notes) },
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export function archiveOccurrence(client: ApiClient, occurrenceId: string) {
  return client.delete<void>(`/v1/occurrences/${occurrenceId}`)
}

export async function restoreOccurrence(
  client: ApiClient,
  occurrenceId: string,
  locale?: AppLocale,
) {
  const occurrence = await client.post<ApiOccurrenceDetail, Record<string, never>>(
    `/v1/occurrences/${occurrenceId}/restore`,
    {},
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function addOccurrenceComment(
  client: ApiClient,
  occurrenceId: string,
  body: string,
  isInternal: boolean,
  locale?: AppLocale,
) {
  const occurrence = await client.post<ApiOccurrenceDetail, { body: string; isInternal: boolean }>(
    `/v1/occurrences/${occurrenceId}/comments`,
    { body, isInternal },
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function attachOccurrenceDocument(
  client: ApiClient,
  occurrenceId: string,
  documentId: string,
  label?: string,
  locale?: AppLocale,
) {
  const occurrence = await client.post<ApiOccurrenceDetail, { documentId: string; label?: string }>(
    `/v1/occurrences/${occurrenceId}/attachments`,
    { documentId, label: cleanText(label) },
    { query: { locale } },
  )

  return mapDetail(occurrence)
}

export async function listOccurrenceTypeOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiSelectOption<OccurrenceType>>>(
    '/v1/occurrences/type-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}

export async function listOccurrencePriorityOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiSelectOption<OccurrencePriority>>>(
    '/v1/occurrences/priority-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}

export async function listOccurrenceStatusOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiSelectOption<OccurrenceStatus>>>(
    '/v1/occurrences/status-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}
