import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult, ApiSelectOption } from './contracts'

export const vehicleTypes = ['car', 'motorcycle', 'truck', 'van', 'bicycle', 'other'] as const

export type VehicleType = (typeof vehicleTypes)[number]

export const vehicleAuthorizationStatuses = [
  'pending',
  'authorized',
  'denied',
  'inactive',
  'archived',
] as const

export type VehicleAuthorizationStatus = (typeof vehicleAuthorizationStatuses)[number]

export type VehicleLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

export type VehicleEntitySummary = {
  description?: string
  id: string
  name: string
  route?: string
}

export type VehicleListFilters = {
  authorizationStatus?: VehicleAuthorizationStatus | ''
  contractId?: string
  hasParkingAllocation?: boolean
  includeArchived?: boolean
  locale?: AppLocale
  page?: number
  pageSize?: number
  parkingSpaceIdentifier?: string
  plate?: string
  propertyId?: string
  residentId?: string
  search?: string
  sort?: string
  type?: VehicleType | ''
}

export type VehicleListItem = {
  authorizationStatus: VehicleLabel<VehicleAuthorizationStatus>
  brand?: string
  color?: string
  concurrencyToken?: string
  contract?: VehicleEntitySummary
  createdAt: string
  hasParkingAllocation: boolean
  id: string
  isArchived: boolean
  model?: string
  normalizedParkingSpaceIdentifier?: string
  normalizedPlate: string
  parkingAllocationNotes?: string
  parkingSpaceIdentifier?: string
  plate: string
  property?: VehicleEntitySummary
  resident: VehicleEntitySummary
  type: VehicleLabel<VehicleType>
  updatedAt?: string
  year?: number
}

export type VehicleDetail = VehicleListItem & {
  archivedAt?: string
  auditRoute: string
  notes?: string
  timelineRoute: string
}

export type VehicleFormRequest = {
  authorizationStatus: VehicleAuthorizationStatus
  brand?: string
  color?: string
  concurrencyToken?: string
  contractId?: string
  model?: string
  notes?: string
  parkingAllocationNotes?: string
  parkingSpaceIdentifier?: string
  plate: string
  propertyId?: string
  residentId?: string
  type: VehicleType
  year?: number
}

type ApiVehicleSelectOption<TValue extends string = string> = ApiSelectOption<TValue> & {
  code?: TValue
  description?: string
  isDisabled?: boolean
  tone?: string
}

type ApiVehicleLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

type ApiVehicleListItem = Omit<VehicleListItem, 'authorizationStatus' | 'type'> & {
  authorizationStatus: ApiVehicleLabel<string>
  type: ApiVehicleLabel<string>
}

type ApiVehicleDetail = Omit<VehicleDetail, 'authorizationStatus' | 'type'> & {
  authorizationStatus: ApiVehicleLabel<string>
  type: ApiVehicleLabel<string>
}

type ApiVehicleCreateRequest = {
  authorizationStatus: string
  brand?: string
  color?: string
  contractId?: string
  model?: string
  notes?: string
  parkingAllocationNotes?: string
  parkingSpaceIdentifier?: string
  plate: string
  propertyId?: string
  residentId?: string
  type: string
  year?: number
}

type ApiVehicleUpdateRequest = ApiVehicleCreateRequest & {
  concurrencyToken?: string
}

function isVehicleType(value: string): value is VehicleType {
  return vehicleTypes.includes(value as VehicleType)
}

function isVehicleAuthorizationStatus(value: string): value is VehicleAuthorizationStatus {
  return vehicleAuthorizationStatuses.includes(value as VehicleAuthorizationStatus)
}

function mapType(label: ApiVehicleLabel<string>): VehicleLabel<VehicleType> {
  return isVehicleType(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'other', label: label.label, tone: label.tone }
}

function mapAuthorizationStatus(
  label: ApiVehicleLabel<string>,
): VehicleLabel<VehicleAuthorizationStatus> {
  return isVehicleAuthorizationStatus(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'pending', label: label.label, tone: label.tone }
}

function mapListItem(item: ApiVehicleListItem): VehicleListItem {
  const archivedAt = (item as { archivedAt?: string }).archivedAt
  const isArchived =
    item.isArchived ?? (item.authorizationStatus.code === 'archived' || Boolean(archivedAt))

  return {
    ...item,
    authorizationStatus: mapAuthorizationStatus(item.authorizationStatus),
    hasParkingAllocation: item.hasParkingAllocation ?? Boolean(item.parkingSpaceIdentifier),
    isArchived,
    type: mapType(item.type),
  }
}

function mapDetail(item: ApiVehicleDetail): VehicleDetail {
  return {
    ...mapListItem(item),
    archivedAt: item.archivedAt,
    auditRoute: item.auditRoute,
    notes: item.notes,
    timelineRoute: item.timelineRoute,
  }
}

