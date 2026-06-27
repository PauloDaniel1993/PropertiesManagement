import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult } from './contracts'

export const residentStatuses = ['active', 'inactive', 'archived'] as const

export type ResidentStatus = (typeof residentStatuses)[number]

export const residentPortalStatuses = ['not-invited', 'invited', 'active', 'disabled'] as const

export type ResidentPortalStatus = (typeof residentPortalStatuses)[number]

export const residentPrivacyFlags = [
  'contact-data',
  'identification-data',
  'emergency-contact',
  'notes',
] as const

export type ResidentPrivacyFlag = (typeof residentPrivacyFlags)[number]

export type ResidentStatusTone =
  'archived' | 'danger' | 'info' | 'neutral' | 'success' | 'warning' | string

export type ResidentStatusLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: ResidentStatusTone
}

export type ResidentEmergencyContact = {
  name?: string
  phone?: string
  relationship?: string
}

export type ResidentRelationshipSummary = {
  count: number
  label: string
  module: string
  route?: string
}

export type ResidentListFilters = {
  hasPortalAccess?: boolean | ''
  includeArchived?: boolean
  locale?: AppLocale
  page?: number
  pageSize?: number
  portalStatus?: ResidentPortalStatus | ''
  search?: string
  status?: ResidentStatus | ''
}

export type ResidentListItem = {
  archivedAt?: string
  birthDate?: string
  concurrencyToken?: string
  contactSummary?: string
  createdAt?: string
  documentIdentifier?: string
  documentType?: string
  email?: string
  emergencyContact: ResidentEmergencyContact
  fullName: string
  id: string
  isArchived: boolean
  isSensitiveMasked: boolean
  notes?: string
  phone?: string
  portalStatus: ResidentStatusLabel<ResidentPortalStatus>
  preferredName?: string
  privacyFlags: string[]
  relationships?: ResidentRelationshipSummary[]
  secondaryPhone?: string
  status: ResidentStatusLabel<ResidentStatus>
  updatedAt?: string
}

export type ResidentDetail = ResidentListItem & {
  relationships: ResidentRelationshipSummary[]
}

export type ResidentFormRequest = {
  birthDate?: string
  documentIdentifier?: string
  documentType?: string
  email?: string
  emergencyContact: ResidentEmergencyContact
  fullName: string
  notes?: string
  phone?: string
  portalStatus: ResidentPortalStatus
  preferredName?: string
  privacyFlags: string[]
  secondaryPhone?: string
  status: ResidentStatus
}

export type ResidentDuplicateWarningRequest = {
  documentIdentifier?: string
  email?: string
  ignoreResidentId?: string
  phone?: string
}

export type ResidentDuplicateWarning = {
  field: string
  message: string
  residentId: string
  residentName: string
  value: string
}

type ApiResidentStatusLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

type ApiResidentListItem = {
  archivedAt?: string
  birthDate?: string
  concurrencyToken?: string
  contactSummary?: string
  createdAt?: string
  documentIdentifier?: string
  documentType?: string
  email?: string
  emergencyContact?: ResidentEmergencyContact
  fullName: string
  id: string
  isArchived?: boolean
  isSensitiveMasked?: boolean
  notes?: string
  phone?: string
  portalStatus: ApiResidentStatusLabel<ResidentPortalStatus>
  preferredName?: string
  privacyFlags?: string[]
  relationships?: ResidentRelationshipSummary[]
  secondaryPhone?: string
  status: ApiResidentStatusLabel<ResidentStatus>
  updatedAt?: string
}

type ApiResidentDetail = ApiResidentListItem & {
  relationships?: ResidentRelationshipSummary[]
}

function isResidentStatus(value: string): value is ResidentStatus {
  return residentStatuses.includes(value as ResidentStatus)
}

function isResidentPortalStatus(value: string): value is ResidentPortalStatus {
  return residentPortalStatuses.includes(value as ResidentPortalStatus)
}

function mapStatus<TCode extends string>(
  status: ApiResidentStatusLabel<TCode>,
): ResidentStatusLabel<TCode> {
  return {
    code: status.code,
    label: status.label,
    tone: status.tone,
  }
}

