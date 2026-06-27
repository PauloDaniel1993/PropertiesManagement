import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Archive,
  CheckCircle2,
  Eye,
  Home,
  Pencil,
  RotateCcw,
  Wrench,
  type LucideIcon,
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
  CheckboxInput,
  TextInput,
  type StatusBadgeTone,
} from '../../components'
import {
  archiveProperty,
  createProperty,
  listProperties,
  propertyStatuses,
  propertyTypes,
  restoreProperty,
  updateProperty,
  updatePropertyStatus,
  type PropertyDetail,
  type PropertyFormRequest,
  type PropertyListFilters,
  type PropertyListItem,
  type PropertyStatus,
  type PropertyType,
} from '../../lib/api/properties'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatMoney } from '../../lib/format'
import { useListRouteState, type RouteFilterDefinition } from '../../lib/routing/useListRouteState'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { PropertyDetailPage } from './PropertyDetailPage'
import { PropertyForm, type PropertyFormMode } from './PropertyForm'
import { getPropertyCopy } from './propertyCopy'

const propertyFilterScope = 'properties.list'
const propertyDetailRouteParams = ['id', 'propertyId'] as const

const propertyRouteFilters = [
  { filterKey: 'search', params: ['search'] },
  { filterKey: 'status', params: ['status'] },
  { filterKey: 'type', params: ['type'] },
  { filterKey: 'hasGarage', params: ['hasGarage'], type: 'boolean' },
  { filterKey: 'includeArchived', params: ['includeArchived'], type: 'boolean' },
  { filterKey: 'minRent', params: ['minRent'], type: 'number' },
  { filterKey: 'maxRent', params: ['maxRent'], type: 'number' },
] as const satisfies readonly RouteFilterDefinition[]

const defaultFilters: PropertyListFilters = {
  hasGarage: '',
  includeArchived: false,
  page: 1,
  pageSize: 10,
  search: '',
  status: '',
  type: '',
}

const statusTones: Record<PropertyStatus, StatusBadgeTone> = {
  archived: 'archived',
  available: 'success',
  inactive: 'neutral',
  maintenance: 'warning',
  rented: 'info',
  reserved: 'warning',
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
  mode: PropertyFormMode
  property?: PropertyDetail | PropertyListItem
}

type IconActionButtonProps = {
  icon: ReactNode
  isDestructive?: boolean
  label: string
  onClick: () => void
}

function isPropertyStatus(value: FilterValue | undefined): value is PropertyStatus {
  return typeof value === 'string' && propertyStatuses.includes(value as PropertyStatus)
}

