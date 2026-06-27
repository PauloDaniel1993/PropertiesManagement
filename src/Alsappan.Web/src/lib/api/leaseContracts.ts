import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult, ApiSelectOption } from './contracts'

export const contractStatuses = [
  'draft',
  'active',
  'ending-soon',
  'ended',
  'terminated',
  'cancelled',
  'archived',
] as const

export type ContractStatus = (typeof contractStatuses)[number]

export const contractAdjustmentIndexes = ['igpm', 'ipca', 'fixed', 'other'] as const

export type ContractAdjustmentIndex = (typeof contractAdjustmentIndexes)[number]

export type ContractStatusLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

export type ContractMoney = {
  amount: number
  currency: string
}

export type ContractParty = {
  id: string
  isPrimary: boolean
  name: string
}

export type ContractPropertySummary = {
  id: string
  location?: string
  name: string
}

export type ContractRelationshipSummary = {
  count: number
  label: string
  module: string
  route: string
}

export type ContractDocumentLinkSummary = {
  category: string
  count: number
  documentId?: string
  label: string
  route: string
}

export type ContractListFilters = {
  endingSoonOnly?: boolean
  endsFrom?: string
  endsTo?: string
  includeArchived?: boolean
  locale?: AppLocale
  page?: number
  pageSize?: number
  propertyId?: string
  residentId?: string
  search?: string
  startsFrom?: string
  startsTo?: string
  status?: ContractStatus | ''
}

export type ContractListItem = {
  adjustmentIndex: ContractAdjustmentIndex
  adjustmentIndexLabel: string
  concurrencyToken?: string
  createdAt?: string
  dueDay: number
  endDate?: string
  id: string
  isArchived: boolean
  monthlyRent: ContractMoney
  primaryResident: ContractParty
  property: ContractPropertySummary
  residents: ContractParty[]
  startDate: string
  status: ContractStatusLabel<ContractStatus>
  updatedAt?: string
}

export type ContractDetail = ContractListItem & {
  adjustmentIntervalMonths: number
  archivedAt?: string
  concurrencyToken?: string
  depositAmount?: ContractMoney
  discountNotes?: string
  documents: ContractDocumentLinkSummary[]
  generatePaymentsAutomatically: boolean
  nextAdjustmentDate?: string
  notes?: string
  penaltyNotes?: string
  relationships: ContractRelationshipSummary[]
}

export type ContractFormRequest = {
  adjustmentIndex: ContractAdjustmentIndex
  adjustmentIntervalMonths: number
  concurrencyToken?: string
  depositAmount?: number
  discountNotes?: string
  dueDay: number
  endDate?: string
  generatePaymentsAutomatically: boolean
  lifecycleAction?: 'activate' | 'draft'
  monthlyRent: number
  nextAdjustmentDate?: string
  notes?: string
  penaltyNotes?: string
  primaryResidentId: string
  propertyId: string
  residentIds: string[]
  startDate: string
}

export type ContractLifecycleRequest = {
  effectiveDate?: string
  notes?: string
}

type ApiContractStatusLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

type ApiContractListItem = Omit<ContractListItem, 'status'> & {
  status: ApiContractStatusLabel<ContractStatus>
}

type ApiContractDetail = ApiContractListItem & {
  adjustmentIntervalMonths: number
  archivedAt?: string
  concurrencyToken?: string
  depositAmount?: ContractMoney
  discountNotes?: string
  documents?: ContractDocumentLinkSummary[]
  generatePaymentsAutomatically: boolean
  nextAdjustmentDate?: string
  notes?: string
  penaltyNotes?: string
  relationships?: ContractRelationshipSummary[]
}

type ApiContractCreateRequest = {
  adjustmentIndex: ContractAdjustmentIndex
  adjustmentIntervalMonths: number
  depositAmount?: ContractMoney
  discountNotes?: string
  dueDay: number
  endDate?: string
  generatePaymentsAutomatically: boolean
  lifecycleAction: 'activate' | 'draft'
  monthlyRent: ContractMoney
  nextAdjustmentDate?: string
  notes?: string
  penaltyNotes?: string
  primaryResidentId: string
  propertyId: string
  residentIds: string[]
  startDate: string
}

type ApiContractUpdateRequest = Omit<ApiContractCreateRequest, 'lifecycleAction' | 'propertyId'> & {
  concurrencyToken?: string
}

function isContractStatus(value: string): value is ContractStatus {
  return contractStatuses.includes(value as ContractStatus)
}

function isAdjustmentIndex(value: string): value is ContractAdjustmentIndex {
  return contractAdjustmentIndexes.includes(value as ContractAdjustmentIndex)
}

function mapStatus(
  status: ApiContractStatusLabel<ContractStatus>,
): ContractStatusLabel<ContractStatus> {
  return isContractStatus(status.code)
    ? { code: status.code, label: status.label, tone: status.tone }
    : { code: 'draft', label: status.label, tone: status.tone }
}

function mapAdjustmentIndex(value: string): ContractAdjustmentIndex {
  return isAdjustmentIndex(value) ? value : 'other'
}

function mapContract(item: ApiContractListItem): ContractListItem {
  return {
    adjustmentIndex: mapAdjustmentIndex(item.adjustmentIndex),
    adjustmentIndexLabel: item.adjustmentIndexLabel,
    concurrencyToken: item.concurrencyToken,
    createdAt: item.createdAt,
    dueDay: item.dueDay,
    endDate: item.endDate,
    id: item.id,
    isArchived: item.isArchived,
    monthlyRent: item.monthlyRent,
    primaryResident: item.primaryResident,
    property: item.property,
    residents: item.residents,
    startDate: item.startDate,
    status: mapStatus(item.status),
    updatedAt: item.updatedAt,
  }
}