function cleanText(value: string | undefined) {
  const trimmedValue = value?.trim()

  return trimmedValue && trimmedValue.length > 0 ? trimmedValue : undefined
}

function cleanNumber(value: number | undefined) {
  return typeof value === 'number' && Number.isFinite(value) ? value : undefined
}

function toCreateRequest(request: VehicleFormRequest): ApiVehicleCreateRequest {
  return {
    authorizationStatus: request.authorizationStatus,
    brand: cleanText(request.brand),
    color: cleanText(request.color),
    contractId: cleanText(request.contractId),
    model: cleanText(request.model),
    notes: cleanText(request.notes),
    parkingAllocationNotes: cleanText(request.parkingAllocationNotes),
    parkingSpaceIdentifier: cleanText(request.parkingSpaceIdentifier),
    plate: request.plate,
    propertyId: cleanText(request.propertyId),
    residentId: cleanText(request.residentId),
    type: request.type,
    year: cleanNumber(request.year),
  }
}

function toUpdateRequest(request: VehicleFormRequest): ApiVehicleUpdateRequest {
  return {
    ...toCreateRequest(request),
    concurrencyToken: request.concurrencyToken,
  }
}

function mapSelectOption<TValue extends string = string>(
  option: ApiVehicleSelectOption<TValue>,
): ApiSelectOption<TValue> {
  return {
    disabled: option.disabled ?? option.isDisabled,
    label: option.label,
    value: option.value ?? option.code ?? ('' as TValue),
  }
}

export async function listVehicles(client: ApiClient, filters: VehicleListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiVehicleListItem>>('/v1/vehicles', {
    query: {
      authorizationStatus: filters.authorizationStatus,
      contractId: filters.contractId,
      hasParkingAllocation: filters.hasParkingAllocation,
      includeArchived: filters.includeArchived,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      parkingSpaceIdentifier: filters.parkingSpaceIdentifier,
      plate: filters.plate,
      propertyId: filters.propertyId,
      residentId: filters.residentId,
      search: filters.search,
      sort: filters.sort,
      type: filters.type,
    },
  })

  return {
    ...page,
    items: page.items.map(mapListItem),
  }
}

export async function getVehicle(client: ApiClient, vehicleId: string, locale?: AppLocale) {
  const vehicle = await client.get<ApiVehicleDetail>(`/v1/vehicles/${vehicleId}`, {
    query: { locale },
  })

  return mapDetail(vehicle)
}

export async function createVehicle(
  client: ApiClient,
  request: VehicleFormRequest,
  locale?: AppLocale,
) {
  const vehicle = await client.post<ApiVehicleDetail, ApiVehicleCreateRequest>(
    '/v1/vehicles',
    toCreateRequest(request),
    { query: { locale } },
  )

  return mapDetail(vehicle)
}

export async function updateVehicle(
  client: ApiClient,
  vehicleId: string,
  request: VehicleFormRequest,
  locale?: AppLocale,
) {
  const vehicle = await client.put<ApiVehicleDetail, ApiVehicleUpdateRequest>(
    `/v1/vehicles/${vehicleId}`,
    toUpdateRequest(request),
    { query: { locale } },
  )

  return mapDetail(vehicle)
}

export async function authorizeVehicle(client: ApiClient, vehicleId: string, locale?: AppLocale) {
  const vehicle = await client.post<ApiVehicleDetail, Record<string, never>>(
    `/v1/vehicles/${vehicleId}/authorize`,
    {},
    { query: { locale } },
  )

  return mapDetail(vehicle)
}

export async function denyVehicle(
  client: ApiClient,
  vehicleId: string,
  notes?: string,
  locale?: AppLocale,
) {
  const vehicle = await client.post<ApiVehicleDetail, { notes?: string }>(
    `/v1/vehicles/${vehicleId}/deny`,
    { notes: cleanText(notes) },
    { query: { locale } },
  )

  return mapDetail(vehicle)
}

export function archiveVehicle(client: ApiClient, vehicleId: string) {
  return client.delete<void>(`/v1/vehicles/${vehicleId}`)
}

export async function restoreVehicle(client: ApiClient, vehicleId: string, locale?: AppLocale) {
  const vehicle = await client.post<ApiVehicleDetail, Record<string, never>>(
    `/v1/vehicles/${vehicleId}/restore`,
    {},
    { query: { locale } },
  )

  return mapDetail(vehicle)
}

export async function listVehicleTypeOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiVehicleSelectOption<VehicleType>>>(
    '/v1/vehicles/type-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}

export async function listVehicleAuthorizationStatusOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiVehicleSelectOption<VehicleAuthorizationStatus>>>(
    '/v1/vehicles/authorization-status-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}
