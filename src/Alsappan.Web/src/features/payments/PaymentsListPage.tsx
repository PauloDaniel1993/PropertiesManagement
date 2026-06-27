import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, Eye, Pencil, Plus, QrCode, ReceiptText, RotateCcw } from 'lucide-react'
import {
  DataTable,
  Drawer,
  EmptyState,
  ErrorState,
  FilterBar,
  PageHeader,
  Pagination,
  SearchInput,
  StatusBadge,
  TextInput,
  type StatusBadgeTone,
} from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import {
  archivePayment,
  createPayment,
  createPaymentInstruction,
  listPaymentMethodOptions,
  listPaymentProviderOptions,
  listPaymentReconciliationStatusOptions,
  listPayments,
  paymentStatuses,
  recordPaymentTransaction,
  restorePayment,
  updatePayment,
  type PaymentDetail,
  type PaymentFormRequest,
  type PaymentInstruction,
  type PaymentListFilters,
  type PaymentListItem,
  type PaymentMethod,
  type PaymentStatus,
  type PaymentTransactionRequest,
} from '../../lib/api/payments'
import { formatDate, formatMoney } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { PaymentDetailPage } from './PaymentDetailPage'
import { PaymentForm, type PaymentFormMode } from './PaymentForm'
import { PaymentInstructionDialog } from './PaymentInstructionDialog'
import { PaymentSettlementDialog } from './PaymentSettlementDialog'
import { getPaymentCopy } from './paymentCopy'

const paymentFilterScope = 'payments.list'

const defaultFilters: PaymentListFilters = {
  overdueOnly: false,
  page: 1,
  pageSize: 10,
  search: '',
  status: '',
}

const statusTones: Record<PaymentStatus, StatusBadgeTone> = {
  archived: 'archived',
  cancelled: 'archived',
  disputed: 'danger',
  overdue: 'danger',
  paid: 'success',
  'partially-paid': 'warning',
  pending: 'warning',
}

const iconButtonStyle: CSSProperties = {
  alignItems: 'center',
  background: 'var(--als-color-surface, #ffffff)',
  border: '1px solid var(--als-color-border, #d7deea)',
  borderRadius: 'var(--als-radius-sm, 6px)',
  color: 'var(--als-color-text, #1f2937)',
  display: 'inline-flex',
  height: 36,
  justifyContent: 'center',
  padding: 0,
  width: 36,
}

type FormState = {
  mode: PaymentFormMode
  payment?: Partial<PaymentDetail | PaymentListItem> & Pick<PaymentListItem, 'id'>
}

type InstructionState = {
  instruction?: PaymentInstruction
  payment: PaymentListItem
}

type IconActionButtonProps = {
  icon: ReactNode
  isDestructive?: boolean
  label: string
  onClick: () => void
}

function IconActionButton({ icon, isDestructive = false, label, onClick }: IconActionButtonProps) {
  return (
    <button
      aria-label={label}
      onClick={onClick}
      style={{
        ...iconButtonStyle,
        color: isDestructive ? 'var(--als-color-danger, #b42318)' : iconButtonStyle.color,
      }}
      title={label}
      type="button"
    >
      {icon}
    </button>
  )
}

function isPaymentStatus(value: FilterValue | undefined): value is PaymentStatus {
  return typeof value === 'string' && paymentStatuses.includes(value as PaymentStatus)
}

function getStringFilter(value: FilterValue | undefined) {
  return typeof value === 'string' ? value : ''
}

function getNumberFilter(value: FilterValue | undefined) {
  return typeof value === 'number' && Number.isFinite(value) ? value : undefined
}

function getPageFilter(value: FilterValue | undefined, fallback: number) {
  const numericValue = getNumberFilter(value)

  return numericValue && numericValue > 0 ? numericValue : fallback
}

function getBooleanFilter(value: FilterValue | undefined) {
  if (value === true || value === false) {
    return value
  }

  if (value === 'true') {
    return true
  }

  return false
}

function normalizePaymentFilters(filters: FilterSet | undefined): PaymentListFilters {
  const status = filters?.status

  return {
    contractId: getStringFilter(filters?.contractId) || undefined,
    dueFrom: getStringFilter(filters?.dueFrom) || undefined,
    dueTo: getStringFilter(filters?.dueTo) || undefined,
    overdueOnly: getBooleanFilter(filters?.overdueOnly),
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    propertyId: getStringFilter(filters?.propertyId) || undefined,
    residentId: getStringFilter(filters?.residentId) || undefined,
    search: getStringFilter(filters?.search),
    status: isPaymentStatus(status) ? status : '',
  }
}

