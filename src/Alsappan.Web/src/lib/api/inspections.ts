import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult, ApiSelectOption } from './contracts'

export const inspectionTypes = [
  'move-in',
  'move-out',
  'periodic',
  'maintenance',
  'inventory',
  'other',
] as const

export type InspectionType = (typeof inspectionTypes)[number]

export const inspectionStatuses = [
  'scheduled',
  'in-progress',
  'completed',
  'cancelled',
  'archived',
] as const

export type InspectionStatus = (typeof inspectionStatuses)[number]

export const inspectionConditionRatings = [
  'pending',
  'good',
  'attention',
  'damaged',
  'critical',
  'not-applicable',
] as const

export type InspectionConditionRating = (typeof inspectionConditionRatings)[number]

export const inspectionDocumentKinds = ['photo', 'attachment', 'report', 'signature'] as const

export type InspectionDocumentKind = (typeof inspectionDocumentKinds)[number]

export type InspectionLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

export type InspectionEntitySummary = {
  description?: string
  id: string
  name: string
  route?: string
}

export type InspectionProgress = {
  completedItems: number
  percentage: number
  totalItems: number
}

export type InspectionListFilters = {
  assignedUserId?: string
  contractId?: string
  includeArchived?: boolean
  locale?: AppLocale
  page?: number
  pageSize?: number
  pendingOnly?: boolean
  propertyId?: string
  residentId?: string
  scheduledFrom?: string
  scheduledTo?: string
  search?: string
  sort?: string
  status?: InspectionStatus | ''
  type?: InspectionType | ''
}

export type InspectionListItem = {
  assignee: InspectionEntitySummary
  completedAt?: string
  concurrencyToken?: string
  contract?: InspectionEntitySummary
  createdAt: string
  id: string
  isArchived: boolean
  isPending: boolean
  progress: InspectionProgress
  property: InspectionEntitySummary
  resident?: InspectionEntitySummary
  scheduledAt: string
  startedAt?: string
  status: InspectionLabel<InspectionStatus>
  title: string
  type: InspectionLabel<InspectionType>
  updatedAt?: string
}

export type InspectionChecklistItem = {
  areaName: string
  conditionRating: InspectionLabel<InspectionConditionRating>
  createdAt: string
  id: string
  isComplete: boolean
  isRequired: boolean
  itemName: string
  observations?: string
  sortOrder: number
  updatedAt?: string
}

export type InspectionDocument = {
  checklistItemId?: string
  documentId: string
  kind: InspectionLabel<InspectionDocumentKind>
  label?: string
  route: string
}

export type InspectionSignatureSlot = {
  id: string
  isRequired: boolean
  isSigned: boolean
  notes?: string
  signatureDocumentId?: string
  signedAt?: string
  signerName?: string
  signerRole: string
}

export type InspectionDetail = InspectionListItem & {
  archivedAt?: string
  auditRoute: string
  cancellationReason?: string
  cancelledAt?: string
  checklistItems: InspectionChecklistItem[]
  completionNotes?: string
  linkedDocuments: InspectionDocument[]
  notes?: string
  photoDocuments: InspectionDocument[]
  signatureSlots: InspectionSignatureSlot[]
  timelineRoute: string
}

export type InspectionSignatureSlotRequest = {
  isRequired?: boolean
  signerName?: string
  signerRole: string
}

export type InspectionFormRequest = {
  assignedUserId: string
  concurrencyToken?: string
  contractId?: string
  notes?: string
  propertyId: string
  residentId?: string
  scheduledAt: string
  signatureSlots?: InspectionSignatureSlotRequest[]
  title?: string
  type: InspectionType
}

export type InspectionChecklistItemRequest = {
  areaName: string
  conditionRating: InspectionConditionRating
  isRequired: boolean
  itemName: string
  observations?: string
  sortOrder?: number
}

export type InspectionDocumentLinkRequest = {
  checklistItemId?: string
  documentId: string
  kind: InspectionDocumentKind
  label?: string
}

type ApiInspectionLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

type ApiInspectionListItem = Omit<InspectionListItem, 'status' | 'type'> & {
  status: ApiInspectionLabel<string>
  type: ApiInspectionLabel<string>
}

type ApiInspectionChecklistItem = Omit<InspectionChecklistItem, 'conditionRating'> & {
  conditionRating: ApiInspectionLabel<string>
}

type ApiInspectionDocument = Omit<InspectionDocument, 'kind'> & {
  kind: ApiInspectionLabel<string>
}

type ApiInspectionDetail = Omit<
  InspectionDetail,
  'checklistItems' | 'linkedDocuments' | 'photoDocuments' | 'status' | 'type'
> & {
  checklistItems?: ApiInspectionChecklistItem[]
  linkedDocuments?: ApiInspectionDocument[]
  photoDocuments?: ApiInspectionDocument[]
  status: ApiInspectionLabel<string>
  type: ApiInspectionLabel<string>
}

