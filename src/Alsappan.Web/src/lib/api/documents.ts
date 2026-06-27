import type { AppLocale } from '../../i18n'
import { ApiClientError, type ApiClient, type ApiClientOptions, type ApiQueryValue } from './client'
import {
  isApiProblemDetails,
  type ApiPagedResult,
  type ApiSelectOption,
  type ApiStatusLabel,
} from './contracts'

export const documentStatuses = ['active', 'archived'] as const

export type DocumentStatus = (typeof documentStatuses)[number]

export const documentCategories = [
  'general',
  'property',
  'contract',
  'resident',
  'payment-receipt',
  'utility-account',
  'pet',
  'vehicle',
  'occurrence',
  'inspection',
  'other',
] as const

export type DocumentCategory = (typeof documentCategories)[number]

export const documentLinkedEntityTypes = [
  'property',
  'contract',
  'resident',
  'payment',
  'utility-account',
  'pet',
  'vehicle',
  'occurrence',
  'inspection',
] as const

export type DocumentLinkedEntityType = (typeof documentLinkedEntityTypes)[number]

export type DocumentStatusLabel<TCode extends string = string> = ApiStatusLabel<TCode> & {
  tone?: string
}

export type DocumentLinkRequest = {
  entityId: string
  entityType: DocumentLinkedEntityType
  label?: string
}

export type DocumentLink = DocumentLinkRequest & {
  route: string
}

export type DocumentVersion = {
  contentType: string
  fileName: string
  id: string
  notes?: string
  sizeBytes: number
  uploadedAt: string
  uploadedByUserId?: string
  versionNumber: number
}

export type DocumentListFilters = {
  category?: DocumentCategory | ''
  includeArchived?: boolean
  linkedEntityId?: string
  linkedEntityType?: DocumentLinkedEntityType | ''
  locale?: AppLocale
  page?: number
  pageSize?: number
  search?: string
  uploadedByUserId?: string
  uploadedFrom?: string
  uploadedTo?: string
}

export type DocumentListItem = {
  category: DocumentCategory
  categoryLabel: string
  concurrencyToken?: string
  contentType: string
  currentVersionNumber: number
  description?: string
  fileName: string
  id: string
  isArchived: boolean
  links: DocumentLink[]
  sizeBytes: number
  status: DocumentStatusLabel<DocumentStatus>
  title: string
  updatedAt?: string
  uploadedAt: string
}

export type DocumentDetail = DocumentListItem & {
  archivedAt?: string
  auditRoute: string
  createdAt: string
  downloadRoute: string
  timelineRoute: string
  versions: DocumentVersion[]
}

export type DocumentUploadRequest = {
  category: DocumentCategory
  description?: string
  file: File
  links: DocumentLinkRequest[]
  title: string
  versionNotes?: string
}

export type DocumentUpdateRequest = {
  category: DocumentCategory
  concurrencyToken?: string
  description?: string
  links: DocumentLinkRequest[]
  title: string
}

export type DocumentVersionUploadRequest = {
  file: File
  notes?: string
}

export type DocumentDownload = {
  blob: Blob
  contentType: string
  fileName: string
  sizeBytes: number
}

type ApiDocumentStatusLabel<TCode extends string = string> = ApiStatusLabel<TCode> & {
  tone?: string
}

type ApiDocumentLink = {
  entityId: string
  entityType: string
  label?: string
  route: string
}

type ApiDocumentVersion = {
  contentType: string
  fileName: string
  id: string
  notes?: string
  sizeBytes: number
  uploadedAt: string
  uploadedByUserId?: string
  versionNumber: number
}

type ApiDocumentListItem = {
  category: string
  categoryLabel: string
  concurrencyToken?: string
  contentType: string
  currentVersionNumber: number
  description?: string
  fileName: string
  id: string
  isArchived: boolean
  links?: ApiDocumentLink[]
  sizeBytes: number
  status: ApiDocumentStatusLabel<DocumentStatus>
  title: string
  updatedAt?: string
  uploadedAt: string
}

type ApiDocumentDetail = ApiDocumentListItem & {
  archivedAt?: string
  auditRoute: string
  createdAt: string
  downloadRoute: string
  timelineRoute: string
  versions?: ApiDocumentVersion[]
}

type RawApiClient = {
  baseUrl: string
  fetchImpl: typeof fetch
  getAccessToken?: ApiClientOptions['getAccessToken']
  getLocale?: ApiClientOptions['getLocale']
  getOrganizationId?: ApiClientOptions['getOrganizationId']
  onUnauthorized?: () => void
}

type RawRequestOptions = {
  accept?: string
  body?: BodyInit
  method: 'GET' | 'POST'
  query?: Record<string, ApiQueryValue>
  signal?: AbortSignal
}