function toFilterSet(filters: PaymentListFilters): FilterSet {
  const nextFilters: FilterSet = {
    overdueOnly: filters.overdueOnly ?? false,
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  if (filters.contractId) {
    nextFilters.contractId = filters.contractId
  }

  if (filters.dueFrom) {
    nextFilters.dueFrom = filters.dueFrom
  }

  if (filters.dueTo) {
    nextFilters.dueTo = filters.dueTo
  }

  if (filters.propertyId) {
    nextFilters.propertyId = filters.propertyId
  }

  if (filters.residentId) {
    nextFilters.residentId = filters.residentId
  }

  if (filters.search) {
    nextFilters.search = filters.search
  }

  if (filters.status) {
    nextFilters.status = filters.status
  }

  return nextFilters
}

function paymentTitleForAction(row: PaymentListItem) {
  return row.property?.name ? `${row.title} - ${row.property.name}` : row.title
}

function getFormInitialValue(payment: PaymentDetail | PaymentListItem): Partial<PaymentDetail> {
  return {
    amount: payment.amount,
    concurrencyToken: payment.concurrencyToken,
    contract: payment.contract,
    description: payment.description,
    discountAmount: payment.discountAmount,
    dueDate: payment.dueDate,
    id: payment.id,
    notes: 'notes' in payment ? payment.notes : undefined,
    penaltyAmount: payment.penaltyAmount,
    preferredMethod: payment.preferredMethod,
    property: payment.property,
    reconciliationStatus:
      'reconciliationStatus' in payment
        ? payment.reconciliationStatus
        : { code: 'not-required', label: 'Nao requer' },
    resident: payment.resident,
    title: payment.title,
    utilityAccountId: 'utilityAccountId' in payment ? payment.utilityAccountId : undefined,
  }
}

export function PaymentsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[paymentFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getPaymentCopy(locale)
  const canWritePayments = hasAnyPermission(['payments.write'], authUser)
  const canManagePayments = hasAnyPermission(['payments.manage'], authUser)
  const canArchivePayments = hasAnyPermission(['payments.archive'], authUser)
  const filters = useMemo(() => normalizePaymentFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.status === 'archived',
      locale,
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailPaymentId, setDetailPaymentId] = useState<string | null>(null)
  const [settlementPayment, setSettlementPayment] = useState<PaymentListItem | null>(null)
  const [instructionState, setInstructionState] = useState<InstructionState | null>(null)
  const paymentsQuery = useQuery({
    queryFn: () => listPayments(apiClient, apiFilters),
    queryKey: ['payments', 'list', apiFilters],
  })
  const methodOptionsQuery = useQuery({
    queryFn: () => listPaymentMethodOptions(apiClient, locale),
    queryKey: ['payments', 'method-options', locale],
  })
  const reconciliationOptionsQuery = useQuery({
    queryFn: () => listPaymentReconciliationStatusOptions(apiClient, locale),
    queryKey: ['payments', 'reconciliation-status-options', locale],
  })
  const providerOptionsQuery = useQuery({
    queryFn: () => listPaymentProviderOptions(apiClient, locale),
    queryKey: ['payments', 'provider-options', locale],
  })
  const rows = paymentsQuery.data?.items ?? []
  const page = paymentsQuery.data?.page ?? filters.page ?? 1
  const pageSize = paymentsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const activeFilterCount = [
    filters.search,
    filters.status,
    filters.dueFrom,
    filters.dueTo,
    filters.contractId,
    filters.propertyId,
    filters.residentId,
    filters.overdueOnly ? 'true' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const statusOptions = useMemo(
    () => [
      { label: copy.list.allStatuses, value: '' },
      ...paymentStatuses.map((status) => ({
        label: copy.list.status[status],
        value: status,
      })),
    ],
    [copy],
  )
  const methodOptions = useMemo(
    () =>
      methodOptionsQuery.data ??
      ([
        { label: copy.terms.methods.pix, value: 'pix' },
        { label: copy.terms.methods.boleto, value: 'boleto' },
        { label: copy.terms.methods.paypal, value: 'paypal' },
        { label: copy.terms.methods['bank-transfer'], value: 'bank-transfer' },
      ] as Array<{ disabled?: boolean; label: string; value: PaymentMethod }>),
    [copy, methodOptionsQuery.data],
  )
  const reconciliationOptions =
    reconciliationOptionsQuery.data ??
    ([
      { code: 'not-required', label: copy.terms.reconciliationStatuses['not-required'] },
      { code: 'pending', label: copy.terms.reconciliationStatuses.pending },
    ] as Array<{ code: 'not-required' | 'pending'; label: string }>)
  const formMutation = useMutation({
    mutationFn: (values: PaymentFormRequest) => {
      if (formState?.mode === 'edit' && formState.payment) {
        return updatePayment(apiClient, formState.payment.id, values, locale)
      }

      return createPayment(apiClient, values, locale)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['payments'] })
    },
  })
  const settlementMutation = useMutation({
    mutationFn: (request: { paymentId: string; values: PaymentTransactionRequest }) =>
      recordPaymentTransaction(apiClient, request.paymentId, request.values, locale),
    onSuccess: async () => {
      setSettlementPayment(null)
      await queryClient.invalidateQueries({ queryKey: ['payments'] })
    },
  })
  const archiveMutation = useMutation({
    mutationFn: async (request: { action: 'archive' | 'restore'; paymentId: string }) => {
      if (request.action === 'archive') {
        await archivePayment(apiClient, request.paymentId)
        return
      }

      await restorePayment(apiClient, request.paymentId, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['payments'] })
    },
  })
  const instructionMutation = useMutation({
    mutationFn: (request: { paymentId: string; providerCode: string }) =>
      createPaymentInstruction(apiClient, request.paymentId, request.providerCode, locale),
    onSuccess: (instruction) => {
      setInstructionState((current) => (current ? { ...current, instruction } : current))
      void queryClient.invalidateQueries({ queryKey: ['payments'] })
    },
  })

  function updateFilters(nextFilters: Partial<PaymentListFilters>) {
    setStoredFilters(
      paymentFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      paymentFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: PaymentListItem) {
    const rowTitle = paymentTitleForAction(row)
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${rowTitle}`}
        onClick={() => setDetailPaymentId(row.id)}
      />,
    ]

    if (canManagePayments && !['archived', 'cancelled', 'paid'].includes(row.status.code)) {
      actions.push(
        <IconActionButton
          key="settle"
          icon={<ReceiptText aria-hidden="true" size={16} />}
          label={`${copy.list.settle} ${rowTitle}`}
          onClick={() => setSettlementPayment(row)}
        />,
      )
    }

    if (canWritePayments && !['archived', 'cancelled'].includes(row.status.code)) {
      actions.push(
        <IconActionButton
          key="instructions"
          icon={<QrCode aria-hidden="true" size={16} />}
          label={`${copy.list.instructions} ${rowTitle}`}
          onClick={() => setInstructionState({ payment: row })}
        />,
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${rowTitle}`}
          onClick={() =>
            setFormState({
              mode: 'edit',
              payment: getFormInitialValue(row) as FormState['payment'],
            })
          }
        />,
      )
    }

    if (canArchivePayments) {
      actions.push(
        row.status.code === 'archived' ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${rowTitle}`}
            onClick={() => archiveMutation.mutate({ action: 'restore', paymentId: row.id })}
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${rowTitle}`}
            onClick={() => archiveMutation.mutate({ action: 'archive', paymentId: row.id })}
          />
        ),
      )
    }

    return actions
  }

  if (detailPaymentId) {
    return (
      <PaymentDetailPage
        paymentId={detailPaymentId}
        onBack={() => setDetailPaymentId(null)}
        onEdit={
          canWritePayments
            ? (payment) => {
                setDetailPaymentId(null)
                setFormState({
                  mode: 'edit',
                  payment: getFormInitialValue(payment) as FormState['payment'],
                })
              }
            : undefined
        }
        onInstructions={
          canWritePayments
            ? (payment) =>
                setInstructionState({
                  payment,
                })
            : undefined
        }
        onReverseTransaction={undefined}
        onSettle={canManagePayments ? (payment) => setSettlementPayment(payment) : undefined}
      />
    )
  }

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        title={copy.list.pageTitle}
        description={copy.list.pageDescription}
        primaryAction={
          canWritePayments
            ? {
                icon: <Plus aria-hidden="true" size={18} />,
                label: copy.list.add,
                onClick: () => setFormState({ mode: 'create' }),
              }
            : undefined
        }
      />

      <FilterBar
        activeCount={activeFilterCount}
        clearLabel={copy.list.clearFilters}
        label={copy.list.filters}
        onClear={clearFilters}
        summary={activeFilterCount > 0 ? copy.list.activeFilters(activeFilterCount) : undefined}
      >
        <SearchInput
          clearLabel={copy.list.clearFilters}
          label={copy.list.search}
          onClear={() => updateFilters({ search: '' })}
          onValueChange={(search) => updateFilters({ search })}
          placeholder={copy.list.searchPlaceholder}
          value={filters.search}
        />
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.columns.status}</span>
          <select
            aria-label={copy.list.columns.status}
            onChange={(event) =>
              updateFilters({ status: event.currentTarget.value as PaymentStatus | '' })
            }
            value={filters.status}
          >
            {statusOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ alignItems: 'center', display: 'flex', gap: 8, minHeight: 44 }}>
          <input
            checked={Boolean(filters.overdueOnly)}
            onChange={(event) => updateFilters({ overdueOnly: event.currentTarget.checked })}
            style={{ accentColor: 'var(--als-color-primary, #1877f2)', height: 18, width: 18 }}
            type="checkbox"
          />
          <span style={{ fontWeight: 800 }}>{copy.list.overdueOnly}</span>
        </label>
        <TextInput
          aria-label={copy.list.dueFrom}
          onChange={(event) => updateFilters({ dueFrom: event.currentTarget.value })}
          style={{ maxWidth: 150 }}
          type="date"
          value={filters.dueFrom ?? ''}
        />
        <TextInput
          aria-label={copy.list.dueTo}
          onChange={(event) => updateFilters({ dueTo: event.currentTarget.value })}
          style={{ maxWidth: 150 }}
          type="date"
          value={filters.dueTo ?? ''}
        />
      </FilterBar>

      <DataTable
        columns={[
          {
            cell: (row) => (
              <div style={{ display: 'grid', gap: 4, minWidth: 0 }}>
                <strong>{row.title}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {row.property?.name ??
                    row.resident?.name ??
                    row.contract?.name ??
                    row.description ??
                    '-'}
                </span>
              </div>
            ),
            header: copy.list.columns.payment,
            id: 'payment',
          },
          {
            cell: (row) => (
              <StatusBadge label={row.status.label} tone={statusTones[row.status.code]} />
            ),
            header: copy.list.columns.status,
            id: 'status',
            width: 150,
          },
          {
            cell: (row) => formatDate(row.dueDate, { locale }),
            header: copy.list.columns.dueDate,
            id: 'dueDate',
            width: 140,
          },
          {
            cell: (row) => row.preferredMethodLabel,
            header: copy.list.columns.method,
            id: 'method',
            width: 150,
          },
          {
            cell: (row) =>
              formatMoney(row.grossAmount.amount, {
                currency: row.grossAmount.currency,
                locale,
              }),
            header: copy.list.columns.amount,
            id: 'amount',
            width: 140,
          },
          {
            cell: (row) =>
              formatMoney(row.balance.amount, {
                currency: row.balance.currency,
                locale,
              }),
            header: copy.list.columns.balance,
            id: 'balance',
            width: 140,
          },
          {
            align: 'right',
            cell: (row) => (
              <div
                style={{
                  display: 'inline-flex',
                  flexWrap: 'wrap',
                  gap: 6,
                  justifyContent: 'flex-end',
                }}
              >
                {getRowActions(row)}
              </div>
            ),
            header: copy.list.columns.actions,
            id: 'actions',
            width: 240,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          paymentsQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void paymentsQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={paymentsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={paymentsQuery.data?.totalItems ?? 0}
      />

      <Drawer
        isOpen={Boolean(formState)}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setFormState(null)
          }
        }}
        title={formState ? copy.form.modeTitle[formState.mode] : copy.form.modeTitle.create}
        width={680}
      >
        {formState ? (
          <PaymentForm
            initialValue={formState.payment}
            isSubmitting={formMutation.isPending}
            methodOptions={methodOptions}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onSubmit={async (values) => {
              await formMutation.mutateAsync(values)
            }}
            reconciliationOptions={reconciliationOptions}
          />
        ) : null}
      </Drawer>

      <PaymentSettlementDialog
        isOpen={Boolean(settlementPayment)}
        isSubmitting={settlementMutation.isPending}
        methodOptions={methodOptions}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setSettlementPayment(null)
          }
        }}
        onSubmit={async (values) => {
          if (settlementPayment) {
            await settlementMutation.mutateAsync({ paymentId: settlementPayment.id, values })
          }
        }}
        payment={settlementPayment ?? undefined}
      />

      <PaymentInstructionDialog
        instruction={instructionState?.instruction}
        isGenerating={instructionMutation.isPending}
        isOpen={Boolean(instructionState)}
        onGenerate={async (providerCode) => {
          if (instructionState) {
            await instructionMutation.mutateAsync({
              paymentId: instructionState.payment.id,
              providerCode,
            })
          }
        }}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setInstructionState(null)
          }
        }}
        payment={instructionState?.payment}
        providerOptions={providerOptionsQuery.data ?? []}
      />
    </section>
  )
}
