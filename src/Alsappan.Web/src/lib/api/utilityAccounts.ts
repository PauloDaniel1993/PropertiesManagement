import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult, ApiSelectOption } from './contracts'

export const utilityAccountTypes = [
  'electricity',
  'water',
  'gas',
  'internet',
  'condominium-fee',
  'iptu',
  'insurance',
  'other',
] as const

export type UtilityAccountType = (typeof utilityAccountTypes)[number]

export const utilityAccountStatuses = [
  'open',
  'overdue',
  'paid',
  'cancelled',
  'disputed',
  'archived',
] as const

export type UtilityAccountStatus = (typeof utilityAccountStatuses)[number]

export const utilityAccountResponsibilities = [
  'organization',
  'property',
  'contract',
  'resident',
  'owner',
  'other',
] as const

export type UtilityAccountResponsibility = (typeof utilityAccountResponsibilities)[number]

export const utilityPaymentMethods = [
  'cash',
  'bank-transfer',
  'boleto',
  'pix',
  'paypal',
  'card',
  'other',
] as const

export type UtilityPaymentMethod = (typeof utilityPaymentMethods)[number]

export type UtilityMoney = {
  amount: number
  currency: string
}

export type UtilityAccountLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

export type UtilityEntitySummary = {
  description?: string
  id: string
  name: string
  route?: string
}

export type UtilityDocumentSummary = {
  documentId: string
  label?: string
  route: string
}

export type UtilityAccountListFilters = {
  billingFrom?: string
  billingTo?: string
  contractId?: string
  dueFrom?: string
  dueTo?: string
  includeArchived?: boolean
  locale?: AppLocale
  page?: number
  pageSize?: number
  propertyId?: string
  responsibility?: UtilityAccountResponsibility | ''
  search?: string
  sort?: string
  status?: UtilityAccountStatus | ''
  type?: UtilityAccountType | ''
}

export type UtilityAccountListItem = {
  amount: UtilityMoney
  archivedAt?: string
  auditRoute?: string
  balance: UtilityMoney
  bankReference?: string
  billDocuments: UtilityDocumentSummary[]
  billingPeriodEnd?: string
  billingPeriodStart: string
  concurrencyToken?: string
  contract?: UtilityEntitySummary
  createdAt: string
  description?: string
  dueDate: string
  id: string
  isArchived: boolean
  isOverdue: boolean
  paidAmount: UtilityMoney
  paidOn?: string
  paymentMethod?: UtilityPaymentMethod | string
  property?: UtilityEntitySummary
  receiptDocuments: UtilityDocumentSummary[]
  resident?: UtilityEntitySummary
  responsibility: UtilityAccountLabel<UtilityAccountResponsibility>
  status: UtilityAccountLabel<UtilityAccountStatus>
  timelineRoute?: string
  title: string
  type: UtilityAccountLabel<UtilityAccountType>
  updatedAt?: string
}

export type UtilityAccountDetail = UtilityAccountListItem & {
  auditRoute: string
  timelineRoute: string
}

export type UtilityAccountFormRequest = {
  amount: number
  billDocumentId?: string
  billingPeriodEnd: string
  billingPeriodStart: string
  concurrencyToken?: string
  contractId?: string
  description?: string
  dueDate: string
  propertyId?: string
  residentId?: string
  responsibility: UtilityAccountResponsibility
  title: string
  type: UtilityAccountType
}

export type UtilityAccountMarkPaidRequest = {
  bankReference?: string
  notes?: string
  amount: number
  paidOn: string
  paymentMethod?: UtilityPaymentMethod
  receiptDocumentId?: string
}

type ApiUtilityAccountSelectOption<TValue extends string = string> = ApiSelectOption<TValue> & {
  code?: TValue
  description?: string
  isDisabled?: boolean
  tone?: string
}

type ApiUtilityAccountLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

type ApiUtilityAccountListItem = Omit<
  UtilityAccountListItem,
  'paymentMethod' | 'responsibility' | 'status' | 'type'
> & {
  paymentMethod?: string
  responsibility: ApiUtilityAccountLabel<string>
  status: ApiUtilityAccountLabel<string>
  type: ApiUtilityAccountLabel<string>
}

type ApiUtilityAccountDetail = Omit<
  UtilityAccountDetail,
  'paymentMethod' | 'responsibility' | 'status' | 'type'
> & {
  paymentMethod?: string
  responsibility: ApiUtilityAccountLabel<string>
  status: ApiUtilityAccountLabel<string>
  type: ApiUtilityAccountLabel<string>
}

