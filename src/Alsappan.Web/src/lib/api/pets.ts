import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult, ApiSelectOption } from './contracts'

export const petSpecies = ['dog', 'cat', 'bird', 'rabbit', 'fish', 'reptile', 'other'] as const

export type PetSpecies = (typeof petSpecies)[number]

export const petAuthorizationStatuses = [
  'pending',
  'authorized',
  'denied',
  'inactive',
  'archived',
] as const

export type PetAuthorizationStatus = (typeof petAuthorizationStatuses)[number]

export const petDocumentKinds = ['vaccination-record', 'authorization-form'] as const

export type PetDocumentKind = (typeof petDocumentKinds)[number]

export type PetLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

export type PetEntitySummary = {
  description?: string
  id: string
  name: string
  route?: string
}

export type PetDocumentSummary = {
  documentId: string
  kind: PetDocumentKind
  kindLabel: string
  label?: string
  route: string
}

export type PetAuthorizationHistoryItem = {
  notes?: string
  occurredAt: string
  status: PetAuthorizationStatus
  statusLabel: string
}

export type PetListFilters = {
  activeContractOnly?: boolean
  authorizationStatus?: PetAuthorizationStatus | ''
  contractId?: string
  includeArchived?: boolean
  locale?: AppLocale
  page?: number
  pageSize?: number
  propertyId?: string
  residentId?: string
  search?: string
  sort?: string
  species?: PetSpecies | ''
}

export type PetListItem = {
  authorizationFormDocuments: PetDocumentSummary[]
  authorizationNotes?: string
  authorizationStatus: PetLabel<PetAuthorizationStatus>
  breed?: string
  concurrencyToken?: string
  contract?: PetEntitySummary
  createdAt: string
  id: string
  isArchived: boolean
  name: string
  property?: PetEntitySummary
  resident: PetEntitySummary
  species: PetLabel<PetSpecies>
  updatedAt?: string
  vaccinationRecordDocuments: PetDocumentSummary[]
}

export type PetDetail = PetListItem & {
  archivedAt?: string
  auditRoute: string
  authorizationHistory: PetAuthorizationHistoryItem[]
  notes?: string
  timelineRoute: string
}

export type PetFormRequest = {
  authorizationFormDocumentId?: string
  authorizationNotes?: string
  authorizationStatus: PetAuthorizationStatus
  breed?: string
  concurrencyToken?: string
  contractId?: string
  name: string
  notes?: string
  propertyId?: string
  residentId: string
  species: PetSpecies
  vaccinationRecordDocumentId?: string
}

export type PetOptions = {
  authorizationStatuses: Array<ApiSelectOption<PetAuthorizationStatus>>
  documentKinds: Array<ApiSelectOption<PetDocumentKind>>
  species: Array<ApiSelectOption<PetSpecies>>
}

type ApiPetLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

type ApiPetDocumentSummary = Omit<PetDocumentSummary, 'kind'> & {
  kind: string
}

type ApiPetAuthorizationHistoryItem = Omit<PetAuthorizationHistoryItem, 'status'> & {
  status: string
}

type ApiPetListItem = Omit<
  PetListItem,
  'authorizationFormDocuments' | 'authorizationStatus' | 'species' | 'vaccinationRecordDocuments'
> & {
  authorizationFormDocuments?: ApiPetDocumentSummary[]
  authorizationStatus: ApiPetLabel<string>
  species: ApiPetLabel<string>
  vaccinationRecordDocuments?: ApiPetDocumentSummary[]
}

type ApiPetDetail = Omit<
  PetDetail,
  | 'authorizationFormDocuments'
  | 'authorizationHistory'
  | 'authorizationStatus'
  | 'species'
  | 'vaccinationRecordDocuments'
> & {
  authorizationFormDocuments?: ApiPetDocumentSummary[]
  authorizationHistory?: ApiPetAuthorizationHistoryItem[]
  authorizationStatus: ApiPetLabel<string>
  species: ApiPetLabel<string>
  vaccinationRecordDocuments?: ApiPetDocumentSummary[]
}

type ApiPetFormRequest = {
  authorizationFormDocumentId?: string
  authorizationNotes?: string
  authorizationStatus: string
  breed?: string
  contractId?: string
  name: string
  notes?: string
  propertyId?: string
  residentId: string
  species: string
  vaccinationRecordDocumentId?: string
}

type ApiPetUpdateRequest = ApiPetFormRequest & {
  concurrencyToken?: string
}

type ApiPetOptions = {
  authorizationStatuses: Array<ApiSelectOption<string>>
  documentKinds: Array<ApiSelectOption<string>>
  species: Array<ApiSelectOption<string>>
}

function isPetSpecies(value: string): value is PetSpecies {
  return petSpecies.includes(value as PetSpecies)
}

function isPetAuthorizationStatus(value: string): value is PetAuthorizationStatus {
  return petAuthorizationStatuses.includes(value as PetAuthorizationStatus)
}

function isPetDocumentKind(value: string): value is PetDocumentKind {
  return petDocumentKinds.includes(value as PetDocumentKind)
}

function mapSpecies(label: ApiPetLabel<string>): PetLabel<PetSpecies> {
  return isPetSpecies(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'other', label: label.label, tone: label.tone }
}

function mapAuthorizationStatus(label: ApiPetLabel<string>): PetLabel<PetAuthorizationStatus> {
  return isPetAuthorizationStatus(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'pending', label: label.label, tone: label.tone }
}

function mapDocument(document: ApiPetDocumentSummary): PetDocumentSummary {
  return {
    ...document,
    kind: isPetDocumentKind(document.kind) ? document.kind : 'vaccination-record',
  }
}

