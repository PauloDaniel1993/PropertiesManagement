import type { ApiClient } from './client'
import type { ApiAuditMetadata, ApiPagedResult } from './contracts'

export const propertyStatuses = [
  'available',
  'reserved',
  'rented',
  'maintenance',
  'inactive',
  'archived',
] as const

export type PropertyStatus = (typeof propertyStatuses)[number]

export const propertyTypes = ['apartment', 'house', 'commercial-room', 'land', 'other'] as const

export type PropertyType = (typeof propertyTypes)[number]

export type PropertyAddress = {
  city: string
  complement?: string
  country?: string
  neighborhood: string
  number?: string
  postalCode?: string
  state: string
  street: string
}

export type PropertyListFilters = {
  hasGarage?: boolean | ''
  maxRent?: number
  minRent?: number
  page?: number
  pageSize?: number
  search?: string
  status?: PropertyStatus | ''
  type?: PropertyType | ''
}

export type PropertyListItem = {
  address: PropertyAddress
  archivedAt?: string
  code?: string
  currencyCode?: string
  garageSpaces?: number
  hasGarage: boolean
  id: string
  name: string
  notes?: string
  status: PropertyStatus
  statusLabel?: string
  suggestedRent: number
  type?: PropertyType
  typeLabel?: string
  updatedAt?: string
}

export type PropertyDetail = PropertyListItem & {
  audit?: ApiAuditMetadata
  createdAt?: string
}

export type PropertyFormRequest = {
  address: PropertyAddress
  garageSpaces?: number
  hasGarage: boolean
  name: string
  notes?: string
  status: PropertyStatus
  suggestedRent: number
  type?: PropertyType
}

export type PropertyStatusRequest = {
  status: PropertyStatus
}

type ApiPropertyAddress = {
  city: string
  complement?: string
  countryCode?: string
  neighborhood: string
  number: string
  postalCode?: string
  stateCode: string
  streetLine: string
}

type ApiPropertyMoney = {
  amount: number
  currency: string
}

type ApiPropertyStatus = {
  code: PropertyStatus
  label: string
  tone?: string
}

type ApiPropertyListItem = {
  address: ApiPropertyAddress
  createdAt?: string
  description?: string
  garageSpaceCount: number
  garageSpaceIdentifiers?: string
  garageSummary?: string
  id: string
  isArchived?: boolean
  location?: string
  name: string
  notes?: string
  status: ApiPropertyStatus
  suggestedRent: ApiPropertyMoney
  type?: PropertyType
  typeLabel?: string
  updatedAt?: string
}

type ApiPropertyDetail = ApiPropertyListItem & {
  archivedAt?: string
  concurrencyToken?: string
  relationships?: Array<{
    count: number
    label: string
    module: string
    route: string
  }>
}

type ApiPropertyFormRequest = {
  address: ApiPropertyAddress
  description?: string
  garageSpaceCount: number
  garageSpaceIdentifiers?: string
  name: string
  notes?: string
  status: PropertyStatus
  suggestedRent: ApiPropertyMoney
  type?: PropertyType
}

function mapAddress(address: ApiPropertyAddress): PropertyAddress {
  return {
    city: address.city,
    complement: address.complement,
    country: address.countryCode,
    neighborhood: address.neighborhood,
    number: address.number,
    postalCode: address.postalCode,
    state: address.stateCode,
    street: address.streetLine,
  }
}

function mapProperty(item: ApiPropertyListItem): PropertyListItem {
  return {
    address: mapAddress(item.address),
    archivedAt: item.isArchived ? item.updatedAt : undefined,
    currencyCode: item.suggestedRent.currency,
    garageSpaces: item.garageSpaceCount,
    hasGarage: item.garageSpaceCount > 0,
    id: item.id,
    name: item.name,
    notes: item.notes,
    status: item.status.code,
    statusLabel: item.status.label,
    suggestedRent: item.suggestedRent.amount,
    type: item.type,
    typeLabel: item.typeLabel,
    updatedAt: item.updatedAt,
  }
}

function mapPropertyDetail(item: ApiPropertyDetail): PropertyDetail {
  return {
    ...mapProperty(item),
    archivedAt: item.archivedAt,
    createdAt: item.createdAt,
  }
}

function toApiAddress(address: PropertyAddress): ApiPropertyAddress {
  return {
    city: address.city,
    complement: address.complement,
    countryCode: address.country ?? 'BR',
    neighborhood: address.neighborhood,
    number: address.number ?? 'S/N',
    postalCode: address.postalCode,
    stateCode: address.state,
    streetLine: address.street,
  }
}

function toApiRequest(request: PropertyFormRequest): ApiPropertyFormRequest {
  return {
    address: toApiAddress(request.address),
    garageSpaceCount: request.hasGarage ? (request.garageSpaces ?? 0) : 0,
    name: request.name,
    notes: request.notes,
    status: request.status,
    suggestedRent: {
      amount: request.suggestedRent,
      currency: 'BRL',
    },
    type: request.type,
  }
}

export async function listProperties(client: ApiClient, filters: PropertyListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiPropertyListItem>>('/v1/properties', {
    query: {
      hasGarage: filters.hasGarage,
      maxRent: filters.maxRent,
      minRent: filters.minRent,
      page: filters.page,
      pageSize: filters.pageSize,
      search: filters.search,
      status: filters.status,
      type: filters.type,
    },
  })

  return {
    ...page,
    items: page.items.map(mapProperty),
  }
}

export async function getProperty(client: ApiClient, propertyId: string) {
  const property = await client.get<ApiPropertyDetail>(`/v1/properties/${propertyId}`)

  return mapPropertyDetail(property)
}

export async function createProperty(client: ApiClient, request: PropertyFormRequest) {
  const property = await client.post<ApiPropertyDetail, ApiPropertyFormRequest>(
    '/v1/properties',
    toApiRequest(request),
  )

  return mapPropertyDetail(property)
}

export async function updateProperty(
  client: ApiClient,
  propertyId: string,
  request: PropertyFormRequest,
) {
  const property = await client.put<ApiPropertyDetail, ApiPropertyFormRequest>(
    `/v1/properties/${propertyId}`,
    toApiRequest(request),
  )

  return mapPropertyDetail(property)
}

export function archiveProperty(client: ApiClient, propertyId: string) {
  return client.delete<void>(`/v1/properties/${propertyId}`)
}

export async function restoreProperty(client: ApiClient, propertyId: string) {
  const property = await client.post<ApiPropertyDetail, Record<string, never>>(
    `/v1/properties/${propertyId}/restore`,
    {},
  )

  return mapPropertyDetail(property)
}

export async function updatePropertyStatus(
  client: ApiClient,
  propertyId: string,
  request: PropertyStatusRequest,
) {
  const property = await client.post<ApiPropertyDetail, PropertyStatusRequest>(
    `/v1/properties/${propertyId}/status`,
    request,
  )

  return mapPropertyDetail(property)
}