type ApiUtilityAccountFormRequest = {
  amount: UtilityMoney
  billDocumentId?: string
  billingPeriodEnd: string
  billingPeriodStart: string
  contractId?: string
  description?: string
  dueDate: string
  propertyId?: string
  residentId?: string
  responsibility: string
  title: string
  type: string
}

type ApiUtilityAccountUpdateRequest = ApiUtilityAccountFormRequest & {
  concurrencyToken?: string
}

type ApiUtilityAccountMarkPaidRequest = {
  amount: UtilityMoney
  bankReference?: string
  notes?: string
  paidOn: string
  paymentMethod?: string
  receiptDocumentId?: string
}

function isUtilityAccountType(value: string): value is UtilityAccountType {
  return utilityAccountTypes.includes(value as UtilityAccountType)
}

function isUtilityAccountStatus(value: string): value is UtilityAccountStatus {
  return utilityAccountStatuses.includes(value as UtilityAccountStatus)
}

function isUtilityAccountResponsibility(value: string): value is UtilityAccountResponsibility {
  return utilityAccountResponsibilities.includes(value as UtilityAccountResponsibility)
}

function isUtilityPaymentMethod(value: string): value is UtilityPaymentMethod {
  return utilityPaymentMethods.includes(value as UtilityPaymentMethod)
}

function mapType(label: ApiUtilityAccountLabel<string>): UtilityAccountLabel<UtilityAccountType> {
  return isUtilityAccountType(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'other', label: label.label, tone: label.tone }
}

function mapStatus(
  label: ApiUtilityAccountLabel<string>,
): UtilityAccountLabel<UtilityAccountStatus> {
  return isUtilityAccountStatus(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'open', label: label.label, tone: label.tone }
}

function mapResponsibility(
  label: ApiUtilityAccountLabel<string>,
): UtilityAccountLabel<UtilityAccountResponsibility> {
  return isUtilityAccountResponsibility(label.code)
    ? { code: label.code, label: label.label, tone: label.tone }
    : { code: 'other', label: label.label, tone: label.tone }
}

function mapPaymentMethod(value: string | undefined) {
  if (!value) {
    return undefined
  }

  return isUtilityPaymentMethod(value) ? value : value
}

function mapListItem(item: ApiUtilityAccountListItem): UtilityAccountListItem {
  return {
    ...item,
    billDocuments: item.billDocuments ?? [],
    isArchived: item.isArchived ?? (item.status.code === 'archived' || Boolean(item.archivedAt)),
    isOverdue: item.isOverdue ?? item.status.code === 'overdue',
    paymentMethod: mapPaymentMethod(item.paymentMethod),
    receiptDocuments: item.receiptDocuments ?? [],
    responsibility: mapResponsibility(item.responsibility),
    status: mapStatus(item.status),
    type: mapType(item.type),
  }
}

function mapDetail(item: ApiUtilityAccountDetail): UtilityAccountDetail {
  return {
    ...mapListItem(item),
    auditRoute: item.auditRoute,
    timelineRoute: item.timelineRoute,
  }
}

function toMoney(amount: number): UtilityMoney {
  return {
    amount,
    currency: 'BRL',
  }
}

function cleanId(id: string | undefined) {
  const value = id?.trim()

  return value && value.length > 0 ? value : undefined
}

function toFormRequest(request: UtilityAccountFormRequest): ApiUtilityAccountFormRequest {
  return {
    amount: toMoney(request.amount),
    billDocumentId: cleanId(request.billDocumentId),
    billingPeriodEnd: request.billingPeriodEnd,
    billingPeriodStart: request.billingPeriodStart,
    contractId: request.contractId,
    description: request.description,
    dueDate: request.dueDate,
    propertyId: request.propertyId,
    residentId: request.residentId,
    responsibility: request.responsibility,
    title: request.title,
    type: request.type,
  }
}

function toUpdateRequest(request: UtilityAccountFormRequest): ApiUtilityAccountUpdateRequest {
  return {
    ...toFormRequest(request),
    concurrencyToken: request.concurrencyToken,
  }
}

function toMarkPaidRequest(
  request: UtilityAccountMarkPaidRequest,
): ApiUtilityAccountMarkPaidRequest {
  return {
    amount: toMoney(request.amount),
    bankReference: request.bankReference,
    notes: request.notes,
    paidOn: request.paidOn,
    paymentMethod: request.paymentMethod,
    receiptDocumentId: request.receiptDocumentId,
  }
}

