import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, Eye, Pencil, RotateCcw, UserPlus } from 'lucide-react'
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
  type StatusBadgeTone,
} from '../../components'
import {
  archiveResident,
  checkResidentDuplicateWarnings,
  createResident,
  listResidentPortalStatusOptions,
  listResidents,
  listResidentStatusOptions,
  residentPortalStatuses,
  residentStatuses,
  restoreResident,
  updateResident,
  type ResidentDetail,
  type ResidentFormRequest,
  type ResidentListFilters,
  type ResidentListItem,
  type ResidentPortalStatus,
  type ResidentStatus,
  type ResidentStatusLabel,
} from '../../lib/api/residents'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { useListRouteState, type RouteFilterDefinition } from '../../lib/routing/useListRouteState'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { ResidentDetailPage } from './ResidentDetailPage'
import { ResidentForm, type ResidentFormMode } from './ResidentForm'
import { getResidentCopy } from './residentCopy'

const residentFilterScope = 'residents.list'
const residentDetailRouteParams = ['id', 'residentId'] as const

const residentRouteFilters = [
  { filterKey: 'search', params: ['search'] },
  { filterKey: 'status', params: ['status'] },
  { filterKey: 'portalStatus', params: ['portalStatus'] },
  { filterKey: 'hasPortalAccess', params: ['hasPortalAccess'], type: 'boolean' },
  { filterKey: 'includeArchived', params: ['includeArchived'], type: 'boolean' },
] as const satisfies readonly RouteFilterDefinition[]

const defaultFilters: ResidentListFilters = {
  hasPortalAccess: '',
  includeArchived: false,
  page: 1,
  pageSize: 10,
  portalStatus: '',
  search: '',
  status: '',
}

const statusTones: Record<ResidentStatus, StatusBadgeTone> = {
  active: 'success',
  archived: 'archived',
  inactive: 'neutral',
}

const portalStatusTones: Record<ResidentPortalStatus, StatusBadgeTone> = {
  active: 'success',
  disabled: 'danger',
  invited: 'info',
  'not-invited': 'neutral',
}

const allowedTones: StatusBadgeTone[] = [
  'archived',
  'danger',
  'info',
  'neutral',
  'success',
  'warning',
]

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
  mode: ResidentFormMode
  resident?: ResidentDetail | ResidentListItem
}

type IconActionButtonProps = {
  icon: ReactNode
  isDestructive?: boolean
  label: string
  onClick: () => void
}

function isResidentStatus(value: FilterValue | undefined): value is ResidentStatus {
  return typeof value === 'string' && residentStatuses.includes(value as ResidentStatus)
}

function isResidentPortalStatus(value: FilterValue | undefined): value is ResidentPortalStatus {
  return typeof value === 'string' && residentPortalStatuses.includes(value as ResidentPortalStatus)
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

  return ''
}

function normalizeResidentFilters(filters: FilterSet | undefined): ResidentListFilters {
  const status = filters?.status
  const portalStatus = filters?.portalStatus

  return {
    hasPortalAccess: getBooleanFilter(filters?.hasPortalAccess),
    includeArchived: filters?.includeArchived === true,
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    portalStatus: isResidentPortalStatus(portalStatus) ? portalStatus : '',
    search: getStringFilter(filters?.search),
    status: isResidentStatus(status) ? status : '',
  }
}

