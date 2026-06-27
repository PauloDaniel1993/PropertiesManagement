import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'
import type { ApiPagedResult, ApiSelectOption } from './contracts'

export const paymentStatuses = [
  'pending',
  'overdue',
  'partially-paid',
  'paid',
  'cancelled',
  'disputed',
  'archived',
] as const

export type PaymentStatus = (typeof paymentStatuses)[number]

export const paymentMethods = [
  'cash',
  'bank-transfer',
  'boleto',
  'pix',
  'paypal',
  'card',
  'other',
] as const

export type PaymentMethod = (typeof paymentMethods)[number]

export const paymentReconciliationStatuses = [
  'not-required',
  'pending',
  'matched',
  'failed',
  'manual-review',
] as const

export type PaymentReconciliationStatus = (typeof paymentReconciliationStatuses)[number]

export const paymentProviderCodes = ['mock-boleto', 'mock-pix', 'mock-paypal'] as const

export type PaymentProviderCode = (typeof paymentProviderCodes)[number]

export type PaymentInstructionKind = 'boleto' | 'paypal' | 'pix' | string

export type PaymentMoney = {
  amount: number
  currency: string
}

export type PaymentStatusLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

export type PaymentEntitySummary = {
  description?: string
  id: string
  name: string
  route?: string
}

export type PaymentTransaction = {
  amount: PaymentMoney
  bankReference?: string
  createdAt: string
  id: string
  isReversed: boolean
  method: PaymentMethod
  methodLabel: string
  notes?: string
  providerCode?: string
  providerReference?: string
  receiptDocumentId?: string
  settledOn: string
}

export type PaymentReceiptDocument = {
  documentId: string
  label?: string
  route: string
}

export type PaymentListFilters = {
  contractId?: string
  dueFrom?: string
  dueTo?: string
  includeArchived?: boolean
  locale?: AppLocale
  overdueOnly?: boolean
  page?: number
  pageSize?: number
  propertyId?: string
  residentId?: string
  search?: string
  sort?: string
  status?: PaymentStatus | ''
}

export type PaymentListItem = {
  amount: PaymentMoney
  balance: PaymentMoney
  concurrencyToken?: string
  contract?: PaymentEntitySummary
  createdAt: string
  description?: string
  discountAmount: PaymentMoney
  dueDate: string
  grossAmount: PaymentMoney
  id: string
  isArchived: boolean
  isOverdue: boolean
  penaltyAmount: PaymentMoney
  preferredMethod: PaymentMethod
  preferredMethodLabel: string
  property?: PaymentEntitySummary
  resident?: PaymentEntitySummary
  settledAmount: PaymentMoney
  status: PaymentStatusLabel<PaymentStatus>
  title: string
  updatedAt?: string
}

export type PaymentDetail = PaymentListItem & {
  archivedAt?: string
  auditRoute: string
  notes?: string
  providerCode?: string
  providerMetadataJson?: string
  providerReference?: string
  receiptDocuments: PaymentReceiptDocument[]
  reconciliationStatus: PaymentStatusLabel<PaymentReconciliationStatus>
  timelineRoute: string
  transactions: PaymentTransaction[]
  utilityAccountId?: string
}

export type PaymentFormRequest = {
  amount: number
  concurrencyToken?: string
  contractId?: string
  description?: string
  discountAmount?: number
  dueDate: string
  notes?: string
  penaltyAmount?: number
  preferredMethod: PaymentMethod
  propertyId?: string
  reconciliationStatus: PaymentReconciliationStatus
  residentId?: string
  title: string
  utilityAccountId?: string
}

export type PaymentTransactionRequest = {
  amount: number
  bankReference?: string
  method: PaymentMethod
  notes?: string
  providerCode?: string
  providerReference?: string
  receiptDocumentId?: string
  settledOn: string
}

