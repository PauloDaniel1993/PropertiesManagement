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
  archiveVehicle,
  authorizeVehicle,
  createVehicle,
  denyVehicle,
  getVehicle,
  listVehicleAuthorizationStatusOptions,
  listVehicles,
  listVehicleTypeOptions,
  restoreVehicle,
  updateVehicle,
  vehicleAuthorizationStatuses,
  vehicleTypes,
  type VehicleAuthorizationStatus,
  type VehicleDetail,
  type VehicleFormRequest,
  type VehicleListFilters,
  type VehicleListItem,
  type VehicleType,
} from '../../lib/api/vehicles'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { VehicleDetailPage } from './VehicleDetailPage'
import { VehicleForm, type VehicleFormMode } from './VehicleForm'
import { getVehicleCopy } from './vehicleCopy'

const vehicleFilterScope = 'vehicles.list'

const defaultFilters: VehicleListFilters = {
  authorizationStatus: '',
  includeArchived: false,
  page: 1,
  pageSize: 10,
  search: '',
  type: '',
}

const statusTones: Record<VehicleAuthorizationStatus, StatusBadgeTone> = {
  archived: 'archived',
  authorized: 'success',
  denied: 'danger',
  inactive: 'neutral',
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
  mode: VehicleFormMode
  vehicle?: Partial<VehicleDetail | VehicleListItem> & Pick<VehicleListItem, 'id'>
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

function isVehicleType(value: FilterValue | undefined): value is VehicleType {
  return typeof value === 'string' && vehicleTypes.includes(value as VehicleType)
}

function isVehicleAuthorizationStatus(
  value: FilterValue | undefined,
): value is VehicleAuthorizationStatus {
  return (
    typeof value === 'string' &&
    vehicleAuthorizationStatuses.includes(value as VehicleAuthorizationStatus)
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

  if (value === 'true') {
    return true
  }

  if (value === 'false') {
    return false
  }

  return undefined
}

function normalizeVehicleFilters(filters: FilterSet | undefined): VehicleListFilters {
  const authorizationStatus = filters?.authorizationStatus
  const type = filters?.type

  return {
    authorizationStatus: isVehicleAuthorizationStatus(authorizationStatus)
      ? authorizationStatus
      : '',
    contractId: getStringFilter(filters?.contractId) || undefined,
    hasParkingAllocation: getBooleanFilter(filters?.hasParkingAllocation),
    includeArchived: getBooleanFilter(filters?.includeArchived) ?? false,
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    parkingSpaceIdentifier: getStringFilter(filters?.parkingSpaceIdentifier) || undefined,
    plate: getStringFilter(filters?.plate) || undefined,
    propertyId: getStringFilter(filters?.propertyId) || undefined,
    residentId: getStringFilter(filters?.residentId) || undefined,
    search: getStringFilter(filters?.search),
    type: isVehicleType(type) ? type : '',
  }
}

function toFilterSet(filters: VehicleListFilters): FilterSet {
  const nextFilters: FilterSet = {
    includeArchived: filters.includeArchived ?? false,
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  if (filters.authorizationStatus) {
    nextFilters.authorizationStatus = filters.authorizationStatus
  }

  if (filters.contractId) {
    nextFilters.contractId = filters.contractId
  }

  if (typeof filters.hasParkingAllocation === 'boolean') {
    nextFilters.hasParkingAllocation = filters.hasParkingAllocation
  }

  if (filters.parkingSpaceIdentifier) {
    nextFilters.parkingSpaceIdentifier = filters.parkingSpaceIdentifier
  }

  if (filters.plate) {
    nextFilters.plate = filters.plate
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

function vehicleTitleForAction(row: VehicleListItem) {
  return row.property?.name ? `${row.plate} - ${row.property.name}` : row.plate
}

function getFormInitialValue(vehicle: VehicleDetail | VehicleListItem): Partial<VehicleDetail> {
  return {
    authorizationStatus: vehicle.authorizationStatus,
    brand: vehicle.brand,
    color: vehicle.color,
    concurrencyToken: vehicle.concurrencyToken,
    contract: vehicle.contract,
    id: vehicle.id,
    model: vehicle.model,
    notes: 'notes' in vehicle ? vehicle.notes : undefined,
    parkingAllocationNotes: vehicle.parkingAllocationNotes,
    parkingSpaceIdentifier: vehicle.parkingSpaceIdentifier,
    plate: vehicle.plate,
    property: vehicle.property,
    resident: vehicle.resident,
    type: vehicle.type,
    year: vehicle.year,
  }
}

function formatVehicleName(vehicle: VehicleListItem) {
  const details = [vehicle.brand, vehicle.model, vehicle.year].filter(Boolean).join(' ')

  return details.length > 0 ? details : vehicle.normalizedPlate
}

export function VehiclesListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[vehicleFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getVehicleCopy(locale)
  const canWriteVehicles = hasAnyPermission(['vehicles.write'], authUser)
  const canManageVehicles = hasAnyPermission(['vehicles.manage'], authUser)
  const canArchiveVehicles = hasAnyPermission(['vehicles.archive'], authUser)
  const filters = useMemo(() => normalizeVehicleFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.authorizationStatus === 'archived',
      locale,
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailVehicleId, setDetailVehicleId] = useState<string | null>(null)
  const vehiclesQuery = useQuery({
    queryFn: () => listVehicles(apiClient, apiFilters),
    queryKey: ['vehicles', 'list', apiFilters],
  })
  const typeOptionsQuery = useQuery({
    queryFn: () => listVehicleTypeOptions(apiClient, locale),
    queryKey: ['vehicles', 'type-options', locale],
  })
  const statusOptionsQuery = useQuery({
    queryFn: () => listVehicleAuthorizationStatusOptions(apiClient, locale),
    queryKey: ['vehicles', 'authorization-status-options', locale],
  })
  const rows = vehiclesQuery.data?.items ?? []
  const page = vehiclesQuery.data?.page ?? filters.page ?? 1
  const pageSize = vehiclesQuery.data?.pageSize ?? filters.pageSize ?? 10
  const typeOptions =
    typeOptionsQuery.data ??
    vehicleTypes.map((type) => ({ label: copy.terms.types[type], value: type }))
  const statusOptions =
    statusOptionsQuery.data ??
    vehicleAuthorizationStatuses.map((status) => ({
      label: copy.list.status[status],
      value: status,
    }))
  const activeFilterCount = [
    filters.search,
    filters.plate,
    filters.type,
    filters.authorizationStatus,
    filters.residentId,
    filters.propertyId,
    filters.contractId,
    filters.parkingSpaceIdentifier,
    typeof filters.hasParkingAllocation === 'boolean' ? String(filters.hasParkingAllocation) : '',
    filters.includeArchived ? 'true' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const formMutation = useMutation({
    mutationFn: (values: VehicleFormRequest) => {
      if (formState?.mode === 'edit' && formState.vehicle) {
        return updateVehicle(apiClient, formState.vehicle.id, values, locale)
      }

      return createVehicle(apiClient, values, locale)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['vehicles'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: {
      action: 'archive' | 'authorize' | 'deny' | 'restore'
      vehicleId: string
    }) => {
      if (request.action === 'archive') {
        await archiveVehicle(apiClient, request.vehicleId)
        return
      }

      if (request.action === 'authorize') {
        await authorizeVehicle(apiClient, request.vehicleId, locale)
        return
      }

      if (request.action === 'deny') {
        await denyVehicle(apiClient, request.vehicleId, undefined, locale)
        return
      }

      await restoreVehicle(apiClient, request.vehicleId, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['vehicles'] })
    },
  })
  const editVehicleMutation = useMutation({
    mutationFn: (vehicleId: string) => getVehicle(apiClient, vehicleId, locale),
    onSuccess: (vehicle) => {
      setFormState({
        mode: 'edit',
        vehicle: getFormInitialValue(vehicle) as FormState['vehicle'],
      })
    },
  })

  function updateFilters(nextFilters: Partial<VehicleListFilters>) {
    setStoredFilters(
      vehicleFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      vehicleFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: VehicleListItem) {
    const rowTitle = vehicleTitleForAction(row)
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${rowTitle}`}
        onClick={() => setDetailVehicleId(row.id)}
      />,
    ]

    if (canManageVehicles && row.authorizationStatus.code !== 'archived') {
      if (row.authorizationStatus.code !== 'authorized') {
        actions.push(
          <IconActionButton
            key="authorize"
            icon={<CheckCircle2 aria-hidden="true" size={16} />}
            label={`${copy.list.authorize} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'authorize', vehicleId: row.id })}
          />,
        )
      }

      if (row.authorizationStatus.code !== 'denied') {
        actions.push(
          <IconActionButton
            key="deny"
            icon={<Ban aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.deny} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'deny', vehicleId: row.id })}
          />,
        )
      }
    }

    if (canWriteVehicles && row.authorizationStatus.code !== 'archived') {
      actions.push(
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${rowTitle}`}
          onClick={() => editVehicleMutation.mutate(row.id)}
        />,
      )
    }

    if (canArchiveVehicles) {
      actions.push(
        row.authorizationStatus.code === 'archived' || row.isArchived ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'restore', vehicleId: row.id })}
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'archive', vehicleId: row.id })}
          />
        ),
      )
    }

    return actions
  }

  if (detailVehicleId) {
    return (
      <VehicleDetailPage
        vehicleId={detailVehicleId}
        onBack={() => setDetailVehicleId(null)}
        onAuthorize={
          canManageVehicles
            ? (vehicle) => lifecycleMutation.mutate({ action: 'authorize', vehicleId: vehicle.id })
            : undefined
        }
        onDeny={
          canManageVehicles
            ? (vehicle) => lifecycleMutation.mutate({ action: 'deny', vehicleId: vehicle.id })
            : undefined
        }
        onEdit={
          canWriteVehicles
            ? (vehicle) => {
                setDetailVehicleId(null)
                setFormState({
                  mode: 'edit',
                  vehicle: getFormInitialValue(vehicle) as FormState['vehicle'],
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
          canWriteVehicles
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
              updateFilters({ type: event.currentTarget.value as VehicleType | '' })
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
          <span style={{ fontWeight: 800 }}>{copy.list.columns.authorizationStatus}</span>
          <select
            aria-label={copy.list.columns.authorizationStatus}
            onChange={(event) =>
              updateFilters({
                authorizationStatus: event.currentTarget.value as VehicleAuthorizationStatus | '',
              })
            }
            value={filters.authorizationStatus}
          >
            {withEmptyOption(copy.list.allStatuses, statusOptions).map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.columns.parking}</span>
          <select
            aria-label={copy.list.columns.parking}
            onChange={(event) =>
              updateFilters({
                hasParkingAllocation:
                  event.currentTarget.value === ''
                    ? undefined
                    : event.currentTarget.value === 'true',
              })
            }
            value={
              typeof filters.hasParkingAllocation === 'boolean'
                ? String(filters.hasParkingAllocation)
                : ''
            }
          >
            <option value="">{copy.list.allParking}</option>
            <option value="true">{copy.terms.parking.true}</option>
            <option value="false">{copy.terms.parking.false}</option>
          </select>
        </label>
        <TextInput
          aria-label={copy.list.plate}
          onChange={(event) => updateFilters({ plate: event.currentTarget.value })}
          placeholder={copy.list.plate}
          style={{ maxWidth: 150 }}
          value={filters.plate ?? ''}
        />
        <TextInput
          aria-label={copy.list.residentId}
          onChange={(event) => updateFilters({ residentId: event.currentTarget.value })}
          placeholder={copy.list.residentId}
          style={{ maxWidth: 210 }}
          value={filters.residentId ?? ''}
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
        <TextInput
          aria-label={copy.list.parkingSpaceIdentifier}
          onChange={(event) => updateFilters({ parkingSpaceIdentifier: event.currentTarget.value })}
          placeholder={copy.list.parkingSpaceIdentifier}
          style={{ maxWidth: 150 }}
          value={filters.parkingSpaceIdentifier ?? ''}
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
                <strong>{row.plate}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {formatVehicleName(row)}
                </span>
              </div>
            ),
            header: copy.list.columns.vehicle,
            id: 'vehicle',
          },
          {
            cell: (row) => row.type.label,
            header: copy.list.columns.type,
            id: 'type',
            width: 130,
          },
          {
            cell: (row) => (
              <StatusBadge
                label={row.authorizationStatus.label}
                tone={statusTones[row.authorizationStatus.code]}
              />
            ),
            header: copy.list.columns.authorizationStatus,
            id: 'authorizationStatus',
            width: 150,
          },
          {
            cell: (row) => row.resident.name,
            header: copy.list.columns.owner,
            id: 'owner',
            width: 190,
          },
          {
            cell: (row) => row.property?.name ?? copy.terms.none,
            header: copy.list.columns.property,
            id: 'property',
            width: 190,
          },
          {
            cell: (row) => row.parkingSpaceIdentifier ?? copy.terms.none,
            header: copy.list.columns.parking,
            id: 'parking',
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
            width: 260,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          vehiclesQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void vehiclesQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={vehiclesQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={vehiclesQuery.data?.totalItems ?? 0}
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
          <VehicleForm
            authorizationStatusOptions={statusOptions}
            canManageAuthorization={canManageVehicles}
            initialValue={formState.vehicle}
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
