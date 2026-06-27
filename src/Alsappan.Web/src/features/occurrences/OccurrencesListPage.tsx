import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, Eye, Pencil, Plus, RotateCcw } from 'lucide-react'
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
import { listAdministrators } from '../../lib/api/administrators'
import type { ApiSelectOption } from '../../lib/api/contracts'
import {
  archiveOccurrence,
  createOccurrence,
  getOccurrence,
  listOccurrencePriorityOptions,
  listOccurrences,
  listOccurrenceStatusOptions,
  listOccurrenceTypeOptions,
  occurrencePriorities,
  occurrenceStatuses,
  occurrenceTypes,
  restoreOccurrence,
  updateOccurrence,
  type OccurrenceDetail,
  type OccurrenceFormRequest,
  type OccurrenceListFilters,
  type OccurrenceListItem,
  type OccurrencePriority,
  type OccurrenceStatus,
  type OccurrenceType,
} from '../../lib/api/occurrences'
import { formatDate } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { OccurrenceDetailPage } from './OccurrenceDetailPage'
import { OccurrenceForm, type OccurrenceFormMode } from './OccurrenceForm'
import { getOccurrenceCopy } from './occurrenceCopy'

const occurrenceFilterScope = 'occurrences.list'

const defaultFilters: OccurrenceListFilters = {
  includeArchived: false,
  page: 1,
  pageSize: 10,
  priority: '',
  search: '',
  status: '',
  type: '',
  unresolvedOnly: false,
}

const statusTones: Record<OccurrenceStatus, StatusBadgeTone> = {
  archived: 'archived',
  assigned: 'warning',
  cancelled: 'danger',
  'in-progress': 'warning',
  open: 'info',
  resolved: 'success',
  waiting: 'neutral',
}

