import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, Ban, CheckCircle2, Eye, Pencil, Plus, RotateCcw } from 'lucide-react'
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
import type { ApiSelectOption } from '../../lib/api/contracts'
import {
  archiveUtilityAccount,
  cancelUtilityAccount,
  createUtilityAccount,
  getUtilityAccount,
  listUtilityAccountResponsibilityOptions,
  listUtilityAccounts,
  listUtilityAccountStatusOptions,
  listUtilityAccountTypeOptions,
  markUtilityAccountPaid,
  restoreUtilityAccount,
  updateUtilityAccount,
  utilityAccountResponsibilities,
  utilityAccountStatuses,
  utilityAccountTypes,
  type UtilityAccountDetail,
  type UtilityAccountFormRequest,
  type UtilityAccountListFilters,
  type UtilityAccountListItem,
  type UtilityAccountMarkPaidRequest,
  type UtilityAccountResponsibility,
  type UtilityAccountStatus,
  type UtilityAccountType,
} from '../../lib/api/utilityAccounts'
import { formatDate, formatMoney } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { UtilityAccountDetailPage } from './UtilityAccountDetailPage'
import { UtilityAccountForm, type UtilityAccountFormMode } from './UtilityAccountForm'
import { UtilityAccountMarkPaidDialog } from './UtilityAccountMarkPaidDialog'
import { getUtilityAccountCopy } from './utilityAccountCopy'

const utilityAccountFilterScope = 'utility-accounts.list'

const defaultFilters: UtilityAccountListFilters = {
  includeArchived: false,
  page: 1,
  pageSize: 10,
  responsibility: '',
  search: '',
  status: '',
  type: '',
}