function mapContractDetail(item: ApiContractDetail): ContractDetail {
  return {
    ...mapContract(item),
    adjustmentIntervalMonths: item.adjustmentIntervalMonths,
    archivedAt: item.archivedAt,
    concurrencyToken: item.concurrencyToken,
    depositAmount: item.depositAmount,
    discountNotes: item.discountNotes,
    documents: item.documents ?? [],
    generatePaymentsAutomatically: item.generatePaymentsAutomatically,
    nextAdjustmentDate: item.nextAdjustmentDate,
    notes: item.notes,
    penaltyNotes: item.penaltyNotes,
    relationships: item.relationships ?? [],
  }
}

function toMoney(amount: number): ContractMoney {
  return {
    amount,
    currency: 'BRL',
  }
}

function toCreateRequest(request: ContractFormRequest): ApiContractCreateRequest {
  return {
    adjustmentIndex: request.adjustmentIndex,
    adjustmentIntervalMonths: request.adjustmentIntervalMonths,
    depositAmount:
      typeof request.depositAmount === 'number' ? toMoney(request.depositAmount) : undefined,
    discountNotes: request.discountNotes,
    dueDay: request.dueDay,
    endDate: request.endDate,
    generatePaymentsAutomatically: request.generatePaymentsAutomatically,
    lifecycleAction: request.lifecycleAction ?? 'draft',
    monthlyRent: toMoney(request.monthlyRent),
    nextAdjustmentDate: request.nextAdjustmentDate,
    notes: request.notes,
    penaltyNotes: request.penaltyNotes,
    primaryResidentId: request.primaryResidentId,
    propertyId: request.propertyId,
    residentIds: request.residentIds,
    startDate: request.startDate,
  }
}

function toUpdateRequest(request: ContractFormRequest): ApiContractUpdateRequest {
  const {
    lifecycleAction: _lifecycleAction,
    propertyId: _propertyId,
    ...payload
  } = toCreateRequest(request)

  return {
    ...payload,
    concurrencyToken: request.concurrencyToken,
  }
}

export async function listLeaseContracts(client: ApiClient, filters: ContractListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiContractListItem>>('/v1/contracts', {
    query: {
      endingSoonOnly: filters.endingSoonOnly,
      endsFrom: filters.endsFrom,
      endsTo: filters.endsTo,
      includeArchived: filters.includeArchived,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      propertyId: filters.propertyId,
      residentId: filters.residentId,
      search: filters.search,
      startsFrom: filters.startsFrom,
      startsTo: filters.startsTo,
      status: filters.status,
    },
  })

  return {
    ...page,
    items: page.items.map(mapContract),
  }
}

export function listContractStatusOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<Array<ContractStatusLabel<ContractStatus>>>('/v1/contracts/status-options', {
    query: { locale },
  })
}

export function listContractAdjustmentIndexOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<ApiSelectOption[]>('/v1/contracts/adjustment-index-options', {
    query: { locale },
  })
}

export async function getLeaseContract(client: ApiClient, contractId: string) {
  const contract = await client.get<ApiContractDetail>(`/v1/contracts/${contractId}`)

  return mapContractDetail(contract)
}

export async function createLeaseContract(client: ApiClient, request: ContractFormRequest) {
  const contract = await client.post<ApiContractDetail, ApiContractCreateRequest>(
    '/v1/contracts',
    toCreateRequest(request),
  )

  return mapContractDetail(contract)
}

export async function updateLeaseContract(
  client: ApiClient,
  contractId: string,
  request: ContractFormRequest,
) {
  const contract = await client.put<ApiContractDetail, ApiContractUpdateRequest>(
    `/v1/contracts/${contractId}`,
    toUpdateRequest(request),
  )

  return mapContractDetail(contract)
}

export async function activateLeaseContract(
  client: ApiClient,
  contractId: string,
  request: ContractLifecycleRequest = {},
) {
  const contract = await client.post<ApiContractDetail, ContractLifecycleRequest>(
    `/v1/contracts/${contractId}/activate`,
    request,
  )

  return mapContractDetail(contract)
}

export async function terminateLeaseContract(
  client: ApiClient,
  contractId: string,
  request: ContractLifecycleRequest = {},
) {
  const contract = await client.post<ApiContractDetail, ContractLifecycleRequest>(
    `/v1/contracts/${contractId}/terminate`,
    request,
  )

  return mapContractDetail(contract)
}

export async function cancelLeaseContract(
  client: ApiClient,
  contractId: string,
  request: ContractLifecycleRequest = {},
) {
  const contract = await client.post<ApiContractDetail, ContractLifecycleRequest>(
    `/v1/contracts/${contractId}/cancel`,
    request,
  )

  return mapContractDetail(contract)
}

export function archiveLeaseContract(client: ApiClient, contractId: string) {
  return client.delete<void>(`/v1/contracts/${contractId}`)
}

export async function restoreLeaseContract(client: ApiClient, contractId: string) {
  const contract = await client.post<ApiContractDetail, Record<string, never>>(
    `/v1/contracts/${contractId}/restore`,
    {},
  )

  return mapContractDetail(contract)
}