function isDocumentCategory(value: string): value is DocumentCategory {
  return documentCategories.includes(value as DocumentCategory)
}

function isDocumentStatus(value: string): value is DocumentStatus {
  return documentStatuses.includes(value as DocumentStatus)
}

function isDocumentLinkedEntityType(value: string): value is DocumentLinkedEntityType {
  return documentLinkedEntityTypes.includes(value as DocumentLinkedEntityType)
}

function mapCategory(value: string): DocumentCategory {
  return isDocumentCategory(value) ? value : 'general'
}

function mapStatus(
  status: ApiDocumentStatusLabel<DocumentStatus>,
): DocumentStatusLabel<DocumentStatus> {
  return isDocumentStatus(status.code)
    ? { code: status.code, color: status.color, label: status.label, tone: status.tone }
    : { code: 'active', color: status.color, label: status.label, tone: status.tone }
}

function mapLink(link: ApiDocumentLink): DocumentLink {
  return {
    entityId: link.entityId,
    entityType: isDocumentLinkedEntityType(link.entityType) ? link.entityType : 'property',
    label: link.label,
    route: link.route,
  }
}

function mapListItem(item: ApiDocumentListItem): DocumentListItem {
  return {
    category: mapCategory(item.category),
    categoryLabel: item.categoryLabel,
    concurrencyToken: item.concurrencyToken,
    contentType: item.contentType,
    currentVersionNumber: item.currentVersionNumber,
    description: item.description,
    fileName: item.fileName,
    id: item.id,
    isArchived: item.isArchived,
    links: (item.links ?? []).map(mapLink),
    sizeBytes: item.sizeBytes,
    status: mapStatus(item.status),
    title: item.title,
    updatedAt: item.updatedAt,
    uploadedAt: item.uploadedAt,
  }
}

function mapDetail(item: ApiDocumentDetail): DocumentDetail {
  return {
    ...mapListItem(item),
    archivedAt: item.archivedAt,
    auditRoute: item.auditRoute,
    createdAt: item.createdAt,
    downloadRoute: item.downloadRoute,
    timelineRoute: item.timelineRoute,
    versions: (item.versions ?? []).map((version) => ({
      contentType: version.contentType,
      fileName: version.fileName,
      id: version.id,
      notes: version.notes,
      sizeBytes: version.sizeBytes,
      uploadedAt: version.uploadedAt,
      uploadedByUserId: version.uploadedByUserId,
      versionNumber: version.versionNumber,
    })),
  }
}

function toLinkRequest(link: DocumentLinkRequest) {
  return {
    entityId: link.entityId,
    entityType: link.entityType,
    label: link.label,
  }
}

function normalizeBaseUrl(baseUrl: string) {
  return baseUrl.replace(/\/+$/, '')
}

function buildUrl(baseUrl: string, path: string, query?: Record<string, ApiQueryValue>) {
  const normalizedBaseUrl = normalizeBaseUrl(baseUrl)
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const url = `${normalizedBaseUrl}${normalizedPath}`
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

async function buildHeaders(client: RawApiClient, accept: string) {
  const headers = new Headers()
  headers.set('Accept', accept)

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

async function rawRequest(client: ApiClient, path: string, options: RawRequestOptions) {
  const rawClient = client as unknown as RawApiClient
  const response = await rawClient.fetchImpl(buildUrl(rawClient.baseUrl, path, options.query), {
    body: options.body,
    credentials: 'include',
    headers: await buildHeaders(rawClient, options.accept ?? 'application/json'),
    method: options.method,
    signal: options.signal,
  })

  if (!response.ok) {
    const problem = await parseProblem(response)

    if (response.status === 401) {
      rawClient.onUnauthorized?.()
    }

    throw new ApiClientError(response.status, response.headers, problem)
  }

  return response
}

async function rawJsonRequest<TResponse>(
  client: ApiClient,
  path: string,
  options: RawRequestOptions,
) {
  const response = await rawRequest(client, path, options)

  if (response.status === 204) {
    return undefined as TResponse
  }

  return (await response.json()) as TResponse
}

function buildUploadFormData(request: DocumentUploadRequest) {
  const formData = new FormData()
  formData.append('file', request.file)
  formData.append('category', request.category)
  formData.append('title', request.title)
  formData.append('description', request.description ?? '')
  formData.append('versionNotes', request.versionNotes ?? '')
  formData.append('linksJson', JSON.stringify(request.links.map(toLinkRequest)))

  return formData
}

function buildVersionFormData(request: DocumentVersionUploadRequest) {
  const formData = new FormData()
  formData.append('file', request.file)
  formData.append('notes', request.notes ?? '')

  return formData
}

function getFileNameFromDisposition(contentDisposition: string | null) {
  if (!contentDisposition) {
    return undefined
  }

  const encodedMatch = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition)
  if (encodedMatch?.[1]) {
    return decodeURIComponent(encodedMatch[1].trim())
  }

  const quotedMatch = /filename="([^"]+)"/i.exec(contentDisposition)
  if (quotedMatch?.[1]) {
    return quotedMatch[1].trim()
  }

  const plainMatch = /filename=([^;]+)/i.exec(contentDisposition)

  return plainMatch?.[1]?.trim()
}