export type PaymentInstruction = {
  amount: PaymentMoney
  approvalUrl?: string
  barcode?: string
  chargeId: string
  copyPasteCode?: string
  dueDate: string
  expiresAt?: string
  kind: PaymentInstructionKind
  linhaDigitavel?: string
  metadata: Record<string, string>
  payerSummary: string
  paymentIntentId?: string
  providerCode: string
  providerReference: string
  qrPayload?: string
  status: string
}

export type PaymentProviderEventRequest = {
  amount?: number
  eventType: string
  notes?: string
  providerCode: string
  providerReference: string
  settledOn: string
}

type ApiPaymentStatusLabel<TCode extends string = string> = {
  code: TCode
  label: string
  tone?: string
}

type ApiPaymentSelectOption<TValue extends string = string> = ApiSelectOption<TValue> & {
  description?: string
  isDisabled?: boolean
}

type ApiPaymentListItem = Omit<PaymentListItem, 'preferredMethod' | 'status'> & {
  preferredMethod: string
  status: ApiPaymentStatusLabel<string>
}

type ApiPaymentDetail = Omit<
  PaymentDetail,
  'preferredMethod' | 'receiptDocuments' | 'reconciliationStatus' | 'status' | 'transactions'
> & {
  preferredMethod: string
  receiptDocuments?: PaymentReceiptDocument[]
  reconciliationStatus: ApiPaymentStatusLabel<string>
  status: ApiPaymentStatusLabel<string>
  transactions?: ApiPaymentTransaction[]
}

type ApiPaymentTransaction = Omit<PaymentTransaction, 'method'> & {
  method: string
}

type ApiPaymentFormRequest = {
  amount: PaymentMoney
  contractId?: string
  description?: string
  discountAmount?: PaymentMoney
  dueDate: string
  notes?: string
  penaltyAmount?: PaymentMoney
  preferredMethod: string
  propertyId?: string
  reconciliationStatus: string
  residentId?: string
  title: string
  utilityAccountId?: string
}

type ApiPaymentUpdateRequest = ApiPaymentFormRequest & {
  concurrencyToken?: string
}

type ApiPaymentTransactionRequest = {
  amount: PaymentMoney
  bankReference?: string
  method: string
  notes?: string
  providerCode?: string
  providerReference?: string
  receiptDocumentId?: string
  settledOn: string
}

type ApiPaymentInstruction = Omit<PaymentInstruction, 'metadata'> & {
  metadata?: Record<string, string>
}

function isPaymentStatus(value: string): value is PaymentStatus {
  return paymentStatuses.includes(value as PaymentStatus)
}

function isPaymentMethod(value: string): value is PaymentMethod {
  return paymentMethods.includes(value as PaymentMethod)
}

function isPaymentReconciliationStatus(value: string): value is PaymentReconciliationStatus {
  return paymentReconciliationStatuses.includes(value as PaymentReconciliationStatus)
}

function mapStatus(status: ApiPaymentStatusLabel<string>): PaymentStatusLabel<PaymentStatus> {
  return isPaymentStatus(status.code)
    ? { code: status.code, label: status.label, tone: status.tone }
    : { code: 'pending', label: status.label, tone: status.tone }
}

function mapReconciliationStatus(
  status: ApiPaymentStatusLabel<string>,
): PaymentStatusLabel<PaymentReconciliationStatus> {
  return isPaymentReconciliationStatus(status.code)
    ? { code: status.code, label: status.label, tone: status.tone }
    : { code: 'not-required', label: status.label, tone: status.tone }
}

function mapMethod(value: string): PaymentMethod {
  return isPaymentMethod(value) ? value : 'other'
}

function mapTransaction(transaction: ApiPaymentTransaction): PaymentTransaction {
  return {
    ...transaction,
    method: mapMethod(transaction.method),
  }
}

function mapListItem(item: ApiPaymentListItem): PaymentListItem {
  return {
    ...item,
    preferredMethod: mapMethod(item.preferredMethod),
    status: mapStatus(item.status),
  }
}