function toFilterSet(filters: ResidentListFilters): FilterSet {
  const nextFilters: FilterSet = {
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  if (filters.hasPortalAccess !== undefined && filters.hasPortalAccess !== '') {
    nextFilters.hasPortalAccess = filters.hasPortalAccess
  }

  if (filters.includeArchived) {
    nextFilters.includeArchived = true
  }

  if (filters.portalStatus) {
    nextFilters.portalStatus = filters.portalStatus
  }

  if (filters.search) {
    nextFilters.search = filters.search
  }

  if (filters.status) {
    nextFilters.status = filters.status
  }

  return nextFilters
}

function getMutationKey(filters: ResidentListFilters) {
  return ['residents', 'list', filters] as const
}

function coerceTone(tone: string | undefined, fallback: StatusBadgeTone) {
  return allowedTones.includes(tone as StatusBadgeTone) ? (tone as StatusBadgeTone) : fallback
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

function getDocumentLabel(row: ResidentListItem, copy: ReturnType<typeof getResidentCopy>) {
  if (row.isSensitiveMasked) {
    return copy.list.masked
  }

  const document = [row.documentType, row.documentIdentifier].filter(Boolean).join(' ')

  return document || copy.list.noDocument
}

function getContactLabel(row: ResidentListItem, copy: ReturnType<typeof getResidentCopy>) {
  if (row.isSensitiveMasked) {
    return row.contactSummary || copy.list.maskedContact
  }

  return row.contactSummary || [row.email, row.phone].filter(Boolean).join(' | ') || '-'
}

function getFormInitialValue(resident: ResidentDetail | ResidentListItem): ResidentFormRequest {
  return {
    birthDate: resident.birthDate,
    documentIdentifier: resident.documentIdentifier,
    documentType: resident.documentType,
    email: resident.email,
    emergencyContact: resident.emergencyContact,
    fullName: resident.fullName,
    notes: resident.notes,
    phone: resident.phone,
    portalStatus: resident.portalStatus.code,
    preferredName: resident.preferredName,
    privacyFlags: resident.privacyFlags,
    secondaryPhone: resident.secondaryPhone,
    status: resident.status.code,
  }
}

function getStatusOptions(
  apiOptions: Array<ResidentStatusLabel<ResidentStatus>> | undefined,
  copy: ReturnType<typeof getResidentCopy>,
) {
  const options =
    apiOptions && apiOptions.length > 0
      ? apiOptions
      : residentStatuses.map((status) => ({
          code: status,
          label: copy.list.status[status],
        }))

  return [
    { label: copy.list.allStatuses, value: '' },
    ...options.map(({ code, label }) => ({ label, value: code })),
  ]
}

function getPortalStatusOptions(
  apiOptions: Array<ResidentStatusLabel<ResidentPortalStatus>> | undefined,
  copy: ReturnType<typeof getResidentCopy>,
) {
  const options =
    apiOptions && apiOptions.length > 0
      ? apiOptions
      : residentPortalStatuses.map((portalStatus) => ({
          code: portalStatus,
          label: copy.list.portalStatus[portalStatus],
        }))

  return [
    { label: copy.list.allPortalStatuses, value: '' },
    ...options.map(({ code, label }) => ({ label, value: code })),
  ]
}

export function ResidentsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[residentFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getResidentCopy(locale)
  const canWriteResidents = hasAnyPermission(['residents.write'], authUser)
  const filters = useMemo(() => normalizeResidentFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.status === 'archived',
      locale,
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailResidentId, setDetailResidentId] = useState<string | null>(null)
  useListRouteState({
    detailParams: residentDetailRouteParams,
    onDetailIdChange: setDetailResidentId,
    pageSize: defaultFilters.pageSize ?? 10,
    routeFilters: residentRouteFilters,
    scope: residentFilterScope,
    setFilters: setStoredFilters,
  })
  const residentsQuery = useQuery({
    queryFn: () => listResidents(apiClient, apiFilters),
    queryKey: getMutationKey(apiFilters),
  })
  const statusOptionsQuery = useQuery({
    queryFn: () => listResidentStatusOptions(apiClient, locale),
    queryKey: ['residents', 'status-options', locale],
  })
  const portalStatusOptionsQuery = useQuery({
    queryFn: () => listResidentPortalStatusOptions(apiClient, locale),
    queryKey: ['residents', 'portal-status-options', locale],
  })
  const activeFilterCount = [
    filters.search,
    filters.status,
    filters.portalStatus,
    filters.hasPortalAccess === '' ? '' : String(filters.hasPortalAccess),
  ].filter((value) => value !== undefined && value !== '').length
  const page = residentsQuery.data?.page ?? filters.page ?? 1
  const pageSize = residentsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const rows = residentsQuery.data?.items ?? []
  const formInitialValue = formState?.resident ? getFormInitialValue(formState.resident) : undefined
  const statusOptions = useMemo(
    () => getStatusOptions(statusOptionsQuery.data, copy),
    [copy, statusOptionsQuery.data],
  )
  const portalStatusOptions = useMemo(
    () => getPortalStatusOptions(portalStatusOptionsQuery.data, copy),
    [copy, portalStatusOptionsQuery.data],
  )
  const portalAccessOptions = useMemo(
    () => [
      { label: copy.list.allPortalAccess, value: '' },
      { label: copy.list.portalAccessYes, value: 'true' },
      { label: copy.list.portalAccessNo, value: 'false' },
    ],
    [copy],
  )
  const formMutation = useMutation({
    mutationFn: (values: ResidentFormRequest) => {
      if (formState?.mode === 'edit' && formState.resident) {
        return updateResident(apiClient, formState.resident.id, values)
      }

      return createResident(apiClient, values)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['residents'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: { action: 'archive' | 'restore'; id: string }) => {
      if (request.action === 'archive') {
        await archiveResident(apiClient, request.id)
        return
      }

      await restoreResident(apiClient, request.id)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['residents'] })
    },
  })

  function updateFilters(nextFilters: Partial<ResidentListFilters>) {
    setStoredFilters(
      residentFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      residentFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: ResidentListItem) {
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${row.fullName}`}
        onClick={() => setDetailResidentId(row.id)}
      />,
    ]

    if (!canWriteResidents) {
      return actions
    }

    actions.push(
      <IconActionButton
        key="edit"
        icon={<Pencil aria-hidden="true" size={16} />}
        label={`${copy.list.edit} ${row.fullName}`}
        onClick={() => setFormState({ mode: 'edit', resident: row })}
      />,
    )

    actions.push(
      row.isArchived || row.status.code === 'archived' ? (
        <IconActionButton
          key="restore"
          icon={<RotateCcw aria-hidden="true" size={16} />}
          label={`${copy.list.restore} ${row.fullName}`}
          onClick={() => lifecycleMutation.mutate({ action: 'restore', id: row.id })}
        />
      ) : (
        <IconActionButton
          key="archive"
          icon={<Archive aria-hidden="true" size={16} />}
          isDestructive
          label={`${copy.list.archive} ${row.fullName}`}
          onClick={() => lifecycleMutation.mutate({ action: 'archive', id: row.id })}
        />
      ),
    )

    return actions
  }

  if (detailResidentId) {
    return (
      <ResidentDetailPage
        residentId={detailResidentId}
        onBack={() => setDetailResidentId(null)}
        onEdit={
          canWriteResidents
            ? (resident) => {
                setDetailResidentId(null)
                setFormState({ mode: 'edit', resident })
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
          canWriteResidents
            ? {
                icon: <UserPlus aria-hidden="true" size={18} />,
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
              updateFilters({ status: event.currentTarget.value as ResidentStatus | '' })
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
          <span style={{ fontWeight: 800 }}>{copy.list.columns.portalStatus}</span>
          <select
            aria-label={copy.list.columns.portalStatus}
            onChange={(event) =>
              updateFilters({
                portalStatus: event.currentTarget.value as ResidentPortalStatus | '',
              })
            }
            value={filters.portalStatus}
          >
            {portalStatusOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.portalAccess}</span>
          <select
            aria-label={copy.list.portalAccess}
            onChange={(event) =>
              updateFilters({
                hasPortalAccess:
                  event.currentTarget.value === '' ? '' : event.currentTarget.value === 'true',
              })
            }
            value={filters.hasPortalAccess === '' ? '' : String(filters.hasPortalAccess)}
          >
            {portalAccessOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </FilterBar>

      <DataTable
        columns={[
          {
            cell: (row) => (
              <div style={{ display: 'grid', gap: 4, minWidth: 0 }}>
                <strong>{row.fullName}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {row.preferredName ?? row.id}
                </span>
              </div>
            ),
            header: copy.list.columns.resident,
            id: 'resident',
          },
          {
            cell: (row) => getContactLabel(row, copy),
            header: copy.list.columns.contact,
            id: 'contact',
          },
          {
            cell: (row) => (
              <StatusBadge
                label={row.status.label}
                tone={coerceTone(row.status.tone, statusTones[row.status.code])}
              />
            ),
            header: copy.list.columns.status,
            id: 'status',
            width: 140,
          },
          {
            cell: (row) => (
              <StatusBadge
                label={row.portalStatus.label}
                tone={coerceTone(row.portalStatus.tone, portalStatusTones[row.portalStatus.code])}
              />
            ),
            header: copy.list.columns.portalStatus,
            id: 'portalStatus',
            width: 160,
          },
          {
            cell: (row) => getDocumentLabel(row, copy),
            header: copy.list.columns.document,
            id: 'document',
            width: 160,
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
            width: 148,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          residentsQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void residentsQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={residentsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={residentsQuery.data?.totalItems ?? 0}
      />

      <Drawer
        isOpen={Boolean(formState)}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setFormState(null)
          }
        }}
        title={formState ? copy.form.modeTitle[formState.mode] : copy.form.modeTitle.create}
        width={620}
      >
        {formState ? (
          <ResidentForm
            initialValue={formInitialValue}
            isSubmitting={formMutation.isPending}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onCheckDuplicates={(request) => checkResidentDuplicateWarnings(apiClient, request)}
            onSubmit={async (values) => {
              await formMutation.mutateAsync(values)
            }}
            residentId={formState.resident?.id}
          />
        ) : null}
      </Drawer>
    </section>
  )
}