function mapSelectOption<TValue extends string = string>(
  option: ApiUtilityAccountSelectOption<TValue>,
): ApiSelectOption<TValue> {
  return {
    disabled: option.disabled ?? option.isDisabled,
    label: option.label,
    value: option.value ?? option.code,
  }
}

export async function listUtilityAccounts(
  client: ApiClient,
  filters: UtilityAccountListFilters = {},
) {
  const page = await client.get<ApiPagedResult<ApiUtilityAccountListItem>>('/v1/utility-accounts', {
    query: {
      billingFrom: filters.billingFrom,
      billingTo: filters.billingTo,
      contractId: filters.contractId,
      dueFrom: filters.dueFrom,
      dueTo: filters.dueTo,
      includeArchived: filters.includeArchived,
      locale: filters.locale,
      page: filters.page,
      pageSize: filters.pageSize,
      propertyId: filters.propertyId,
      responsibility: filters.responsibility,
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

export async function getUtilityAccount(
  client: ApiClient,
  utilityAccountId: string,
  locale?: AppLocale,
) {
  const utilityAccount = await client.get<ApiUtilityAccountDetail>(
    `/v1/utility-accounts/${utilityAccountId}`,
    {
      query: { locale },
    },
  )

  return mapDetail(utilityAccount)
}

export async function createUtilityAccount(
  client: ApiClient,
  request: UtilityAccountFormRequest,
  locale?: AppLocale,
) {
  const utilityAccount = await client.post<ApiUtilityAccountDetail, ApiUtilityAccountFormRequest>(
    '/v1/utility-accounts',
    toFormRequest(request),
    { query: { locale } },
  )

  return mapDetail(utilityAccount)
}

export async function updateUtilityAccount(
  client: ApiClient,
  utilityAccountId: string,
  request: UtilityAccountFormRequest,
  locale?: AppLocale,
) {
  const utilityAccount = await client.put<ApiUtilityAccountDetail, ApiUtilityAccountUpdateRequest>(
    `/v1/utility-accounts/${utilityAccountId}`,
    toUpdateRequest(request),
    { query: { locale } },
  )

  return mapDetail(utilityAccount)
}

export async function markUtilityAccountPaid(
  client: ApiClient,
  utilityAccountId: string,
  request: UtilityAccountMarkPaidRequest,
  locale?: AppLocale,
) {
  const utilityAccount = await client.post<
    ApiUtilityAccountDetail,
    ApiUtilityAccountMarkPaidRequest
  >(`/v1/utility-accounts/${utilityAccountId}/mark-paid`, toMarkPaidRequest(request), {
    query: { locale },
  })

  return mapDetail(utilityAccount)
}

export async function cancelUtilityAccount(
  client: ApiClient,
  utilityAccountId: string,
  notes?: string,
  locale?: AppLocale,
) {
  const utilityAccount = await client.post<ApiUtilityAccountDetail, { notes?: string }>(
    `/v1/utility-accounts/${utilityAccountId}/cancel`,
    { notes },
    { query: { locale } },
  )

  return mapDetail(utilityAccount)
}

export function archiveUtilityAccount(client: ApiClient, utilityAccountId: string) {
  return client.delete<void>(`/v1/utility-accounts/${utilityAccountId}`)
}

export async function restoreUtilityAccount(
  client: ApiClient,
  utilityAccountId: string,
  locale?: AppLocale,
) {
  const utilityAccount = await client.post<ApiUtilityAccountDetail, Record<string, never>>(
    `/v1/utility-accounts/${utilityAccountId}/restore`,
    {},
    { query: { locale } },
  )

  return mapDetail(utilityAccount)
}

export async function listUtilityAccountStatusOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiUtilityAccountSelectOption<UtilityAccountStatus>>>(
    '/v1/utility-accounts/status-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}

export async function listUtilityAccountTypeOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiUtilityAccountSelectOption<UtilityAccountType>>>(
    '/v1/utility-accounts/type-options',
    { query: { locale } },
  )

  return options.map(mapSelectOption)
}

export async function listUtilityAccountResponsibilityOptions(
  client: ApiClient,
  locale?: AppLocale,
) {
  const options = await client.get<
    Array<ApiUtilityAccountSelectOption<UtilityAccountResponsibility>>
  >('/v1/utility-accounts/responsibility-options', { query: { locale } })

  return options.map(mapSelectOption)
}