function mapDetail(item: ApiPaymentDetail): PaymentDetail {
  return {
    ...mapListItem(item),
    archivedAt: item.archivedAt,
    auditRoute: item.auditRoute,
    notes: item.notes,
    providerCode: item.providerCode,
    providerMetadataJson: item.providerMetadataJson,
    providerReference: item.providerReference,
    receiptDocuments: item.receiptDocuments ?? [],
    reconciliationStatus: mapReconciliationStatus(item.reconciliationStatus),
    timelineRoute: item.timelineRoute,
    transactions: (item.transactions ?? []).map(mapTransaction),
    utilityAccountId: item.utilityAccountId,
  }
}

function toMoney(amount: number): PaymentMoney {
  return {
    amount,
    currency: 'BRL',
  }
}

function toOptionalMoney(amount: number | undefined) {
  return typeof amount === 'number' ? toMoney(amount) : undefined
}

function toFormRequest(request: PaymentFormRequest): ApiPaymentFormRequest {
  return {
    amount: toMoney(request.amount),
    contractId: request.contractId,
    description: request.description,
    discountAmount: toOptionalMoney(request.discountAmount),
    dueDate: request.dueDate,
    notes: request.notes,
    penaltyAmount: toOptionalMoney(request.penaltyAmount),
    preferredMethod: request.preferredMethod,
    propertyId: request.propertyId,
    reconciliationStatus: request.reconciliationStatus,
    residentId: request.residentId,
    title: request.title,
    utilityAccountId: request.utilityAccountId,
  }
}

function toUpdateRequest(request: PaymentFormRequest): ApiPaymentUpdateRequest {
  return {
    ...toFormRequest(request),
    concurrencyToken: request.concurrencyToken,
  }
}

function toTransactionRequest(request: PaymentTransactionRequest): ApiPaymentTransactionRequest {
  return {
    amount: toMoney(request.amount),
    bankReference: request.bankReference,
    method: request.method,
    notes: request.notes,
    providerCode: request.providerCode,
    providerReference: request.providerReference,
    receiptDocumentId: request.receiptDocumentId,
    settledOn: request.settledOn,
  }
}

function mapInstruction(instruction: ApiPaymentInstruction): PaymentInstruction {
  return {
    ...instruction,
    metadata: instruction.metadata ?? {},
  }
}

export function mapPaymentSelectOption<TValue extends string = string>(
  option: ApiPaymentSelectOption<TValue>,
): ApiSelectOption<TValue> {
  return {
    disabled: option.disabled ?? option.isDisabled,
    label: option.label,
    value: option.value,
  }
}

export async function listPayments(client: ApiClient, filters: PaymentListFilters = {}) {
  const page = await client.get<ApiPagedResult<ApiPaymentListItem>>('/v1/payments', {
    query: {
      contractId: filters.contractId,
      dueFrom: filters.dueFrom,
      dueTo: filters.dueTo,
      includeArchived: filters.includeArchived,
      locale: filters.locale,
      overdueOnly: filters.overdueOnly,
      page: filters.page,
      pageSize: filters.pageSize,
      propertyId: filters.propertyId,
      residentId: filters.residentId,
      search: filters.search,
      sort: filters.sort,
      status: filters.status,
    },
  })

  return {
    ...page,
    items: page.items.map(mapListItem),
  }
}

export async function getPayment(client: ApiClient, paymentId: string, locale?: AppLocale) {
  const payment = await client.get<ApiPaymentDetail>(`/v1/payments/${paymentId}`, {
    query: { locale },
  })

  return mapDetail(payment)
}

export async function createPayment(
  client: ApiClient,
  request: PaymentFormRequest,
  locale?: AppLocale,
) {
  const payment = await client.post<ApiPaymentDetail, ApiPaymentFormRequest>(
    '/v1/payments',
    toFormRequest(request),
    { query: { locale } },
  )

  return mapDetail(payment)
}

