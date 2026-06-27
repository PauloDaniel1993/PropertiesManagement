import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, Ban, CheckCircle2, Eye, Pencil, Play, Plus, RotateCcw } from 'lucide-react'
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
import { formatDateTime } from '../../lib/format'
import {
  archiveInspection,
  cancelInspection,
  completeInspection,
  getInspection,
  inspectionStatuses,
  inspectionTypes,
  listInspections,
  listInspectionStatusOptions,
  listInspectionTypeOptions,
  restoreInspection,
  scheduleInspection,
  startInspection,
  updateInspection,
  type InspectionDetail,
  type InspectionFormRequest,
  type InspectionListFilters,
  type InspectionListItem,
  type InspectionStatus,
  type InspectionType,
} from '../../lib/api/inspections'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { InspectionDetailPage } from './InspectionDetailPage'
import { InspectionForm, type InspectionFormMode } from './InspectionForm'
import { getInspectionCopy } from './inspectionCopy'

const inspectionFilterScope = 'inspections.list'

const defaultFilters: InspectionListFilters = {
  includeArchived: false,
  page: 1,
  pageSize: 10,
  pendingOnly: false,
  search: '',
  status: '',
  type: '',
}

const statusTones: Record<InspectionStatus, StatusBadgeTone> = {
  archived: 'archived',
  cancelled: 'archived',
  completed: 'success',
  'in-progress': 'info',
  scheduled: 'warning',
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
  inspection?: Partial<InspectionDetail | InspectionListItem> & Pick<InspectionListItem, 'id'>
  mode: InspectionFormMode
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

function isInspectionType(value: FilterValue | undefined): value is InspectionType {
  return typeof value === 'string' && inspectionTypes.includes(value as InspectionType)
}

function isInspectionStatus(value: FilterValue | undefined): value is InspectionStatus {
  return typeof value === 'string' && inspectionStatuses.includes(value as InspectionStatus)
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

function normalizeInspectionFilters(filters: FilterSet | undefined): InspectionListFilters {
  const status = filters?.status
  const type = filters?.type

  return {
    assignedUserId: getStringFilter(filters?.assignedUserId) || undefined,
    contractId: getStringFilter(filters?.contractId) || undefined,
    includeArchived: getBooleanFilter(filters?.includeArchived) ?? false,
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    pendingOnly: getBooleanFilter(filters?.pendingOnly) ?? false,
    propertyId: getStringFilter(filters?.propertyId) || undefined,
    residentId: getStringFilter(filters?.residentId) || undefined,
    scheduledFrom: getStringFilter(filters?.scheduledFrom) || undefined,
    scheduledTo: getStringFilter(filters?.scheduledTo) || undefined,
    search: getStringFilter(filters?.search),
    status: isInspectionStatus(status) ? status : '',
    type: isInspectionType(type) ? type : '',
  }
}

function toFilterSet(filters: InspectionListFilters): FilterSet {
  const nextFilters: FilterSet = {
    includeArchived: filters.includeArchived ?? false,
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
    pendingOnly: filters.pendingOnly ?? false,
  }

  if (filters.assignedUserId) {
    nextFilters.assignedUserId = filters.assignedUserId
  }

  if (filters.contractId) {
    nextFilters.contractId = filters.contractId
  }

  if (filters.propertyId) {
    nextFilters.propertyId = filters.propertyId
  }

  if (filters.residentId) {
    nextFilters.residentId = filters.residentId
  }

  if (filters.scheduledFrom) {
    nextFilters.scheduledFrom = filters.scheduledFrom
  }

  if (filters.scheduledTo) {
    nextFilters.scheduledTo = filters.scheduledTo
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

function toDateRangeStart(value?: string) {
  return value ? new Date(`${value}T00:00:00`).toISOString() : undefined
}

function toDateRangeEnd(value?: string) {
  return value ? new Date(`${value}T23:59:59`).toISOString() : undefined
}

function getFormInitialValue(
  inspection: InspectionDetail | InspectionListItem,
): Partial<InspectionDetail> {
  return {
    assignee: inspection.assignee,
    concurrencyToken: inspection.concurrencyToken,
    contract: inspection.contract,
    id: inspection.id,
    notes: 'notes' in inspection ? inspection.notes : undefined,
    property: inspection.property,
    resident: inspection.resident,
    scheduledAt: inspection.scheduledAt,
    signatureSlots: 'signatureSlots' in inspection ? inspection.signatureSlots : undefined,
    title: inspection.title,
    type: inspection.type,
  }
}

function inspectionTitleForAction(row: InspectionListItem) {
  return row.property?.name ? `${row.title} - ${row.property.name}` : row.title
}

function progressLabel(row: InspectionListItem, copy: ReturnType<typeof getInspectionCopy>) {
  return copy.terms.progress(
    row.progress.completedItems,
    row.progress.totalItems,
    row.progress.percentage,
  )
}

export function InspectionsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[inspectionFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getInspectionCopy(locale)
  const canWriteInspections = hasAnyPermission(['inspections.write'], authUser)
  const canManageInspections = hasAnyPermission(['inspections.manage'], authUser)
  const canArchiveInspections = hasAnyPermission(['inspections.archive'], authUser)
  const filters = useMemo(() => normalizeInspectionFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.status === 'archived',
      locale,
      scheduledFrom: toDateRangeStart(filters.scheduledFrom),
      scheduledTo: toDateRangeEnd(filters.scheduledTo),
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailInspectionId, setDetailInspectionId] = useState<string | null>(null)
  const inspectionsQuery = useQuery({
    queryFn: () => listInspections(apiClient, apiFilters),
    queryKey: ['inspections', 'list', apiFilters],
  })
  const typeOptionsQuery = useQuery({
    queryFn: () => listInspectionTypeOptions(apiClient, locale),
    queryKey: ['inspections', 'type-options', locale],
  })
  const statusOptionsQuery = useQuery({
    queryFn: () => listInspectionStatusOptions(apiClient, locale),
    queryKey: ['inspections', 'status-options', locale],
  })
  const rows = inspectionsQuery.data?.items ?? []
  const page = inspectionsQuery.data?.page ?? filters.page ?? 1
  const pageSize = inspectionsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const typeOptions =
    typeOptionsQuery.data ??
    inspectionTypes.map((type) => ({ label: copy.terms.types[type], value: type }))
  const statusOptions =
    statusOptionsQuery.data ??
    inspectionStatuses.map((status) => ({
      label: copy.list.status[status],
      value: status,
    }))
  const activeFilterCount = [
    filters.search,
    filters.type,
    filters.status,
    filters.propertyId,
    filters.contractId,
    filters.residentId,
    filters.assignedUserId,
    filters.scheduledFrom,
    filters.scheduledTo,
    filters.pendingOnly ? 'true' : '',
    filters.includeArchived ? 'true' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const formMutation = useMutation({
    mutationFn: (values: InspectionFormRequest) => {
      if (formState?.mode === 'edit' && formState.inspection) {
        return updateInspection(apiClient, formState.inspection.id, values, locale)
      }

      return scheduleInspection(apiClient, values, locale)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['inspections'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: {
      action: 'archive' | 'cancel' | 'complete' | 'restore' | 'start'
      inspectionId: string
    }) => {
      if (request.action === 'archive') {
        await archiveInspection(apiClient, request.inspectionId)
        return
      }

      if (request.action === 'cancel') {
        await cancelInspection(apiClient, request.inspectionId, undefined, locale)
        return
      }

      if (request.action === 'complete') {
        await completeInspection(apiClient, request.inspectionId, undefined, locale)
        return
      }

      if (request.action === 'start') {
        await startInspection(apiClient, request.inspectionId, locale)
        return
      }

      await restoreInspection(apiClient, request.inspectionId, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['inspections'] })
    },
  })
  const editInspectionMutation = useMutation({
    mutationFn: (inspectionId: string) => getInspection(apiClient, inspectionId, locale),
    onSuccess: (inspection) => {
      setFormState({
        inspection: getFormInitialValue(inspection) as FormState['inspection'],
        mode: 'edit',
      })
    },
  })

  function updateFilters(nextFilters: Partial<InspectionListFilters>) {
    setStoredFilters(
      inspectionFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      inspectionFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: InspectionListItem) {
    const rowTitle = inspectionTitleForAction(row)
    const canMutate = !['archived', 'cancelled', 'completed'].includes(row.status.code)
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${rowTitle}`}
        onClick={() => setDetailInspectionId(row.id)}
      />,
    ]

    if (canWriteInspections && row.status.code === 'scheduled') {
      actions.push(
        <IconActionButton
          key="start"
          icon={<Play aria-hidden="true" size={16} />}
          label={`${copy.list.start} ${rowTitle}`}
          onClick={() => lifecycleMutation.mutate({ action: 'start', inspectionId: row.id })}
        />,
      )
    }

    if (canManageInspections && row.status.code === 'in-progress') {
      actions.push(
        <IconActionButton
          key="complete"
          icon={<CheckCircle2 aria-hidden="true" size={16} />}
          label={`${copy.list.complete} ${rowTitle}`}
          onClick={() => lifecycleMutation.mutate({ action: 'complete', inspectionId: row.id })}
        />,
      )
    }

    if (canManageInspections && canMutate) {
      actions.push(
        <IconActionButton
          key="cancel"
          icon={<Ban aria-hidden="true" size={16} />}
          isDestructive
          label={`${copy.list.cancel} ${rowTitle}`}
          onClick={() => lifecycleMutation.mutate({ action: 'cancel', inspectionId: row.id })}
        />,
      )
    }

    if (canWriteInspections && canMutate) {
      actions.push(
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${rowTitle}`}
          onClick={() => editInspectionMutation.mutate(row.id)}
        />,
      )
    }

    if (canArchiveInspections) {
      actions.push(
        row.status.code === 'archived' || row.isArchived ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'restore', inspectionId: row.id })}
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'archive', inspectionId: row.id })}
          />
        ),
      )
    }

    return actions
  }

  if (detailInspectionId) {
    return (
      <InspectionDetailPage
        inspectionId={detailInspectionId}
        onBack={() => setDetailInspectionId(null)}
        onCancel={
          canManageInspections
            ? (inspection) =>
                lifecycleMutation.mutate({ action: 'cancel', inspectionId: inspection.id })
            : undefined
        }
        onComplete={
          canManageInspections
            ? (inspection) =>
                lifecycleMutation.mutate({ action: 'complete', inspectionId: inspection.id })
            : undefined
        }
        onEdit={
          canWriteInspections
            ? (inspection) => {
                setDetailInspectionId(null)
                setFormState({
                  inspection: getFormInitialValue(inspection) as FormState['inspection'],
                  mode: 'edit',
                })
              }
            : undefined
        }
        onStart={
          canWriteInspections
            ? (inspection) =>
                lifecycleMutation.mutate({ action: 'start', inspectionId: inspection.id })
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
          canWriteInspections
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
              updateFilters({ type: event.currentTarget.value as InspectionType | '' })
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
              updateFilters({ status: event.currentTarget.value as InspectionStatus | '' })
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
        <TextInput
          aria-label={copy.list.columns.assignee}
          onChange={(event) => updateFilters({ assignedUserId: event.currentTarget.value })}
          placeholder={copy.list.columns.assignee}
          style={{ maxWidth: 210 }}
          value={filters.assignedUserId ?? ''}
        />
        <TextInput
          aria-label={copy.list.scheduledFrom}
          onChange={(event) => updateFilters({ scheduledFrom: event.currentTarget.value })}
          placeholder={copy.list.scheduledFrom}
          style={{ maxWidth: 160 }}
          type="date"
          value={filters.scheduledFrom ?? ''}
        />
        <TextInput
          aria-label={copy.list.scheduledTo}
          onChange={(event) => updateFilters({ scheduledTo: event.currentTarget.value })}
          placeholder={copy.list.scheduledTo}
          style={{ maxWidth: 160 }}
          type="date"
          value={filters.scheduledTo ?? ''}
        />
        <label style={{ alignItems: 'center', display: 'flex', gap: 8, minHeight: 44 }}>
          <input
            checked={Boolean(filters.pendingOnly)}
            onChange={(event) => updateFilters({ pendingOnly: event.currentTarget.checked })}
            style={{ accentColor: 'var(--als-color-primary, #1877f2)', height: 18, width: 18 }}
            type="checkbox"
          />
          <span style={{ fontWeight: 800 }}>{copy.list.pendingOnly}</span>
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
                  {row.type.label}
                </span>
              </div>
            ),
            header: copy.list.columns.title,
            id: 'inspection',
          },
          {
            cell: (row) => formatDateTime(row.scheduledAt, { locale }),
            header: copy.list.columns.date,
            id: 'scheduledAt',
            width: 150,
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
            cell: (row) => row.assignee.name,
            header: copy.list.columns.assignee,
            id: 'assignee',
            width: 170,
          },
          {
            cell: (row) => row.property.name,
            header: copy.list.columns.property,
            id: 'property',
            width: 190,
          },
          {
            cell: (row) => progressLabel(row, copy),
            header: copy.list.columns.progress,
            id: 'progress',
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
            width: 300,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          inspectionsQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void inspectionsQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={inspectionsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={inspectionsQuery.data?.totalItems ?? 0}
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
          <InspectionForm
            initialValue={formState.inspection}
            isSubmitting={formMutation.isPending}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onSubmit={async (values) => {
              await formMutation.mutateAsync(values)
            }}
            typeOptions={typeOptions}
          />
        ) : null}
      </Drawer>
    </section>
  )
}