type ApiInspectionFormRequest = {
  assignedUserId?: string
  concurrencyToken?: string
  contractId?: string
  notes?: string
  propertyId?: string
  residentId?: string
  scheduledAt: string
  signatureSlots?: InspectionSignatureSlotRequest[]
  title?: string
  type: string
}

type ApiInspectionChecklistItemRequest = {
  areaName: string
  conditionRating: string
  isRequired: boolean
  itemName: string
  observations?: string
  sortOrder?: number
}

type ApiInspectionDocumentLinkRequest = {
  checklistItemId?: string
  documentId?: string
  kind: string
  label?: string
}

type ApiInspectionSelectOption<TValue extends string = string> = ApiSelectOption<TValue> & {
  code?: TValue
  description?: string
  isDisabled?: boolean
  tone?: string
}

function isInspectionType(value: string): value is InspectionType {
  return inspectionTypes.includes(value as InspectionType)
}

function isInspectionStatus(value: string): value is InspectionStatus {
  return inspectionStatuses.includes(value as InspectionStatus)
}

function isInspectionConditionRating(value: string): value is InspectionConditionRating {
  return inspectionConditionRatings.includes(value as InspectionConditionRating)
}

function isInspectionDocumentKind(value: string): value is InspectionDocumentKind {
  return inspectionDocumentKinds.includes(value as InspectionDocumentKind)
}

function mapType(label: ApiInspectionLabel<string>): InspectionLabel<InspectionType> {
  return isInspectionType(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'other', label: label.label, tone: label.tone }
}

function mapStatus(label: ApiInspectionLabel<string>): InspectionLabel<InspectionStatus> {
  return isInspectionStatus(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'scheduled', label: label.label, tone: label.tone }
}

function mapRating(label: ApiInspectionLabel<string>): InspectionLabel<InspectionConditionRating> {
  return isInspectionConditionRating(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'pending', label: label.label, tone: label.tone }
}

function mapDocumentKind(
  label: ApiInspectionLabel<string>,
): InspectionLabel<InspectionDocumentKind> {
  return isInspectionDocumentKind(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'attachment', label: label.label, tone: label.tone }
}

function mapListItem(item: ApiInspectionListItem): InspectionListItem {
  const archivedAt = (item as { archivedAt?: string }).archivedAt
  const isArchived = item.isArchived ?? (item.status.code === 'archived' || Boolean(archivedAt))

  return {
    ...item,
    isArchived,
    status: mapStatus(item.status),
    type: mapType(item.type),
  }
}

function mapChecklistItem(item: ApiInspectionChecklistItem): InspectionChecklistItem {
  return {
    ...item,
    conditionRating: mapRating(item.conditionRating),
  }
}

function mapDocument(item: ApiInspectionDocument): InspectionDocument {
  return {
    ...item,
    kind: mapDocumentKind(item.kind),
  }
}

function mapDetail(item: ApiInspectionDetail): InspectionDetail {
  return {
    ...mapListItem(item),
    archivedAt: item.archivedAt,
    auditRoute: item.auditRoute,
    cancellationReason: item.cancellationReason,
    cancelledAt: item.cancelledAt,
    checklistItems: (item.checklistItems ?? []).map(mapChecklistItem),
    completionNotes: item.completionNotes,
    linkedDocuments: (item.linkedDocuments ?? []).map(mapDocument),
    notes: item.notes,
    photoDocuments: (item.photoDocuments ?? []).map(mapDocument),
    signatureSlots: item.signatureSlots ?? [],
    timelineRoute: item.timelineRoute,
  }
}

function cleanText(value: string | undefined) {
  const trimmedValue = value?.trim()

  return trimmedValue && trimmedValue.length > 0 ? trimmedValue : undefined
}

function toFormRequest(request: InspectionFormRequest): ApiInspectionFormRequest {
  return {
    assignedUserId: cleanText(request.assignedUserId),
    concurrencyToken: request.concurrencyToken,
    contractId: cleanText(request.contractId),
    notes: cleanText(request.notes),
    propertyId: cleanText(request.propertyId),
    residentId: cleanText(request.residentId),
    scheduledAt: request.scheduledAt,
    signatureSlots: request.signatureSlots,
    title: cleanText(request.title),
    type: request.type,
  }
}

function toChecklistItemRequest(
  request: InspectionChecklistItemRequest,
): ApiInspectionChecklistItemRequest {
  return {
    areaName: request.areaName,
    conditionRating: request.conditionRating,
    isRequired: request.isRequired,
    itemName: request.itemName,
    observations: cleanText(request.observations),
    sortOrder: request.sortOrder,
  }
}

function toDocumentLinkRequest(
  request: InspectionDocumentLinkRequest,
): ApiInspectionDocumentLinkRequest {
  return {
    checklistItemId: cleanText(request.checklistItemId),
    documentId: cleanText(request.documentId),
    kind: request.kind,
    label: cleanText(request.label),
  }
}

function mapSelectOption<TValue extends string = string>(
  option: ApiInspectionSelectOption<TValue>,
): ApiSelectOption<TValue> {
  return {
    disabled: option.disabled ?? option.isDisabled,
    label: option.label,
    value: option.value ?? option.code ?? ('' as TValue),
  }
}