export async function updatePayment(
  client: ApiClient,
  paymentId: string,
  request: PaymentFormRequest,
  locale?: AppLocale,
) {
  const payment = await client.put<ApiPaymentDetail, ApiPaymentUpdateRequest>(
    `/v1/payments/${paymentId}`,
    toUpdateRequest(request),
    { query: { locale } },
  )

  return mapDetail(payment)
}

export async function recordPaymentTransaction(
  client: ApiClient,
  paymentId: string,
  request: PaymentTransactionRequest,
  locale?: AppLocale,
) {
  const payment = await client.post<ApiPaymentDetail, ApiPaymentTransactionRequest>(
    `/v1/payments/${paymentId}/transactions`,
    toTransactionRequest(request),
    { query: { locale } },
  )

  return mapDetail(payment)
}

export async function reversePaymentTransaction(
  client: ApiClient,
  paymentId: string,
  transactionId: string,
  notes?: string,
  locale?: AppLocale,
) {
  const payment = await client.post<ApiPaymentDetail, { notes?: string; transactionId: string }>(
    `/v1/payments/${paymentId}/transactions/reverse`,
    { notes, transactionId },
    { query: { locale } },
  )

  return mapDetail(payment)
}

export async function cancelPayment(
  client: ApiClient,
  paymentId: string,
  notes?: string,
  locale?: AppLocale,
) {
  const payment = await client.post<ApiPaymentDetail, { notes?: string }>(
    `/v1/payments/${paymentId}/cancel`,
    { notes },
    { query: { locale } },
  )

  return mapDetail(payment)
}

export function archivePayment(client: ApiClient, paymentId: string) {
  return client.delete<void>(`/v1/payments/${paymentId}`)
}

export async function restorePayment(client: ApiClient, paymentId: string, locale?: AppLocale) {
  const payment = await client.post<ApiPaymentDetail, Record<string, never>>(
    `/v1/payments/${paymentId}/restore`,
    {},
    { query: { locale } },
  )

  return mapDetail(payment)
}

export async function createPaymentInstruction(
  client: ApiClient,
  paymentId: string,
  providerCode: string,
  locale?: AppLocale,
) {
  const instruction = await client.post<ApiPaymentInstruction, { providerCode: string }>(
    `/v1/payments/${paymentId}/instructions`,
    { providerCode },
    { query: { locale } },
  )

  return mapInstruction(instruction)
}

export async function applyPaymentProviderEvent(
  client: ApiClient,
  request: PaymentProviderEventRequest,
  locale?: AppLocale,
) {
  const payment = await client.post<
    ApiPaymentDetail,
    Omit<PaymentProviderEventRequest, 'amount'> & { amount?: PaymentMoney }
  >(
    '/v1/payments/provider-events',
    {
      ...request,
      amount: toOptionalMoney(request.amount),
    },
    { query: { locale } },
  )

  return mapDetail(payment)
}

export function listPaymentStatusOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<Array<PaymentStatusLabel<PaymentStatus>>>('/v1/payments/status-options', {
    query: { locale },
  })
}

export async function listPaymentMethodOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiPaymentSelectOption<PaymentMethod>>>(
    '/v1/payments/method-options',
    { query: { locale } },
  )

  return options.map(mapPaymentSelectOption)
}

export function listPaymentReconciliationStatusOptions(client: ApiClient, locale?: AppLocale) {
  return client.get<Array<PaymentStatusLabel<PaymentReconciliationStatus>>>(
    '/v1/payments/reconciliation-status-options',
    { query: { locale } },
  )
}

export async function listPaymentProviderOptions(client: ApiClient, locale?: AppLocale) {
  const options = await client.get<Array<ApiPaymentSelectOption<PaymentProviderCode>>>(
    '/v1/payments/provider-options',
    { query: { locale } },
  )

  return options.map(mapPaymentSelectOption)
}