export async function listDocuments(client: ApiClient, filters: DocumentListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiDocumentListItem>>('/v1/documents', {
    query: {
      category: filters.category,
      includeArchived: filters.includeArchived,
      linkedEntityId: filters.linkedEntityId,
      linkedEntityType: filters.linkedEntityType,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      search: filters.search,
      uploadedByUserId: filters.uploadedByUserId,
      uploadedFrom: filters.uploadedFrom,
      uploadedTo: filters.uploadedTo,
    },
  })

  return {
    ...page,
    items: page.items.map(mapListItem),
  }
}

export function listDocumentCategoryOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<Array<ApiSelectOption<DocumentCategory>>>('/v1/documents/category-options', {
    query: { locale },
  })
}

export function listDocumentStatusOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<Array<DocumentStatusLabel<DocumentStatus>>>('/v1/documents/status-options', {
    query: { locale },
  })
}

export function listAllowedDocumentFileTypes(client: ApiClient, locale?: AppLocale) {
  return client.get<ApiSelectOption[]>('/v1/documents/allowed-file-types', {
    query: { locale },
  })
}

export async function getDocument(client: ApiClient, documentId: string, locale?: AppLocale) {
  const document = await client.get<ApiDocumentDetail>(`/v1/documents/${documentId}`, {
    query: { locale },
  })

  return mapDetail(document)
}

export async function uploadDocument(
  client: ApiClient,
  request: DocumentUploadRequest,
  locale?: AppLocale,
) {
  const document = await rawJsonRequest<ApiDocumentDetail>(client, '/v1/documents', {
    body: buildUploadFormData(request),
    method: 'POST',
    query: { locale },
  })

  return mapDetail(document)
}

export async function updateDocument(
  client: ApiClient,
  documentId: string,
  request: DocumentUpdateRequest,
  locale?: AppLocale,
) {
  const document = await client.put<ApiDocumentDetail, DocumentUpdateRequest>(
    `/v1/documents/${documentId}`,
    {
      category: request.category,
      concurrencyToken: request.concurrencyToken,
      description: request.description,
      links: request.links.map(toLinkRequest),
      title: request.title,
    },
    { query: { locale } },
  )

  return mapDetail(document)
}

export async function uploadDocumentVersion(
  client: ApiClient,
  documentId: string,
  request: DocumentVersionUploadRequest,
  locale?: AppLocale,
) {
  const document = await rawJsonRequest<ApiDocumentDetail>(
    client,
    `/v1/documents/${documentId}/versions`,
    {
      body: buildVersionFormData(request),
      method: 'POST',
      query: { locale },
    },
  )

  return mapDetail(document)
}

export async function downloadDocument(client: ApiClient, documentId: string) {
  const response = await rawRequest(client, `/v1/documents/${documentId}/download`, {
    accept: '*/*',
    method: 'GET',
  })
  const blob = await response.blob()

  return {
    blob,
    contentType: response.headers.get('content-type') ?? blob.type,
    fileName: getFileNameFromDisposition(response.headers.get('content-disposition')) ?? 'document',
    sizeBytes: blob.size,
  } satisfies DocumentDownload
}

export async function downloadDocumentVersion(
  client: ApiClient,
  documentId: string,
  versionNumber: number,
) {
  const response = await rawRequest(
    client,
    `/v1/documents/${documentId}/versions/${versionNumber}/download`,
    {
      accept: '*/*',
      method: 'GET',
    },
  )
  const blob = await response.blob()

  return {
    blob,
    contentType: response.headers.get('content-type') ?? blob.type,
    fileName: getFileNameFromDisposition(response.headers.get('content-disposition')) ?? 'document',
    sizeBytes: blob.size,
  } satisfies DocumentDownload
}

export function archiveDocument(client: ApiClient, documentId: string) {
  return client.delete<void>(`/v1/documents/${documentId}`)
}

export async function restoreDocument(client: ApiClient, documentId: string, locale?: AppLocale) {
  const document = await client.post<ApiDocumentDetail, Record<string, never>>(
    `/v1/documents/${documentId}/restore`,
    {},
    { query: { locale } },
  )

  return mapDetail(document)
}