function mapResidentStatus(
  status: ApiResidentStatusLabel<ResidentStatus>,
): ResidentStatusLabel<ResidentStatus> {
  return isResidentStatus(status.code)
    ? mapStatus(status)
    : { code: 'active', label: status.label, tone: status.tone }
}

function mapResidentPortalStatus(
  status: ApiResidentStatusLabel<ResidentPortalStatus>,
): ResidentStatusLabel<ResidentPortalStatus> {
  return isResidentPortalStatus(status.code)
    ? mapStatus(status)
    : { code: 'not-invited', label: status.label, tone: status.tone }
}

function mapResident(item: ApiResidentListItem): ResidentListItem {
  return {
    archivedAt: item.archivedAt,
    birthDate: item.birthDate,
    concurrencyToken: item.concurrencyToken,
    contactSummary: item.contactSummary,
    createdAt: item.createdAt,
    documentIdentifier: item.documentIdentifier,
    documentType: item.documentType,
    email: item.email,
    emergencyContact: item.emergencyContact ?? {},
    fullName: item.fullName,
    id: item.id,
    isArchived: item.isArchived ?? item.status.code === 'archived',
    isSensitiveMasked: item.isSensitiveMasked ?? false,
    notes: item.notes,
    phone: item.phone,
    portalStatus: mapResidentPortalStatus(item.portalStatus),
    preferredName: item.preferredName,
    privacyFlags: item.privacyFlags ?? [],
    relationships: item.relationships,
    secondaryPhone: item.secondaryPhone,
    status: mapResidentStatus(item.status),
    updatedAt: item.updatedAt,
  }
}

function mapResidentDetail(item: ApiResidentDetail): ResidentDetail {
  return {
    ...mapResident(item),
    relationships: item.relationships ?? [],
  }
}

export async function listResidents(client: ApiClient, filters: ResidentListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiResidentListItem>>('/v1/residents', {
    query: {
      hasPortalAccess: filters.hasPortalAccess,
      includeArchived: filters.includeArchived,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      portalStatus: filters.portalStatus,
      search: filters.search,
      status: filters.status,
    },
  })

  return {
    ...page,
    items: page.items.map(mapResident),
  }
}

export function listResidentStatusOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<Array<ResidentStatusLabel<ResidentStatus>>>('/v1/residents/status-options', {
    query: { locale },
  })
}

export function listResidentPortalStatusOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<Array<ResidentStatusLabel<ResidentPortalStatus>>>(
    '/v1/residents/portal-status-options',
    { query: { locale } },
  )
}

export function checkResidentDuplicateWarnings(
  client: ApiClient,
  request: ResidentDuplicateWarningRequest,
) {
  return client.get<ResidentDuplicateWarning[]>('/v1/residents/duplicate-warnings', {
    query: {
      documentIdentifier: request.documentIdentifier,
      email: request.email,
      ignoreResidentId: request.ignoreResidentId,
      phone: request.phone,
    },
  })
}

export async function getResident(client: ApiClient, residentId: string) {
  const resident = await client.get<ApiResidentDetail>(`/v1/residents/${residentId}`)

  return mapResidentDetail(resident)
}

export async function createResident(client: ApiClient, request: ResidentFormRequest) {
  const resident = await client.post<ApiResidentDetail, ResidentFormRequest>(
    '/v1/residents',
    request,
  )

  return mapResidentDetail(resident)
}

export async function updateResident(
  client: ApiClient,
  residentId: string,
  request: ResidentFormRequest,
) {
  const resident = await client.put<ApiResidentDetail, ResidentFormRequest>(
    `/v1/residents/${residentId}`,
    request,
  )

  return mapResidentDetail(resident)
}

export function archiveResident(client: ApiClient, residentId: string) {
  return client.delete<void>(`/v1/residents/${residentId}`)
}

export async function restoreResident(client: ApiClient, residentId: string) {
  const resident = await client.post<ApiResidentDetail, Record<string, never>>(
    `/v1/residents/${residentId}/restore`,
    {},
  )

  return mapResidentDetail(resident)
}