function isPropertyType(value: FilterValue | undefined): value is PropertyType {
  return typeof value === 'string' && propertyTypes.includes(value as PropertyType)
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

function getGarageFilter(value: FilterValue | undefined) {
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

function normalizePropertyFilters(filters: FilterSet | undefined): PropertyListFilters {
  const status = filters?.status
  const type = filters?.type

  return {
    hasGarage: getGarageFilter(filters?.hasGarage),
    includeArchived: filters?.includeArchived === true,
    maxRent: getNumberFilter(filters?.maxRent),
    minRent: getNumberFilter(filters?.minRent),
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    search: getStringFilter(filters?.search),
    status: isPropertyStatus(status) ? status : '',
    type: isPropertyType(type) ? type : '',
  }
}

function toFilterSet(filters: PropertyListFilters): FilterSet {
  const nextFilters: FilterSet = {
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  if (filters.hasGarage !== undefined && filters.hasGarage !== '') {
    nextFilters.hasGarage = filters.hasGarage
  }

  if (filters.includeArchived) {
    nextFilters.includeArchived = true
  }

  if (filters.maxRent !== undefined) {
    nextFilters.maxRent = filters.maxRent
  }

  if (filters.minRent !== undefined) {
    nextFilters.minRent = filters.minRent
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

function getMutationKey(filters: PropertyListFilters) {
  return ['properties', 'list', filters] as const
}

function formatAddress(address: PropertyListItem['address']) {
  const line = [address.street, address.number].filter(Boolean).join(', ')
  const location = [address.neighborhood, address.city, address.state].filter(Boolean).join(' - ')

  return [line, location].filter(Boolean).join(' | ')
}

function getGarageLabel(row: PropertyListItem, copy: ReturnType<typeof getPropertyCopy>) {
  if (!row.hasGarage) {
    return copy.list.garageNo
  }

  return copy.list.garageSpaces(row.garageSpaces ?? 0)
}

function getFormInitialValue(property: PropertyDetail | PropertyListItem): PropertyFormRequest {
  return {
    address: property.address,
    garageSpaces: property.garageSpaces,
    hasGarage: property.hasGarage,
    name: property.name,
    notes: property.notes,
    status: property.status,
    suggestedRent: property.suggestedRent,
    type: property.type,
  }
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

function getStatusAction(row: PropertyListItem, copy: ReturnType<typeof getPropertyCopy>) {
  if (row.status === 'archived') {
    return undefined
  }

  if (row.status === 'maintenance') {
    return {
      Icon: CheckCircle2,
      label: copy.list.markAvailable,
      nextStatus: 'available' as const,
    }
  }

  return {
    Icon: Wrench,
    label: copy.list.markMaintenance,
    nextStatus: 'maintenance' as const,
  }
}

export function PropertiesListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[propertyFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getPropertyCopy(locale)
  const canWriteProperties = hasAnyPermission(['properties.write'], authUser)
  const canManageProperties = hasAnyPermission(['properties.manage'], authUser)
  const canArchiveProperties = hasAnyPermission(['properties.archive'], authUser)
  const filters = useMemo(() => normalizePropertyFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.status === 'archived',
    }),
    [filters],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailPropertyId, setDetailPropertyId] = useState<string | null>(null)
  useListRouteState({
    detailParams: propertyDetailRouteParams,
    onDetailIdChange: setDetailPropertyId,
    pageSize: defaultFilters.pageSize ?? 10,
    routeFilters: propertyRouteFilters,
    scope: propertyFilterScope,
    setFilters: setStoredFilters,
  })
  const propertiesQuery = useQuery({
    queryFn: () => listProperties(apiClient, apiFilters),
    queryKey: getMutationKey(apiFilters),
  })
  const activeFilterCount = [
    filters.search,
    filters.status,
    filters.type,
    filters.hasGarage === '' ? '' : String(filters.hasGarage),
    filters.includeArchived ? 'archived' : '',
    filters.minRent,
    filters.maxRent,
  ].filter((value) => value !== undefined && value !== '').length
  const page = propertiesQuery.data?.page ?? filters.page ?? 1
  const pageSize = propertiesQuery.data?.pageSize ?? filters.pageSize ?? 10
  const rows = propertiesQuery.data?.items ?? []
  const formInitialValue = formState?.property ? getFormInitialValue(formState.property) : undefined
  const statusOptions = useMemo(
    () => [
      { label: copy.list.allStatuses, value: '' },
      ...propertyStatuses.map((status) => ({
        label: copy.list.status[status],
        value: status,
      })),
    ],
    [copy],
  )
  const typeOptions = useMemo(
    () => [
      { label: copy.list.allTypes, value: '' },
      ...propertyTypes.map((type) => ({
        label: copy.list.types[type],
        value: type,
      })),
    ],
    [copy],
  )
  const garageOptions = useMemo(
    () => [
      { label: copy.list.allGarage, value: '' },
      { label: copy.list.garageYes, value: 'true' },
      { label: copy.list.garageNo, value: 'false' },
    ],
    [copy],
  )
  const formMutation = useMutation({
    mutationFn: (values: PropertyFormRequest) => {
      if (formState?.mode === 'edit' && formState.property) {
        return updateProperty(apiClient, formState.property.id, values)
      }

      return createProperty(apiClient, values)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['properties'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: {
      action: 'archive' | 'restore' | 'status'
      id: string
      status?: PropertyStatus
    }) => {
      if (request.action === 'archive') {
        await archiveProperty(apiClient, request.id)
        return
      }

      if (request.action === 'restore') {
        await restoreProperty(apiClient, request.id)
        return
      }

      if (request.status) {
        await updatePropertyStatus(apiClient, request.id, { status: request.status })
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['properties'] })
    },
  })

  function updateFilters(nextFilters: Partial<PropertyListFilters>) {
    setStoredFilters(
      propertyFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      propertyFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: PropertyListItem) {
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${row.name}`}
        onClick={() => setDetailPropertyId(row.id)}
      />,
    ]

    const statusAction = getStatusAction(row, copy)

    if (canWriteProperties && row.status !== 'archived') {
      actions.push(
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${row.name}`}
          onClick={() => setFormState({ mode: 'edit', property: row })}
        />,
      )
    }

    if (canManageProperties && statusAction) {
      const StatusIcon: LucideIcon = statusAction.Icon

      actions.push(
        <IconActionButton
          key="status"
          icon={<StatusIcon aria-hidden="true" size={16} />}
          label={`${statusAction.label} ${row.name}`}
          onClick={() =>
            lifecycleMutation.mutate({
              action: 'status',
              id: row.id,
              status: statusAction.nextStatus,
            })
          }
        />,
      )
    }

    if (canArchiveProperties) {
      actions.push(
        row.status === 'archived' ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${row.name}`}
            onClick={() => lifecycleMutation.mutate({ action: 'restore', id: row.id })}
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${row.name}`}
            onClick={() => lifecycleMutation.mutate({ action: 'archive', id: row.id })}
          />
        ),
      )
    }

    return actions
  }

  if (detailPropertyId) {
    return (
      <PropertyDetailPage
        propertyId={detailPropertyId}
        onBack={() => setDetailPropertyId(null)}
        onEdit={
          canWriteProperties
            ? (property) => {
                setDetailPropertyId(null)
                setFormState({ mode: 'edit', property })
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
          canWriteProperties
            ? {
                icon: <Home aria-hidden="true" size={18} />,
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
              updateFilters({ status: event.currentTarget.value as PropertyStatus | '' })
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
          <span style={{ fontWeight: 800 }}>{copy.list.type}</span>
          <select
            aria-label={copy.list.type}
            onChange={(event) =>
              updateFilters({ type: event.currentTarget.value as PropertyType | '' })
            }
            value={filters.type}
          >
            {typeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.hasGarage}</span>
          <select
            aria-label={copy.list.hasGarage}
            onChange={(event) =>
              updateFilters({
                hasGarage:
                  event.currentTarget.value === '' ? '' : event.currentTarget.value === 'true',
              })
            }
            value={filters.hasGarage === '' ? '' : String(filters.hasGarage)}
          >
            {garageOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4, minWidth: 132 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.minRent}</span>
          <TextInput
            aria-label={copy.list.minRent}
            min={0}
            onChange={(event) =>
              updateFilters({
                minRent:
                  event.currentTarget.value === '' ? undefined : Number(event.currentTarget.value),
              })
            }
            type="number"
            value={filters.minRent ?? ''}
          />
        </label>
        <label style={{ display: 'grid', gap: 4, minWidth: 132 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.maxRent}</span>
          <TextInput
            aria-label={copy.list.maxRent}
            min={0}
            onChange={(event) =>
              updateFilters({
                maxRent:
                  event.currentTarget.value === '' ? undefined : Number(event.currentTarget.value),
              })
            }
            type="number"
            value={filters.maxRent ?? ''}
          />
        </label>
        <CheckboxInput
          checked={Boolean(filters.includeArchived)}
          label={copy.list.includeArchived}
          onChange={(event) => updateFilters({ includeArchived: event.currentTarget.checked })}
        />
      </FilterBar>

      <DataTable
        columns={[
          {
            cell: (row) => (
              <div style={{ display: 'grid', gap: 4, minWidth: 0 }}>
                <strong>{row.name}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {row.typeLabel ?? (row.type ? copy.list.types[row.type] : (row.code ?? '-'))}
                </span>
              </div>
            ),
            header: copy.list.columns.property,
            id: 'property',
          },
          {
            cell: (row) => formatAddress(row.address),
            header: copy.list.columns.location,
            id: 'location',
          },
          {
            cell: (row) => (
              <StatusBadge
                label={row.statusLabel ?? copy.list.status[row.status]}
                tone={statusTones[row.status]}
              />
            ),
            header: copy.list.columns.status,
            id: 'status',
            width: 150,
          },
          {
            cell: (row) => formatMoney(row.suggestedRent, { currency: row.currencyCode, locale }),
            header: copy.list.columns.suggestedRent,
            id: 'suggestedRent',
            width: 160,
          },
          {
            cell: (row) => getGarageLabel(row, copy),
            header: copy.list.columns.garage,
            id: 'garage',
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
            width: 188,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          propertiesQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void propertiesQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={propertiesQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={propertiesQuery.data?.totalItems ?? 0}
      />

      <Drawer
        isOpen={Boolean(formState)}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setFormState(null)
          }
        }}
        title={formState ? copy.form.modeTitle[formState.mode] : copy.form.modeTitle.create}
        width={580}
      >
        {formState ? (
          <PropertyForm
            initialValue={formInitialValue}
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
