import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Archive,
  Ban,
  Eye,
  FileSignature,
  Pencil,
  PlayCircle,
  RotateCcw,
  XCircle,
} from 'lucide-react'
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
  activateLeaseContract,
  archiveLeaseContract,
  cancelLeaseContract,
  contractStatuses,
  createLeaseContract,
  listLeaseContracts,
  restoreLeaseContract,
  terminateLeaseContract,
  updateLeaseContract,
  type ContractDetail,
  type ContractFormRequest,
  type ContractListFilters,
  type ContractListItem,
  type ContractStatus,
} from '../../lib/api/leaseContracts'
import type { AppLocale } from '../../i18n'
import { listProperties } from '../../lib/api/properties'
import { listResidents } from '../../lib/api/residents'
import { formatDate, formatMoney } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { ContractDetailPage } from './ContractDetailPage'
import { ContractForm, type ContractFormMode } from './ContractForm'
import { getContractCopy } from './contractCopy'

const contractFilterScope = 'contracts.list'

const defaultFilters: ContractListFilters = {
  endingSoonOnly: false,
  page: 1,
  pageSize: 10,
  propertyId: '',
  residentId: '',
  search: '',
  status: '',
}

const statusTones: Record<ContractStatus, StatusBadgeTone> = {
  active: 'success',
  archived: 'archived',
  cancelled: 'danger',
  draft: 'neutral',
  ended: 'danger',
  'ending-soon': 'warning',
  terminated: 'danger',
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
  contract?: Partial<ContractDetail> & Pick<ContractListItem, 'id'>
  mode: ContractFormMode
}

type IconActionButtonProps = {
  icon: ReactNode
  isDestructive?: boolean
  label: string
  onClick: () => void
}

