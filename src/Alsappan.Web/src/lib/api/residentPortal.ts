import type { AppLocale } from '../../i18n'
import { ApiClientError, type ApiClient, type ApiClientOptions, type ApiQueryValue } from './client'
import { isApiProblemDetails, type ApiStatusLabel } from './contracts'
import type { PaymentInstruction, PaymentMoney, PaymentProviderCode } from './payments'

export type ResidentPortalStatusLabel<TCode extends string = string> = ApiStatusLabel<TCode> & {
  tone?: string
}

export type ResidentPortalSettingsSummary = {
  allowDocumentUpload: boolean
  allowOccurrenceCreation: boolean
  allowProfileUpdateRequests: boolean
  isEnabled: boolean
}

export type ResidentPortalProfile = {
  email?: string
  fullName: string
  phone?: string
  portalStatus: ResidentPortalStatusLabel
  preferredName?: string
  residentId: string
  secondaryPhone?: string
  status: ResidentPortalStatusLabel
}

export type ResidentPortalEntitySummary = {
  description?: string
  id: string
  name: string
  route: string
}

export type ResidentPortalProperty = {
  city: string
  complement?: string
  description?: string
  garageSpaceCount: number
  id: string
  name: string
  neighborhood: string
  number: string
  postalCode?: string
  stateCode: string
  status: ResidentPortalStatusLabel
  streetLine: string
  suggestedRent: PaymentMoney
}

export type ResidentPortalContract = {
  dueDay: number
  endDate?: string
  id: string
  isArchived: boolean
  monthlyRent: PaymentMoney
  property: ResidentPortalEntitySummary
  startDate: string
  status: ResidentPortalStatusLabel
}

export type ResidentPortalPayment = {
  amount: PaymentMoney
  balance: PaymentMoney
  contract?: ResidentPortalEntitySummary
  description?: string
  dueDate: string
  id: string
  isOverdue: boolean
  preferredMethod: string
  preferredMethodLabel: string
  property?: ResidentPortalEntitySummary
  providerCode?: string
  providerReference?: string
  settledAmount: PaymentMoney
  status: ResidentPortalStatusLabel
  title: string
}

export type ResidentPortalDocument = {
  category: string
  categoryLabel: string
  contentType: string
  description?: string
  downloadRoute: string
  fileName: string
  id: string
  sizeBytes: number
  status: ResidentPortalStatusLabel
  title: string
  uploadedAt: string
}

export type ResidentPortalOccurrence = {
  contract?: ResidentPortalEntitySummary
  createdAt: string
  description: string
  dueDate?: string
  id: string
  isUnresolved: boolean
  priority: ResidentPortalStatusLabel
  property?: ResidentPortalEntitySummary
  status: ResidentPortalStatusLabel
  title: string
  type: ResidentPortalStatusLabel
}

export type ResidentPortalInspection = {
  completedAt?: string
  completedItems: number
  contract?: ResidentPortalEntitySummary
  id: string
  progressPercentage: number
  property: ResidentPortalEntitySummary
  scheduledAt: string
  status: ResidentPortalStatusLabel
  title: string
  totalItems: number
  type: ResidentPortalStatusLabel
}

export type ResidentPortalNotification = {
  category: ResidentPortalStatusLabel
  createdAt: string
  deepLink?: string
  id: string
  isRead: boolean
  message: string
  title: string
}

export type ResidentPortalSummary = {
  contracts: ResidentPortalContract[]
  documents: ResidentPortalDocument[]
  inspections: ResidentPortalInspection[]
  linkedProperty?: ResidentPortalProperty
  notifications: ResidentPortalNotification[]
  occurrences: ResidentPortalOccurrence[]
  payments: ResidentPortalPayment[]
  profile: ResidentPortalProfile
  settings: ResidentPortalSettingsSummary
}

export type ResidentPortalOccurrenceCreateRequest = {
  contractId?: string
  description: string
  dueDate?: string
  priority: 'high' | 'low' | 'medium' | 'urgent' | string
  propertyId?: string
  title: string
  type: 'complaint' | 'maintenance' | 'other' | 'request' | string
}

export type ResidentPortalDocumentUploadRequest = {
  category: string
  contractId?: string
  description?: string
  file: File
  propertyId?: string
  title: string
  versionNotes?: string
}

type RawApiClient = {
  baseUrl: string
  fetchImpl: typeof fetch
  getAccessToken?: ApiClientOptions['getAccessToken']
  getLocale?: ApiClientOptions['getLocale']
  getOrganizationId?: ApiClientOptions['getOrganizationId']
  onUnauthorized?: () => void
}

function normalizeBaseUrl(baseUrl: string) {
  return baseUrl.replace(/\/+$/, '')
}