const priorityTones: Record<OccurrencePriority, StatusBadgeTone> = {
  high: 'warning',
  low: 'neutral',
  medium: 'info',
  urgent: 'danger',
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
  mode: OccurrenceFormMode
  occurrence?: Partial<OccurrenceDetail | OccurrenceListItem> & Pick<OccurrenceListItem, 'id'>
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

function isOccurrenceType(value: FilterValue | undefined): value is OccurrenceType {
  return typeof value === 'string' && occurrenceTypes.includes(value as OccurrenceType)
}

function isOccurrencePriority(value: FilterValue | undefined): value is OccurrencePriority {
  return typeof value === 'string' && occurrencePriorities.includes(value as OccurrencePriority)
}

function isOccurrenceStatus(value: FilterValue | undefined): value is OccurrenceStatus {
  return typeof value === 'string' && occurrenceStatuses.includes(value as OccurrenceStatus)
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

  if (value === 'false') {
    return false
  }

  return undefined
}

function normalizeOccurrenceFilters(filters: FilterSet | undefined): OccurrenceListFilters {
  const priority = filters?.priority
  const status = filters?.status
  const type = filters?.type

  return {
    assignedUserId: getStringFilter(filters?.assignedUserId) || undefined,
    contractId: getStringFilter(filters?.contractId) || undefined,
    dateFrom: getStringFilter(filters?.dateFrom) || undefined,
    dateTo: getStringFilter(filters?.dateTo) || undefined,
    includeArchived: getBooleanFilter(filters?.includeArchived) ?? false,
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    priority: isOccurrencePriority(priority) ? priority : '',
    propertyId: getStringFilter(filters?.propertyId) || undefined,
    residentId: getStringFilter(filters?.residentId) || undefined,
    search: getStringFilter(filters?.search),
    status: isOccurrenceStatus(status) ? status : '',
    type: isOccurrenceType(type) ? type : '',
    unresolvedOnly: getBooleanFilter(filters?.unresolvedOnly) ?? false,
  }
}

function toFilterSet(filters: OccurrenceListFilters): FilterSet {
  const nextFilters: FilterSet = {
    includeArchived: filters.includeArchived ?? false,
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
    unresolvedOnly: filters.unresolvedOnly ?? false,
  }

  if (filters.assignedUserId) {
    nextFilters.assignedUserId = filters.assignedUserId
  }

  if (filters.contractId) {
    nextFilters.contractId = filters.contractId
  }

  if (filters.dateFrom) {
    nextFilters.dateFrom = filters.dateFrom
  }

  if (filters.dateTo) {
    nextFilters.dateTo = filters.dateTo
  }

  if (filters.priority) {
    nextFilters.priority = filters.priority
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

  if (filters.type) {
    nextFilters.type = filters.type
  }

  return nextFilters
}

function withEmptyOption<TValue extends string>(
  label: string,
  options: Array<ApiSelectOption<TValue>>,
) {
  return [{ label, value: '' }, ...options]
}

function occurrenceTitleForAction(row: OccurrenceListItem) {
  const context = row.property?.name ?? row.resident?.name ?? row.assignedUser?.displayName

  return context ? `${row.title} - ${context}` : row.title
}

function getFormInitialValue(occurrence: OccurrenceDetail | OccurrenceListItem) {
  return {
    assignedUser: occurrence.assignedUser,
    concurrencyToken: occurrence.concurrencyToken,
    contract: occurrence.contract,
    description: occurrence.description,
    dueDate: occurrence.dueDate,
    id: occurrence.id,
    priority: occurrence.priority,
    property: occurrence.property,
    resident: occurrence.resident,
    title: occurrence.title,
    type: occurrence.type,
  }
}

function isClosedStatus(status: OccurrenceStatus) {
  return status === 'archived' || status === 'cancelled' || status === 'resolved'
}

export function OccurrencesListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[occurrenceFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getOccurrenceCopy(locale)
  const canWriteOccurrences = hasAnyPermission(['occurrences.write'], authUser)
  const canArchiveOccurrences = hasAnyPermission(['occurrences.archive'], authUser)
  const filters = useMemo(() => normalizeOccurrenceFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.status === 'archived',
      locale,
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailOccurrenceId, setDetailOccurrenceId] = useState<string | null>(null)
  const occurrencesQuery = useQuery({
    queryFn: () => listOccurrences(apiClient, apiFilters),
    queryKey: ['occurrences', 'list', apiFilters],
  })
  const typeOptionsQuery = useQuery({
    queryFn: () => listOccurrenceTypeOptions(apiClient, locale),
    queryKey: ['occurrences', 'type-options', locale],
  })
  const priorityOptionsQuery = useQuery({
    queryFn: () => listOccurrencePriorityOptions(apiClient, locale),
    queryKey: ['occurrences', 'priority-options', locale],
  })
  const statusOptionsQuery = useQuery({
    queryFn: () => listOccurrenceStatusOptions(apiClient, locale),
    queryKey: ['occurrences', 'status-options', locale],
  })
  const administratorsQuery = useQuery({
    queryFn: () => listAdministrators(apiClient, { pageSize: 100, status: 'active' }),
    queryKey: ['administrators', 'occurrence-list-assignees'],
  })
  const rows = occurrencesQuery.data?.items ?? []
  const page = occurrencesQuery.data?.page ?? filters.page ?? 1
  const pageSize = occurrencesQuery.data?.pageSize ?? filters.pageSize ?? 10
  const typeOptions =
    typeOptionsQuery.data ??
    occurrenceTypes.map((type) => ({ label: copy.terms.types[type], value: type }))
  const priorityOptions =
    priorityOptionsQuery.data ??
    occurrencePriorities.map((priority) => ({
      label: copy.terms.priorities[priority],
      value: priority,
    }))
  const statusOptions =
    statusOptionsQuery.data ??
    occurrenceStatuses.map((status) => ({
      label: copy.list.status[status],
      value: status,
    }))
  const assigneeOptions =
    administratorsQuery.data?.items.map((administrator) => ({
      label: administrator.displayName,
      value: administrator.id,
    })) ?? []
  const activeFilterCount = [
    filters.assignedUserId,
    filters.contractId,
    filters.dateFrom,
    filters.dateTo,
    filters.priority,
    filters.propertyId,
    filters.residentId,
    filters.search,
    filters.status,
    filters.type,
    filters.includeArchived ? 'true' : '',
    filters.unresolvedOnly ? 'true' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const formMutation = useMutation({
    mutationFn: (values: OccurrenceFormRequest) => {
      if (formState?.mode === 'edit' && formState.occurrence) {
        return updateOccurrence(apiClient, formState.occurrence.id, values, locale)
      }

      return createOccurrence(apiClient, values, locale)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: { action: 'archive' | 'restore'; occurrenceId: string }) => {
      if (request.action === 'archive') {
        await archiveOccurrence(apiClient, request.occurrenceId)
        return
      }

      await restoreOccurrence(apiClient, request.occurrenceId, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    },
  })
  const editOccurrenceMutation = useMutation({
    mutationFn: (occurrenceId: string) => getOccurrence(apiClient, occurrenceId, locale),
    onSuccess: (occurrence) => {
      setFormState({
        mode: 'edit',
        occurrence: getFormInitialValue(occurrence),
      })
    },
  })

  function updateFilters(nextFilters: Partial<OccurrenceListFilters>) {
    setStoredFilters(
      occurrenceFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      occurrenceFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: OccurrenceListItem) {
    const rowTitle = occurrenceTitleForAction(row)
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${rowTitle}`}
        onClick={() => setDetailOccurrenceId(row.id)}
      />,
    ]

    if (canWriteOccurrences && !isClosedStatus(row.status.code)) {
      actions.push(
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${rowTitle}`}
          onClick={() => editOccurrenceMutation.mutate(row.id)}
        />,
      )
    }

    if (canArchiveOccurrences) {
      actions.push(
        row.isArchived || row.status.code === 'archived' ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'restore', occurrenceId: row.id })}
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'archive', occurrenceId: row.id })}
          />
        ),
      )
    }

    return actions
  }

  if (detailOccurrenceId) {
    return (
      <OccurrenceDetailPage
        occurrenceId={detailOccurrenceId}
        onBack={() => setDetailOccurrenceId(null)}
        onEdit={
          canWriteOccurrences
            ? (occurrence) => {
                setDetailOccurrenceId(null)
                setFormState({
                  mode: 'edit',
                  occurrence: getFormInitialValue(occurrence),
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
          canWriteOccurrences
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
              updateFilters({ type: event.currentTarget.value as OccurrenceType | '' })
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
          <span style={{ fontWeight: 800 }}>{copy.list.columns.priority}</span>
          <select
            aria-label={copy.list.columns.priority}
            onChange={(event) =>
              updateFilters({ priority: event.currentTarget.value as OccurrencePriority | '' })
            }
            value={filters.priority}
          >
            {withEmptyOption(copy.list.allPriorities, priorityOptions).map((option) => (
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
              updateFilters({ status: event.currentTarget.value as OccurrenceStatus | '' })
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
          <span style={{ fontWeight: 800 }}>{copy.list.columns.assignee}</span>
          <select
            aria-label={copy.list.columns.assignee}
            onChange={(event) =>
              updateFilters({ assignedUserId: event.currentTarget.value || undefined })
            }
            value={filters.assignedUserId ?? ''}
          >
            {withEmptyOption(copy.list.allAssignees, assigneeOptions).map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <TextInput
          aria-label={copy.list.propertyId}
          onChange={(event) => updateFilters({ propertyId: event.currentTarget.value })}
          placeholder={copy.list.propertyId}
          style={{ maxWidth: 210 }}
          value={filters.propertyId ?? ''}
        />
        <TextInput
          aria-label={copy.list.residentId}
          onChange={(event) => updateFilters({ residentId: event.currentTarget.value })}
          placeholder={copy.list.residentId}
          style={{ maxWidth: 210 }}
          value={filters.residentId ?? ''}
        />
        <TextInput
          aria-label={copy.list.contractId}
          onChange={(event) => updateFilters({ contractId: event.currentTarget.value })}
          placeholder={copy.list.contractId}
          style={{ maxWidth: 210 }}
          value={filters.contractId ?? ''}
        />
        <TextInput
          aria-label="dateFrom"
          onChange={(event) => updateFilters({ dateFrom: event.currentTarget.value })}
          style={{ maxWidth: 160 }}
          type="date"
          value={filters.dateFrom ?? ''}
        />
        <TextInput
          aria-label="dateTo"
          onChange={(event) => updateFilters({ dateTo: event.currentTarget.value })}
          style={{ maxWidth: 160 }}
          type="date"
          value={filters.dateTo ?? ''}
        />
        <label style={{ alignItems: 'center', display: 'flex', gap: 8, minHeight: 44 }}>
          <input
            checked={Boolean(filters.unresolvedOnly)}
            onChange={(event) => updateFilters({ unresolvedOnly: event.currentTarget.checked })}
            style={{ accentColor: 'var(--als-color-primary, #1877f2)', height: 18, width: 18 }}
            type="checkbox"
          />
          <span style={{ fontWeight: 800 }}>{copy.list.unresolvedOnly}</span>
        </label>
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
                  {row.description}
                </span>
              </div>
            ),
            header: copy.list.columns.title,
            id: 'title',
          },
          {
            cell: (row) => row.type.label,
            header: copy.list.columns.type,
            id: 'type',
            width: 130,
          },
          {
            cell: (row) => (
              <StatusBadge label={row.priority.label} tone={priorityTones[row.priority.code]} />
            ),
            header: copy.list.columns.priority,
            id: 'priority',
            width: 130,
          },
          {
            cell: (row) => (
              <StatusBadge label={row.status.label} tone={statusTones[row.status.code]} />
            ),
            header: copy.list.columns.status,
            id: 'status',
            width: 140,
          },
          {
            cell: (row) => row.assignedUser?.displayName ?? copy.terms.none,
            header: copy.list.columns.assignee,
            id: 'assignee',
            width: 170,
          },
          {
            cell: (row) => row.property?.name ?? row.resident?.name ?? copy.terms.none,
            header: copy.list.columns.property,
            id: 'property',
            width: 190,
          },
          {
            cell: (row) => (row.dueDate ? formatDate(row.dueDate, { locale }) : copy.terms.none),
            header: copy.list.columns.dueDate,
            id: 'dueDate',
            width: 130,
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
            width: 190,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          occurrencesQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void occurrencesQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={occurrencesQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={occurrencesQuery.data?.totalItems ?? 0}
      />

      <Drawer
        isOpen={Boolean(formState)}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setFormState(null)
          }
        }}
        title={formState ? copy.form.modeTitle[formState.mode] : copy.form.modeTitle.create}
        width={720}
      >
        {formState ? (
          <OccurrenceForm
            initialValue={formState.occurrence}
            isSubmitting={formMutation.isPending}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onSubmit={async (values) => {
              await formMutation.mutateAsync(values)
            }}
            priorityOptions={priorityOptions}
            typeOptions={typeOptions}
          />
        ) : null}
      </Drawer>
    </section>
  )
}