function isContractStatus(value: FilterValue | undefined): value is ContractStatus {
  return typeof value === 'string' && contractStatuses.includes(value as ContractStatus)
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

function normalizeContractFilters(filters: FilterSet | undefined): ContractListFilters {
  const status = filters?.status

  return {
    endingSoonOnly: getBooleanFilter(filters?.endingSoonOnly),
    endsFrom: getStringFilter(filters?.endsFrom) || undefined,
    endsTo: getStringFilter(filters?.endsTo) || undefined,
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    propertyId: getStringFilter(filters?.propertyId),
    residentId: getStringFilter(filters?.residentId),
    search: getStringFilter(filters?.search),
    startsFrom: getStringFilter(filters?.startsFrom) || undefined,
    startsTo: getStringFilter(filters?.startsTo) || undefined,
    status: isContractStatus(status) ? status : '',
  }
}

function toFilterSet(filters: ContractListFilters): FilterSet {
  const nextFilters: FilterSet = {
    endingSoonOnly: filters.endingSoonOnly ?? false,
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  if (filters.endsFrom) {
    nextFilters.endsFrom = filters.endsFrom
  }

  if (filters.endsTo) {
    nextFilters.endsTo = filters.endsTo
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

  if (filters.startsFrom) {
    nextFilters.startsFrom = filters.startsFrom
  }

  if (filters.startsTo) {
    nextFilters.startsTo = filters.startsTo
  }

  if (filters.status) {
    nextFilters.status = filters.status
  }

  return nextFilters
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

function formatDateRange(contract: ContractListItem, locale: AppLocale) {
  const start = formatDate(contract.startDate, { locale })
  const end = contract.endDate ? formatDate(contract.endDate, { locale }) : undefined

  return end ? `${start} - ${end}` : start
}

function getFormInitialValue(contract: ContractDetail | ContractListItem): Partial<ContractDetail> {
  return {
    adjustmentIndex: contract.adjustmentIndex,
    adjustmentIntervalMonths:
      'adjustmentIntervalMonths' in contract ? contract.adjustmentIntervalMonths : 12,
    depositAmount: 'depositAmount' in contract ? contract.depositAmount : undefined,
    discountNotes: 'discountNotes' in contract ? contract.discountNotes : undefined,
    dueDay: contract.dueDay,
    endDate: contract.endDate,
    generatePaymentsAutomatically:
      'generatePaymentsAutomatically' in contract ? contract.generatePaymentsAutomatically : true,
    id: contract.id,
    monthlyRent: contract.monthlyRent,
    nextAdjustmentDate: 'nextAdjustmentDate' in contract ? contract.nextAdjustmentDate : undefined,
    notes: 'notes' in contract ? contract.notes : undefined,
    penaltyNotes: 'penaltyNotes' in contract ? contract.penaltyNotes : undefined,
    concurrencyToken: contract.concurrencyToken,
    primaryResident: contract.primaryResident,
    property: contract.property,
    residents: contract.residents,
    startDate: contract.startDate,
    status: contract.status,
  }
}

export function ContractsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[contractFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getContractCopy(locale)
  const canWriteContracts = hasAnyPermission(['contracts.write'], authUser)
  const canManageContracts = hasAnyPermission(['contracts.manage'], authUser)
  const canArchiveContracts = hasAnyPermission(['contracts.archive'], authUser)
  const filters = useMemo(() => normalizeContractFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.status === 'archived',
      locale,
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailContractId, setDetailContractId] = useState<string | null>(null)
  const contractsQuery = useQuery({
    queryFn: () => listLeaseContracts(apiClient, apiFilters),
    queryKey: ['contracts', 'list', apiFilters],
  })
  const propertiesQuery = useQuery({
    queryFn: () => listProperties(apiClient, { pageSize: 100 }),
    queryKey: ['properties', 'contract-filter-picker'],
  })
  const residentsQuery = useQuery({
    queryFn: () => listResidents(apiClient, { pageSize: 100 }),
    queryKey: ['residents', 'contract-filter-picker'],
  })
  const rows = contractsQuery.data?.items ?? []
  const page = contractsQuery.data?.page ?? filters.page ?? 1
  const pageSize = contractsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const activeFilterCount = [
    filters.search,
    filters.status,
    filters.propertyId,
    filters.residentId,
    filters.startsFrom,
    filters.startsTo,
    filters.endsFrom,
    filters.endsTo,
    filters.endingSoonOnly ? 'true' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const statusOptions = useMemo(
    () => [
      { label: copy.list.allStatuses, value: '' },
      ...contractStatuses.map((status) => ({
        label: copy.list.status[status],
        value: status,
      })),
    ],
    [copy],
  )
  const propertyOptions = useMemo(
    () => [
      { label: copy.list.allProperties, value: '' },
      ...(propertiesQuery.data?.items ?? []).map((property) => ({
        label: property.name,
        value: property.id,
      })),
    ],
    [copy, propertiesQuery.data?.items],
  )
  const residentOptions = useMemo(
    () => [
      { label: copy.list.allResidents, value: '' },
      ...(residentsQuery.data?.items ?? []).map((resident) => ({
        label: resident.fullName,
        value: resident.id,
      })),
    ],
    [copy, residentsQuery.data?.items],
  )
  const formMutation = useMutation({
    mutationFn: (values: ContractFormRequest) => {
      if (formState?.mode === 'edit' && formState.contract) {
        return updateLeaseContract(apiClient, formState.contract.id, values)
      }

      return createLeaseContract(apiClient, values)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['contracts'] })
      await queryClient.invalidateQueries({ queryKey: ['properties'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: {
      action: 'activate' | 'archive' | 'cancel' | 'restore' | 'terminate'
      id: string
    }) => {
      if (request.action === 'activate') {
        await activateLeaseContract(apiClient, request.id)
        return
      }

      if (request.action === 'archive') {
        await archiveLeaseContract(apiClient, request.id)
        return
      }

      if (request.action === 'cancel') {
        await cancelLeaseContract(apiClient, request.id)
        return
      }

      if (request.action === 'restore') {
        await restoreLeaseContract(apiClient, request.id)
        return
      }

      await terminateLeaseContract(apiClient, request.id)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['contracts'] })
      await queryClient.invalidateQueries({ queryKey: ['properties'] })
    },
  })

  function updateFilters(nextFilters: Partial<ContractListFilters>) {
    setStoredFilters(
      contractFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      contractFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: ContractListItem) {
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${row.property.name}`}
        onClick={() => setDetailContractId(row.id)}
      />,
    ]

    if (canWriteContracts && !['archived', 'cancelled', 'terminated'].includes(row.status.code)) {
      actions.push(
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${row.property.name}`}
          onClick={() =>
            setFormState({
              contract: getFormInitialValue(row) as FormState['contract'],
              mode: 'edit',
            })
          }
        />,
      )
    }

    if (canManageContracts && row.status.code === 'draft') {
      actions.push(
        <IconActionButton
          key="activate"
          icon={<PlayCircle aria-hidden="true" size={16} />}
          label={`${copy.list.statusActions.activate} ${row.property.name}`}
          onClick={() => lifecycleMutation.mutate({ action: 'activate', id: row.id })}
        />,
      )
    }

    if (canManageContracts && ['active', 'ending-soon'].includes(row.status.code)) {
      actions.push(
        <IconActionButton
          key="terminate"
          icon={<Ban aria-hidden="true" size={16} />}
          label={`${copy.list.statusActions.terminate} ${row.property.name}`}
          onClick={() => lifecycleMutation.mutate({ action: 'terminate', id: row.id })}
        />,
        <IconActionButton
          key="cancel"
          icon={<XCircle aria-hidden="true" size={16} />}
          isDestructive
          label={`${copy.list.statusActions.cancel} ${row.property.name}`}
          onClick={() => lifecycleMutation.mutate({ action: 'cancel', id: row.id })}
        />,
      )
    }

    if (canArchiveContracts) {
      actions.push(
        row.status.code === 'archived' ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${row.property.name}`}
            onClick={() => lifecycleMutation.mutate({ action: 'restore', id: row.id })}
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${row.property.name}`}
            onClick={() => lifecycleMutation.mutate({ action: 'archive', id: row.id })}
          />
        ),
      )
    }

    return actions
  }

  if (detailContractId) {
    return (
      <ContractDetailPage
        contractId={detailContractId}
        onBack={() => setDetailContractId(null)}
        onEdit={
          canWriteContracts
            ? (contract) => {
                setDetailContractId(null)
                setFormState({
                  contract: getFormInitialValue(contract) as FormState['contract'],
                  mode: 'edit',
                })
              }
            : undefined
        }
      />
    )
  }

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        title={copy.list.pageTitle}
        description={copy.list.pageDescription}
        primaryAction={
          canWriteContracts
            ? {
                icon: <FileSignature aria-hidden="true" size={18} />,
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
              updateFilters({ status: event.currentTarget.value as ContractStatus | '' })
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
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.form.property}</span>
          <select
            aria-label={copy.form.property}
            onChange={(event) => updateFilters({ propertyId: event.currentTarget.value })}
            value={filters.propertyId}
          >
            {propertyOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.form.primaryResident}</span>
          <select
            aria-label={copy.form.primaryResident}
            onChange={(event) => updateFilters({ residentId: event.currentTarget.value })}
            value={filters.residentId}
          >
            {residentOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ alignItems: 'center', display: 'flex', gap: 8, minHeight: 44 }}>
          <input
            checked={Boolean(filters.endingSoonOnly)}
            onChange={(event) => updateFilters({ endingSoonOnly: event.currentTarget.checked })}
            style={{ accentColor: 'var(--als-color-primary, #1877f2)', height: 18, width: 18 }}
            type="checkbox"
          />
          <span style={{ fontWeight: 800 }}>{copy.list.endingSoonOnly}</span>
        </label>
        <TextInput
          aria-label={copy.form.startDate}
          onChange={(event) => updateFilters({ startsFrom: event.currentTarget.value })}
          style={{ maxWidth: 150 }}
          type="date"
          value={filters.startsFrom ?? ''}
        />
        <TextInput
          aria-label={copy.form.endDate}
          onChange={(event) => updateFilters({ endsTo: event.currentTarget.value })}
          style={{ maxWidth: 150 }}
          type="date"
          value={filters.endsTo ?? ''}
        />
      </FilterBar>

      <DataTable
        columns={[
          {
            cell: (row) => (
              <div style={{ display: 'grid', gap: 4, minWidth: 0 }}>
                <strong>{row.property.name}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {row.property.location ?? '-'}
                </span>
              </div>
            ),
            header: copy.list.columns.property,
            id: 'property',
          },
          {
            cell: (row) => (
              <div style={{ display: 'grid', gap: 4, minWidth: 0 }}>
                <strong>{row.primaryResident.name}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {copy.terms.residentsCount(row.residents.length)}
                </span>
              </div>
            ),
            header: copy.list.columns.residents,
            id: 'residents',
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
            cell: (row) => formatDateRange(row, locale),
            header: copy.list.columns.dates,
            id: 'dates',
            width: 190,
          },
          {
            cell: (row) =>
              formatMoney(row.monthlyRent.amount, { currency: row.monthlyRent.currency, locale }),
            header: copy.list.columns.rent,
            id: 'rent',
            width: 150,
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
            width: 220,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          contractsQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void contractsQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={contractsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={contractsQuery.data?.totalItems ?? 0}
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
          <ContractForm
            initialValue={formState.contract}
            isSubmitting={formMutation.isPending}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onSubmit={async (values) => {
              await formMutation.mutateAsync(values)
            }}
          />
        ) : null}
      </Drawer>
    </section>
  )
}