export async function listInspections(client: ApiClient, filters: InspectionListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiInspectionListItem>>('/v1/inspections', {
    query: {
      assignedUserId: filters.assignedUserId,
      contractId: filters.contractId,
      includeArchived: filters.includeArchived,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      pendingOnly: filters.pendingOnly,
      propertyId: filters.propertyId,
      residentId: filters.residentId,
      scheduledFrom: filters.scheduledFrom,
      scheduledTo: filters.scheduledTo,
      search: filters.search,
      sort: filters.sort,
      status: filters.status,
      type: filters.type,
    },
  })

  return {
    ...page,
    items: page.items.map(mapListItem),
  }
}

export async function getInspection(client: ApiClient, inspectionId: string, locale?: AppLocale) {
  const inspection = await client.get<ApiInspectionDetail>(`/v1/inspections/${inspectionId}`, {
    query: { locale },
  })

  return mapDetail(inspection)
}

export async function scheduleInspection(
  client: ApiClient,
  request: InspectionFormRequest,
  locale?: AppLocale,
) {
  const inspection = await client.post<ApiInspectionDetail, ApiInspectionFormRequest>(
    '/v1/inspections',
    toFormRequest(request),
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function updateInspection(
  client: ApiClient,
  inspectionId: string,
  request: InspectionFormRequest,
  locale?: AppLocale,
) {
  const inspection = await client.put<ApiInspectionDetail, ApiInspectionFormRequest>(
    `/v1/inspections/${inspectionId}`,
    toFormRequest(request),
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function startInspection(client: ApiClient, inspectionId: string, locale?: AppLocale) {
  const inspection = await client.post<ApiInspectionDetail, Record<string, never>>(
    `/v1/inspections/${inspectionId}/start`,
    {},
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function completeInspection(
  client: ApiClient,
  inspectionId: string,
  notes?: string,
  locale?: AppLocale,
) {
  const inspection = await client.post<ApiInspectionDetail, { notes?: string }>(
    `/v1/inspections/${inspectionId}/complete`,
    { notes: cleanText(notes) },
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function cancelInspection(
  client: ApiClient,
  inspectionId: string,
  notes?: string,
  locale?: AppLocale,
) {
  const inspection = await client.post<ApiInspectionDetail, { notes?: string }>(
    `/v1/inspections/${inspectionId}/cancel`,
    { notes: cleanText(notes) },
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export function archiveInspection(client: ApiClient, inspectionId: string) {
  return client.delete<void>(`/v1/inspections/${inspectionId}`)
}

export async function restoreInspection(
  client: ApiClient,
  inspectionId: string,
  locale?: AppLocale,
) {
  const inspection = await client.post<ApiInspectionDetail, Record<string, never>>(
    `/v1/inspections/${inspectionId}/restore`,
    {},
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function addInspectionChecklistItem(
  client: ApiClient,
  inspectionId: string,
  request: InspectionChecklistItemRequest,
  locale?: AppLocale,
) {
  const inspection = await client.post<ApiInspectionDetail, ApiInspectionChecklistItemRequest>(
    `/v1/inspections/${inspectionId}/checklist-items`,
    toChecklistItemRequest(request),
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function updateInspectionChecklistItem(
  client: ApiClient,
  inspectionId: string,
  checklistItemId: string,
  request: InspectionChecklistItemRequest,
  locale?: AppLocale,
) {
  const inspection = await client.put<ApiInspectionDetail, ApiInspectionChecklistItemRequest>(
    `/v1/inspections/${inspectionId}/checklist-items/${checklistItemId}`,
    toChecklistItemRequest(request),
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function deleteInspectionChecklistItem(
  client: ApiClient,
  inspectionId: string,
  checklistItemId: string,
  locale?: AppLocale,
) {
  const inspection = await client.delete<ApiInspectionDetail>(
    `/v1/inspections/${inspectionId}/checklist-items/${checklistItemId}`,
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function linkInspectionDocument(
  client: ApiClient,
  inspectionId: string,
  request: InspectionDocumentLinkRequest,
  locale?: AppLocale,
) {
  const inspection = await client.post<ApiInspectionDetail, ApiInspectionDocumentLinkRequest>(
    `/v1/inspections/${inspectionId}/document-links`,
    toDocumentLinkRequest(request),
    { query: { locale } },
  )

  return mapDetail(inspection)
}

export async function listInspectionTypeOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiInspectionSelectOption<InspectionType>>>(
    '/v1/inspections/type-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}

export async function listInspectionStatusOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiInspectionSelectOption<InspectionStatus>>>(
    '/v1/inspections/status-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}

export async function listInspectionConditionRatingOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiInspectionSelectOption<InspectionConditionRating>>>(
    '/v1/inspections/condition-rating-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}

export async function listInspectionDocumentKindOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiInspectionSelectOption<InspectionDocumentKind>>>(
    '/v1/inspections/document-kind-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}