const statusTones: Record<UtilityAccountStatus, StatusBadgeTone> = {
  archived: 'archived',
  cancelled: 'archived',
  disputed: 'danger',
  open: 'warning',
  overdue: 'danger',
  paid: 'success',
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
  mode: UtilityAccountFormMode
  utilityAccount?: Partial<UtilityAccountDetail | UtilityAccountListItem> &
    Pick<UtilityAccountListItem, 'id'>
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

function isUtilityAccountType(value: FilterValue | undefined): value is UtilityAccountType {
  return typeof value === 'string' && utilityAccountTypes.includes(value as UtilityAccountType)
}

function isUtilityAccountStatus(value: FilterValue | undefined): value is UtilityAccountStatus {
  return typeof value === 'string' && utilityAccountStatuses.includes(value as UtilityAccountStatus)
}

function isUtilityAccountResponsibility(
  value: FilterValue | undefined,
): value is UtilityAccountResponsibility {
  return (
    typeof value === 'string' &&
    utilityAccountResponsibilities.includes(value as UtilityAccountResponsibility)
  )
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

  return value === 'true'
}

function normalizeUtilityAccountFilters(filters: FilterSet | undefined): UtilityAccountListFilters {
  const responsibility = filters?.responsibility
  const status = filters?.status
  const type = filters?.type

  return {
    billingFrom: getStringFilter(filters?.billingFrom) || undefined,
    billingTo: getStringFilter(filters?.billingTo) || undefined,
    contractId: getStringFilter(filters?.contractId) || undefined,
    dueFrom: getStringFilter(filters?.dueFrom) || undefined,
    dueTo: getStringFilter(filters?.dueTo) || undefined,
    includeArchived: getBooleanFilter(filters?.includeArchived),
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    propertyId: getStringFilter(filters?.propertyId) || undefined,
    responsibility: isUtilityAccountResponsibility(responsibility) ? responsibility : '',
    search: getStringFilter(filters?.search),
    status: isUtilityAccountStatus(status) ? status : '',
    type: isUtilityAccountType(type) ? type : '',
  }
}

function toFilterSet(filters: UtilityAccountListFilters): FilterSet {
  const nextFilters: FilterSet = {
    includeArchived: filters.includeArchived ?? false,
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  if (filters.billingFrom) {
    nextFilters.billingFrom = filters.billingFrom
  }

  if (filters.billingTo) {
    nextFilters.billingTo = filters.billingTo
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

  if (filters.responsibility) {
    nextFilters.responsibility = filters.responsibility
  }

  if (filters.search) {
    nextFilters.search = filters.search
  }

  if (filters.status) {
    nextFilters.status = filters.status
  }

  if (filters.type) {
    nextFilters.type = filters.type
  }

  return nextFilters
}

function utilityTitleForAction(row: UtilityAccountListItem) {
  return row.property?.name ? `${row.title} - ${row.property.name}` : row.title
}

function getFormInitialValue(
  utilityAccount: UtilityAccountDetail | UtilityAccountListItem,
): Partial<UtilityAccountDetail> {
  return {
    amount: utilityAccount.amount,
    billDocuments: utilityAccount.billDocuments,
    billingPeriodEnd: utilityAccount.billingPeriodEnd,
    billingPeriodStart: utilityAccount.billingPeriodStart,
    concurrencyToken: utilityAccount.concurrencyToken,
    contract: utilityAccount.contract,
    description: utilityAccount.description,
    dueDate: utilityAccount.dueDate,
    id: utilityAccount.id,
    property: utilityAccount.property,
    receiptDocuments: utilityAccount.receiptDocuments,
    resident: utilityAccount.resident,
    responsibility: utilityAccount.responsibility,
    title: utilityAccount.title,
    type: utilityAccount.type,
  }
}

function withEmptyOption<TValue extends string>(
  label: string,
  options: Array<ApiSelectOption<TValue>>,
) {
  return [{ label, value: '' }, ...options]
}

export function UtilityAccountsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[utilityAccountFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getUtilityAccountCopy(locale)
  const canWriteUtilities = hasAnyPermission(['utility-accounts.write'], authUser)
  const canManageUtilities = hasAnyPermission(['utility-accounts.manage'], authUser)
  const canArchiveUtilities = hasAnyPermission(['utility-accounts.archive'], authUser)
  const canReadDocuments = hasAnyPermission(['documents.read'], authUser)
  const filters = useMemo(() => normalizeUtilityAccountFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.status === 'archived',
      locale,
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailUtilityAccountId, setDetailUtilityAccountId] = useState<string | null>(null)
  const [markPaidUtilityAccount, setMarkPaidUtilityAccount] =
    useState<UtilityAccountListItem | null>(null)
  const utilityAccountsQuery = useQuery({
    queryFn: () => listUtilityAccounts(apiClient, apiFilters),
    queryKey: ['utility-accounts', 'list', apiFilters],
  })
  const typeOptionsQuery = useQuery({
    queryFn: () => listUtilityAccountTypeOptions(apiClient, locale),
    queryKey: ['utility-accounts', 'type-options', locale],
  })
  const statusOptionsQuery = useQuery({
    queryFn: () => listUtilityAccountStatusOptions(apiClient, locale),
    queryKey: ['utility-accounts', 'status-options', locale],
  })
  const responsibilityOptionsQuery = useQuery({
    queryFn: () => listUtilityAccountResponsibilityOptions(apiClient, locale),
    queryKey: ['utility-accounts', 'responsibility-options', locale],
  })
  const rows = utilityAccountsQuery.data?.items ?? []
  const page = utilityAccountsQuery.data?.page ?? filters.page ?? 1
  const pageSize = utilityAccountsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const typeOptions =
    typeOptionsQuery.data ??
    utilityAccountTypes.map((type) => ({ label: copy.terms.types[type], value: type }))
  const statusOptions =
    statusOptionsQuery.data ??
    utilityAccountStatuses.map((status) => ({
      label: copy.list.status[status],
      value: status,
    }))
  const responsibilityOptions =
    responsibilityOptionsQuery.data ??
    utilityAccountResponsibilities.map((responsibility) => ({
      label: copy.terms.responsibilities[responsibility],
      value: responsibility,
    }))
  const activeFilterCount = [
    filters.search,
    filters.type,
    filters.status,
    filters.responsibility,
    filters.billingFrom,
    filters.billingTo,
    filters.dueFrom,
    filters.dueTo,
    filters.propertyId,
    filters.contractId,
    filters.includeArchived ? 'true' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const formMutation = useMutation({
    mutationFn: (values: UtilityAccountFormRequest) => {
      if (formState?.mode === 'edit' && formState.utilityAccount) {
        return updateUtilityAccount(apiClient, formState.utilityAccount.id, values, locale)
      }

      return createUtilityAccount(apiClient, values, locale)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['utility-accounts'] })
    },
  })
  const markPaidMutation = useMutation({
    mutationFn: (request: { utilityAccountId: string; values: UtilityAccountMarkPaidRequest }) =>
      markUtilityAccountPaid(apiClient, request.utilityAccountId, request.values, locale),
    onSuccess: async () => {
      setMarkPaidUtilityAccount(null)
      await queryClient.invalidateQueries({ queryKey: ['utility-accounts'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: {
      action: 'archive' | 'cancel' | 'restore'
      utilityAccountId: string
    }) => {
      if (request.action === 'archive') {
        await archiveUtilityAccount(apiClient, request.utilityAccountId)
        return
      }

      if (request.action === 'cancel') {
        await cancelUtilityAccount(apiClient, request.utilityAccountId, undefined, locale)
        return
      }

      await restoreUtilityAccount(apiClient, request.utilityAccountId, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['utility-accounts'] })
    },
  })
  const editUtilityAccountMutation = useMutation({
    mutationFn: (utilityAccountId: string) =>
      getUtilityAccount(apiClient, utilityAccountId, locale),
    onSuccess: (utilityAccount) => {
      setFormState({
        mode: 'edit',
        utilityAccount: getFormInitialValue(utilityAccount) as FormState['utilityAccount'],
      })
    },
  })

  function updateFilters(nextFilters: Partial<UtilityAccountListFilters>) {
    setStoredFilters(
      utilityAccountFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      utilityAccountFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: UtilityAccountListItem) {
    const rowTitle = utilityTitleForAction(row)
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${rowTitle}`}
        onClick={() => setDetailUtilityAccountId(row.id)}
      />,
    ]

    if (canManageUtilities && !['archived', 'cancelled', 'paid'].includes(row.status.code)) {
      actions.push(
        <IconActionButton
          key="mark-paid"
          icon={<CheckCircle2 aria-hidden="true" size={16} />}
          label={`${copy.list.markPaid} ${rowTitle}`}
          onClick={() => setMarkPaidUtilityAccount(row)}
        />,
        <IconActionButton
          key="cancel"
          icon={<Ban aria-hidden="true" size={16} />}
          isDestructive
          label={`${copy.list.cancel} ${rowTitle}`}
          onClick={() => lifecycleMutation.mutate({ action: 'cancel', utilityAccountId: row.id })}
        />,
      )
    }

    if (canWriteUtilities && !['archived', 'cancelled'].includes(row.status.code)) {
      actions.push(
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${rowTitle}`}
          onClick={() => editUtilityAccountMutation.mutate(row.id)}
        />,
      )
    }

    if (canArchiveUtilities) {
      actions.push(
        row.status.code === 'archived' ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${rowTitle}`}
            onClick={() =>
              lifecycleMutation.mutate({ action: 'restore', utilityAccountId: row.id })
            }
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${rowTitle}`}
            onClick={() =>
              lifecycleMutation.mutate({ action: 'archive', utilityAccountId: row.id })
            }
          />
        ),
      )
    }

    return actions
  }

  const markPaidDialog = (
    <UtilityAccountMarkPaidDialog
      canReadDocuments={canReadDocuments}
      isOpen={Boolean(markPaidUtilityAccount)}
      isSubmitting={markPaidMutation.isPending}
      onOpenChange={(isOpen) => {
        if (!isOpen) {
          setMarkPaidUtilityAccount(null)
        }
      }}
      onSubmit={async (values) => {
        if (markPaidUtilityAccount) {
          await markPaidMutation.mutateAsync({
            utilityAccountId: markPaidUtilityAccount.id,
            values,
          })
        }
      }}
      utilityAccount={markPaidUtilityAccount ?? undefined}
    />
  )

  if (detailUtilityAccountId) {
    return (
      <>
        <UtilityAccountDetailPage
          utilityAccountId={detailUtilityAccountId}
          onBack={() => setDetailUtilityAccountId(null)}
          onCancel={
            canManageUtilities
              ? (utilityAccount) =>
                  lifecycleMutation.mutate({
                    action: 'cancel',
                    utilityAccountId: utilityAccount.id,
                  })
              : undefined
          }
          onEdit={
            canWriteUtilities
              ? (utilityAccount) => {
                  setDetailUtilityAccountId(null)
                  setFormState({
                    mode: 'edit',
                    utilityAccount: getFormInitialValue(
                      utilityAccount,
                    ) as FormState['utilityAccount'],
                  })
                }
              : undefined
          }
          onMarkPaid={
            canManageUtilities
              ? (utilityAccount) => setMarkPaidUtilityAccount(utilityAccount)
              : undefined
          }
        />
        {markPaidDialog}
      </>
    )
  }

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        title={copy.list.pageTitle}
        description={copy.list.pageDescription}
        primaryAction={
          canWriteUtilities
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
          <span style={{ fontWeight: 800 }}>{copy.list.columns.type}</span>
          <select
            aria-label={copy.list.columns.type}
            onChange={(event) =>
              updateFilters({ type: event.currentTarget.value as UtilityAccountType | '' })
            }
            value={filters.type}
          >
            {withEmptyOption(copy.list.allTypes, typeOptions).map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.columns.status}</span>
          <select
            aria-label={copy.list.columns.status}
            onChange={(event) =>
              updateFilters({ status: event.currentTarget.value as UtilityAccountStatus | '' })
            }
            value={filters.status}
          >
            {withEmptyOption(copy.list.allStatuses, statusOptions).map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.columns.responsibility}</span>
          <select
            aria-label={copy.list.columns.responsibility}
            onChange={(event) =>
              updateFilters({
                responsibility: event.currentTarget.value as UtilityAccountResponsibility | '',
              })
            }
            value={filters.responsibility}
          >
            {withEmptyOption(copy.list.allResponsibilities, responsibilityOptions).map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
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
        <TextInput
          aria-label={copy.list.billingFrom}
          onChange={(event) => updateFilters({ billingFrom: event.currentTarget.value })}
          style={{ maxWidth: 150 }}
          type="date"
          value={filters.billingFrom ?? ''}
        />
        <TextInput
          aria-label={copy.list.billingTo}
          onChange={(event) => updateFilters({ billingTo: event.currentTarget.value })}
          style={{ maxWidth: 150 }}
          type="date"
          value={filters.billingTo ?? ''}
        />
        <TextInput
          aria-label={copy.list.propertyId}
          onChange={(event) => updateFilters({ propertyId: event.currentTarget.value })}
          placeholder={copy.list.propertyId}
          style={{ maxWidth: 210 }}
          value={filters.propertyId ?? ''}
        />
        <TextInput
          aria-label={copy.list.contractId}
          onChange={(event) => updateFilters({ contractId: event.currentTarget.value })}
          placeholder={copy.list.contractId}
          style={{ maxWidth: 210 }}
          value={filters.contractId ?? ''}
        />
        <label style={{ alignItems: 'center', display: 'flex', gap: 8, minHeight: 44 }}>
          <input
            checked={Boolean(filters.includeArchived)}
            onChange={(event) => updateFilters({ includeArchived: event.currentTarget.checked })}
            style={{ accentColor: 'var(--als-color-primary, #1877f2)', height: 18, width: 18 }}
            type="checkbox"
          />
          <span style={{ fontWeight: 800 }}>{copy.list.includeArchived}</span>
        </label>
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
            header: copy.list.columns.utilityAccount,
            id: 'utilityAccount',
          },
          {
            cell: (row) => row.type.label,
            header: copy.list.columns.type,
            id: 'type',
            width: 150,
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
            cell: (row) => row.responsibility.label,
            header: copy.list.columns.responsibility,
            id: 'responsibility',
            width: 150,
          },
          {
            cell: (row) => formatDate(row.dueDate, { locale }),
            header: copy.list.columns.dueDate,
            id: 'dueDate',
            width: 140,
          },
          {
            cell: (row) =>
              formatMoney(row.amount.amount, {
                currency: row.amount.currency,
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
            width: 260,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          utilityAccountsQuery.isError ? (
            <ErrorState
              title={copy.list.error}
              onRetry={() => void utilityAccountsQuery.refetch()}
            />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={utilityAccountsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={utilityAccountsQuery.data?.totalItems ?? 0}
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
          <UtilityAccountForm
            canReadDocuments={canReadDocuments}
            initialValue={formState.utilityAccount}
            isSubmitting={formMutation.isPending}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onSubmit={async (values) => {
              await formMutation.mutateAsync(values)
            }}
            responsibilityOptions={responsibilityOptions}
            typeOptions={typeOptions}
          />
        ) : null}
      </Drawer>

      {markPaidDialog}
    </section>
  )
}