function buildUrl(baseUrl: string, path: string, query?: Record<string, ApiQueryValue>) {
  const url = `${normalizeBaseUrl(baseUrl)}${path.startsWith('/') ? path : `/${path}`}`
  const searchParams = new URLSearchParams()

  for (const [key, value] of Object.entries(query ?? {})) {
    if (value === undefined || value === null || value === '') {
      continue
    }

    if (Array.isArray(value)) {
      for (const item of value) {
        searchParams.append(key, String(item))
      }
      continue
    }

    searchParams.set(key, String(value))
  }

  const queryString = searchParams.toString()
  return queryString ? `${url}?${queryString}` : url
}

async function parseProblem(response: Response) {
  const contentType = response.headers.get('content-type') ?? ''

  if (!contentType.includes('application/json')) {
    return undefined
  }

  const body: unknown = await response.json()

  return isApiProblemDetails(body) ? body : undefined
}

async function buildHeaders(client: RawApiClient) {
  const headers = new Headers()
  headers.set('Accept', 'application/json')

  const [accessToken, locale, organizationId] = await Promise.all([
    client.getAccessToken?.(),
    client.getLocale?.(),
    client.getOrganizationId?.(),
  ])

  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  if (locale) {
    headers.set('Accept-Language', locale)
  }

  if (organizationId) {
    headers.set('X-Alsappan-Organization-Id', organizationId)
  }

  return headers
}

async function postFormData<TResponse>(
  client: ApiClient,
  path: string,
  formData: FormData,
  query?: Record<string, ApiQueryValue>,
) {
  const rawClient = client as unknown as RawApiClient
  const response = await rawClient.fetchImpl(buildUrl(rawClient.baseUrl, path, query), {
    body: formData,
    credentials: 'include',
    headers: await buildHeaders(rawClient),
    method: 'POST',
  })

  if (!response.ok) {
    const problem = await parseProblem(response)

    if (response.status === 401) {
      rawClient.onUnauthorized?.()
    }

    throw new ApiClientError(response.status, response.headers, problem)
  }

  return (await response.json()) as TResponse
}

function mapInstruction(instruction: PaymentInstruction): PaymentInstruction {
  return {
    ...instruction,
    metadata: instruction.metadata ?? {},
  }
}

function buildDocumentUploadFormData(request: ResidentPortalDocumentUploadRequest) {
  const formData = new FormData()
  formData.append('file', request.file)
  formData.append('category', request.category)
  formData.append('title', request.title)
  formData.append('description', request.description ?? '')
  formData.append('contractId', request.contractId ?? '')
  formData.append('propertyId', request.propertyId ?? '')
  formData.append('versionNotes', request.versionNotes ?? '')

  return formData
}

export function getResidentPortalSummary(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalSummary>('/v1/resident-portal/summary', {
    query: { locale },
  })
}

export function getResidentPortalProfile(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalProfile>('/v1/resident-portal/profile', {
    query: { locale },
  })
}

export function getResidentPortalProperty(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalProperty | undefined>('/v1/resident-portal/property', {
    query: { locale },
  })
}

export function listResidentPortalContracts(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalContract[]>('/v1/resident-portal/contracts', {
    query: { locale },
  })
}

export function listResidentPortalPayments(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalPayment[]>('/v1/resident-portal/payments', {
    query: { locale },
  })
}

export function listResidentPortalDocuments(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalDocument[]>('/v1/resident-portal/documents', {
    query: { locale },
  })
}

export function listResidentPortalOccurrences(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalOccurrence[]>('/v1/resident-portal/occurrences', {
    query: { locale },
  })
}

export function listResidentPortalInspections(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalInspection[]>('/v1/resident-portal/inspections', {
    query: { locale },
  })
}

export function listResidentPortalNotifications(client: ApiClient, locale?: AppLocale) {
  return client.get<ResidentPortalNotification[]>('/v1/resident-portal/notifications', {
    query: { locale },
  })
}

export function createResidentPortalOccurrence(
  client: ApiClient,
  request: ResidentPortalOccurrenceCreateRequest,
  locale?: AppLocale,
) {
  return client.post<ResidentPortalOccurrence, ResidentPortalOccurrenceCreateRequest>(
    '/v1/resident-portal/occurrences',
    request,
    { query: { locale } },
  )
}

export function uploadResidentPortalDocument(
  client: ApiClient,
  request: ResidentPortalDocumentUploadRequest,
  locale?: AppLocale,
) {
  return postFormData<ResidentPortalDocument>(
    client,
    '/v1/resident-portal/documents',
    buildDocumentUploadFormData(request),
    { locale },
  )
}

export async function createResidentPaymentInstruction(
  client: ApiClient,
  paymentId: string,
  providerCode: PaymentProviderCode | string,
  locale?: AppLocale,
) {
  const instruction = await client.post<PaymentInstruction, { providerCode: string }>(
    `/v1/resident-portal/payments/${paymentId}/instructions`,
    { providerCode },
    { query: { locale } },
  )

  return mapInstruction(instruction)
}