function mapHistoryItem(item: ApiPetAuthorizationHistoryItem): PetAuthorizationHistoryItem {
  return {
    ...item,
    status: isPetAuthorizationStatus(item.status) ? item.status : 'pending',
  }
}

function mapListItem(item: ApiPetListItem): PetListItem {
  return {
    ...item,
    authorizationFormDocuments: (item.authorizationFormDocuments ?? []).map(mapDocument),
    authorizationStatus: mapAuthorizationStatus(item.authorizationStatus),
    isArchived: item.isArchived ?? item.authorizationStatus.code === 'archived',
    species: mapSpecies(item.species),
    vaccinationRecordDocuments: (item.vaccinationRecordDocuments ?? []).map(mapDocument),
  }
}

function mapDetail(item: ApiPetDetail): PetDetail {
  return {
    ...mapListItem(item),
    archivedAt: item.archivedAt,
    auditRoute: item.auditRoute,
    authorizationHistory: (item.authorizationHistory ?? []).map(mapHistoryItem),
    notes: item.notes,
    timelineRoute: item.timelineRoute,
  }
}

function cleanId(id: string | undefined) {
  const value = id?.trim()

  return value && value.length > 0 ? value : undefined
}

function toFormRequest(request: PetFormRequest): ApiPetFormRequest {
  return {
    authorizationFormDocumentId: cleanId(request.authorizationFormDocumentId),
    authorizationNotes: request.authorizationNotes,
    authorizationStatus: request.authorizationStatus,
    breed: request.breed,
    contractId: cleanId(request.contractId),
    name: request.name,
    notes: request.notes,
    propertyId: cleanId(request.propertyId),
    residentId: request.residentId,
    species: request.species,
    vaccinationRecordDocumentId: cleanId(request.vaccinationRecordDocumentId),
  }
}

function toUpdateRequest(request: PetFormRequest): ApiPetUpdateRequest {
  return {
    ...toFormRequest(request),
    concurrencyToken: request.concurrencyToken,
  }
}

function mapSelectOption<TValue extends string>(
  option: ApiSelectOption<string>,
  fallback: TValue,
  guard: (value: string) => value is TValue,
): ApiSelectOption<TValue> {
  const value = String(option.value ?? '')

  return {
    disabled: option.disabled,
    label: option.label,
    value: guard(value) ? value : fallback,
  }
}

export async function listPets(client: ApiClient, filters: PetListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiPetListItem>>('/v1/pets', {
    query: {
      activeContractOnly: filters.activeContractOnly,
      authorizationStatus: filters.authorizationStatus,
      contractId: filters.contractId,
      includeArchived: filters.includeArchived,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      propertyId: filters.propertyId,
      residentId: filters.residentId,
      search: filters.search,
      sort: filters.sort,
      species: filters.species,
    },
  })

  return {
    ...page,
    items: page.items.map(mapListItem),
  }
}

export async function getPet(client: ApiClient, petId: string, locale?: AppLocale) {
  const pet = await client.get<ApiPetDetail>(`/v1/pets/${petId}`, {
    query: { locale },
  })

  return mapDetail(pet)
}

export async function createPet(client: ApiClient, request: PetFormRequest, locale?: AppLocale) {
  const pet = await client.post<ApiPetDetail, ApiPetFormRequest>(
    '/v1/pets',
    toFormRequest(request),
    { query: { locale } },
  )

  return mapDetail(pet)
}

export async function updatePet(
  client: ApiClient,
  petId: string,
  request: PetFormRequest,
  locale?: AppLocale,
) {
  const pet = await client.put<ApiPetDetail, ApiPetUpdateRequest>(
    `/v1/pets/${petId}`,
    toUpdateRequest(request),
    { query: { locale } },
  )

  return mapDetail(pet)
}

export async function authorizePet(
  client: ApiClient,
  petId: string,
  authorizationNotes?: string,
  locale?: AppLocale,
) {
  const pet = await client.post<ApiPetDetail, { authorizationNotes?: string }>(
    `/v1/pets/${petId}/authorize`,
    { authorizationNotes },
    { query: { locale } },
  )

  return mapDetail(pet)
}

export async function denyPet(
  client: ApiClient,
  petId: string,
  authorizationNotes?: string,
  locale?: AppLocale,
) {
  const pet = await client.post<ApiPetDetail, { authorizationNotes?: string }>(
    `/v1/pets/${petId}/deny`,
    { authorizationNotes },
    { query: { locale } },
  )

  return mapDetail(pet)
}

export function archivePet(client: ApiClient, petId: string) {
  return client.delete<void>(`/v1/pets/${petId}`)
}

export async function restorePet(client: ApiClient, petId: string, locale?: AppLocale) {
  const pet = await client.post<ApiPetDetail, Record<string, never>>(
    `/v1/pets/${petId}/restore`,
    {},
    { query: { locale } },
  )

  return mapDetail(pet)
}

export async function getPetOptions(client: ApiClient, locale?: AppLocale): Promise<PetOptions> {
  const options = await client.get<ApiPetOptions>('/v1/pets/options', { query: { locale } })

  return {
    authorizationStatuses: options.authorizationStatuses.map((option) =>
      mapSelectOption(option, 'pending', isPetAuthorizationStatus),
    ),
    documentKinds: options.documentKinds.map((option) =>
      mapSelectOption(option, 'vaccination-record', isPetDocumentKind),
    ),
    species: options.species.map((option) => mapSelectOption(option, 'other', isPetSpecies)),
  }
}
