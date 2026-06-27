import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, CheckCircle2, Eye, PawPrint, Pencil, RotateCcw, ShieldX } from 'lucide-react'
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
import {
  archivePet,
  authorizePet,
  createPet,
  denyPet,
  getPet,
  getPetOptions,
  listPets,
  petAuthorizationStatuses,
  petSpecies,
  restorePet,
  updatePet,
  type PetAuthorizationStatus,
  type PetDetail,
  type PetFormRequest,
  type PetListFilters,
  type PetListItem,
  type PetSpecies,
} from '../../lib/api/pets'
import { useApiClient } from '../../lib/api/ApiClientContext'
import type { ApiSelectOption } from '../../lib/api/contracts'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { PetDetailPage } from './PetDetailPage'
import { PetForm, type PetFormMode } from './PetForm'
import { getPetCopy } from './petCopy'

const petFilterScope = 'pets.list'

const defaultFilters: PetListFilters = {
  activeContractOnly: false,
  authorizationStatus: '',
  includeArchived: false,
  page: 1,
  pageSize: 10,
  search: '',
  species: '',
}

const statusTones: Record<PetAuthorizationStatus, StatusBadgeTone> = {
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
  mode: PetFormMode
  pet?: Partial<PetDetail | PetListItem> & Pick<PetListItem, 'id'>
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

function isPetSpecies(value: FilterValue | undefined): value is PetSpecies {
  return typeof value === 'string' && petSpecies.includes(value as PetSpecies)
}

function isPetAuthorizationStatus(value: FilterValue | undefined): value is PetAuthorizationStatus {
  return (
    typeof value === 'string' && petAuthorizationStatuses.includes(value as PetAuthorizationStatus)
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

function normalizePetFilters(filters: FilterSet | undefined): PetListFilters {
  const authorizationStatus = filters?.authorizationStatus
  const species = filters?.species

  return {
    activeContractOnly: getBooleanFilter(filters?.activeContractOnly),
    authorizationStatus: isPetAuthorizationStatus(authorizationStatus) ? authorizationStatus : '',
    contractId: getStringFilter(filters?.contractId) || undefined,
    includeArchived: getBooleanFilter(filters?.includeArchived),
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    propertyId: getStringFilter(filters?.propertyId) || undefined,
    residentId: getStringFilter(filters?.residentId) || undefined,
    search: getStringFilter(filters?.search),
    species: isPetSpecies(species) ? species : '',
  }
}

function toFilterSet(filters: PetListFilters): FilterSet {
  const nextFilters: FilterSet = {
    activeContractOnly: filters.activeContractOnly ?? false,
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

  if (filters.propertyId) {
    nextFilters.propertyId = filters.propertyId
  }

  if (filters.residentId) {
    nextFilters.residentId = filters.residentId
  }

  if (filters.search) {
    nextFilters.search = filters.search
  }

  if (filters.species) {
    nextFilters.species = filters.species
  }

  return nextFilters
}

function withEmptyOption<TValue extends string>(
  label: string,
  options: Array<ApiSelectOption<TValue>>,
) {
  return [{ label, value: '' }, ...options]
}

function petTitleForAction(row: PetListItem) {
  return `${row.name} - ${row.resident.name}`
}

function formatContext(row: PetListItem, fallback: string) {
  if (row.contract) {
    return row.contract.name
  }

  if (row.property) {
    return row.property.name
  }

  return fallback
}

function getFormInitialValue(pet: PetDetail | PetListItem): Partial<PetDetail> {
  return {
    authorizationFormDocuments: pet.authorizationFormDocuments,
    authorizationNotes: pet.authorizationNotes,
    authorizationStatus: pet.authorizationStatus,
    breed: pet.breed,
    concurrencyToken: pet.concurrencyToken,
    contract: pet.contract,
    id: pet.id,
    name: pet.name,
    notes: 'notes' in pet ? pet.notes : undefined,
    property: pet.property,
    resident: pet.resident,
    species: pet.species,
    vaccinationRecordDocuments: pet.vaccinationRecordDocuments,
  }
}

export function PetsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[petFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getPetCopy(locale)
  const canWritePets = hasAnyPermission(['pets.write'], authUser)
  const canManagePets = hasAnyPermission(['pets.manage'], authUser)
  const canArchivePets = hasAnyPermission(['pets.archive'], authUser)
  const filters = useMemo(() => normalizePetFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: filters.includeArchived || filters.authorizationStatus === 'archived',
      locale,
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailPetId, setDetailPetId] = useState<string | null>(null)
  const petsQuery = useQuery({
    queryFn: () => listPets(apiClient, apiFilters),
    queryKey: ['pets', 'list', apiFilters],
  })
  const optionsQuery = useQuery({
    queryFn: () => getPetOptions(apiClient, locale),
    queryKey: ['pets', 'options', locale],
  })
  const rows = petsQuery.data?.items ?? []
  const page = petsQuery.data?.page ?? filters.page ?? 1
  const pageSize = petsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const speciesOptions =
    optionsQuery.data?.species ??
    petSpecies.map((species) => ({ label: copy.terms.species[species], value: species }))
  const authorizationStatusOptions =
    optionsQuery.data?.authorizationStatuses ??
    petAuthorizationStatuses.map((status) => ({ label: copy.list.status[status], value: status }))
  const activeFilterCount = [
    filters.search,
    filters.species,
    filters.authorizationStatus,
    filters.residentId,
    filters.propertyId,
    filters.contractId,
    filters.activeContractOnly ? 'true' : '',
    filters.includeArchived ? 'true' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const formMutation = useMutation({
    mutationFn: (values: PetFormRequest) => {
      if (formState?.mode === 'edit' && formState.pet) {
        return updatePet(apiClient, formState.pet.id, values, locale)
      }

      return createPet(apiClient, values, locale)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['pets'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: {
      action: 'archive' | 'authorize' | 'deny' | 'restore'
      petId: string
    }) => {
      if (request.action === 'archive') {
        await archivePet(apiClient, request.petId)
        return
      }

      if (request.action === 'authorize') {
        await authorizePet(apiClient, request.petId, undefined, locale)
        return
      }

      if (request.action === 'deny') {
        await denyPet(apiClient, request.petId, undefined, locale)
        return
      }

      await restorePet(apiClient, request.petId, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['pets'] })
    },
  })
  const editPetMutation = useMutation({
    mutationFn: (petId: string) => getPet(apiClient, petId, locale),
    onSuccess: (pet) => {
      setFormState({
        mode: 'edit',
        pet: getFormInitialValue(pet) as FormState['pet'],
      })
    },
  })

  function updateFilters(nextFilters: Partial<PetListFilters>) {
    setStoredFilters(
      petFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      petFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: PetListItem) {
    const rowTitle = petTitleForAction(row)
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${rowTitle}`}
        onClick={() => setDetailPetId(row.id)}
      />,
    ]

    if (canManagePets && !['archived', 'authorized'].includes(row.authorizationStatus.code)) {
      actions.push(
        <IconActionButton
          key="authorize"
          icon={<CheckCircle2 aria-hidden="true" size={16} />}
          label={`${copy.list.authorize} ${rowTitle}`}
          onClick={() => lifecycleMutation.mutate({ action: 'authorize', petId: row.id })}
        />,
      )
    }

    if (canManagePets && !['archived', 'denied'].includes(row.authorizationStatus.code)) {
      actions.push(
        <IconActionButton
          key="deny"
          icon={<ShieldX aria-hidden="true" size={16} />}
          isDestructive
          label={`${copy.list.deny} ${rowTitle}`}
          onClick={() => lifecycleMutation.mutate({ action: 'deny', petId: row.id })}
        />,
      )
    }

    if (canWritePets && row.authorizationStatus.code !== 'archived') {
      actions.push(
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${rowTitle}`}
          onClick={() => editPetMutation.mutate(row.id)}
        />,
      )
    }

    if (canArchivePets) {
      actions.push(
        row.authorizationStatus.code === 'archived' ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'restore', petId: row.id })}
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${rowTitle}`}
            onClick={() => lifecycleMutation.mutate({ action: 'archive', petId: row.id })}
          />
        ),
      )
    }

    return actions
  }

  if (detailPetId) {
    return (
      <PetDetailPage
        petId={detailPetId}
        onBack={() => setDetailPetId(null)}
        onAuthorize={
          canManagePets
            ? (pet) => lifecycleMutation.mutate({ action: 'authorize', petId: pet.id })
            : undefined
        }
        onDeny={
          canManagePets
            ? (pet) => lifecycleMutation.mutate({ action: 'deny', petId: pet.id })
            : undefined
        }
        onEdit={
          canWritePets
            ? (pet) => {
                setDetailPetId(null)
                setFormState({
                  mode: 'edit',
                  pet: getFormInitialValue(pet) as FormState['pet'],
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
          canWritePets
            ? {
                icon: <PawPrint aria-hidden="true" size={18} />,
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
          <span style={{ fontWeight: 800 }}>{copy.list.columns.species}</span>
          <select
            aria-label={copy.list.columns.species}
            onChange={(event) =>
              updateFilters({ species: event.currentTarget.value as PetSpecies | '' })
            }
            value={filters.species}
          >
            {withEmptyOption(copy.list.allSpecies, speciesOptions).map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.columns.authorization}</span>
          <select
            aria-label={copy.list.columns.authorization}
            onChange={(event) =>
              updateFilters({
                authorizationStatus: event.currentTarget.value as PetAuthorizationStatus | '',
              })
            }
            value={filters.authorizationStatus}
          >
            {withEmptyOption(copy.list.allStatuses, authorizationStatusOptions).map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4, minWidth: 180 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.residentId}</span>
          <TextInput
            aria-label={copy.list.residentId}
            onChange={(event) => updateFilters({ residentId: event.currentTarget.value })}
            value={filters.residentId ?? ''}
          />
        </label>
        <label style={{ display: 'grid', gap: 4, minWidth: 180 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.propertyId}</span>
          <TextInput
            aria-label={copy.list.propertyId}
            onChange={(event) => updateFilters({ propertyId: event.currentTarget.value })}
            value={filters.propertyId ?? ''}
          />
        </label>
        <label style={{ display: 'grid', gap: 4, minWidth: 180 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.contractId}</span>
          <TextInput
            aria-label={copy.list.contractId}
            onChange={(event) => updateFilters({ contractId: event.currentTarget.value })}
            value={filters.contractId ?? ''}
          />
        </label>
        <label style={{ alignItems: 'center', display: 'flex', gap: 8, minHeight: 44 }}>
          <input
            checked={Boolean(filters.activeContractOnly)}
            onChange={(event) => updateFilters({ activeContractOnly: event.currentTarget.checked })}
            type="checkbox"
          />
          <span style={{ fontWeight: 800 }}>{copy.list.activeContractOnly}</span>
        </label>
        <label style={{ alignItems: 'center', display: 'flex', gap: 8, minHeight: 44 }}>
          <input
            checked={Boolean(filters.includeArchived)}
            onChange={(event) => updateFilters({ includeArchived: event.currentTarget.checked })}
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
                <strong>{row.name}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {row.breed ?? copy.terms.none}
                </span>
              </div>
            ),
            header: copy.list.columns.pet,
            id: 'pet',
          },
          {
            cell: (row) => row.resident.name,
            header: copy.list.columns.owner,
            id: 'owner',
          },
          {
            cell: (row) => row.species.label,
            header: copy.list.columns.species,
            id: 'species',
            width: 140,
          },
          {
            cell: (row) => (
              <StatusBadge
                label={row.authorizationStatus.label}
                tone={statusTones[row.authorizationStatus.code]}
              />
            ),
            header: copy.list.columns.authorization,
            id: 'authorization',
            width: 150,
          },
          {
            cell: (row) => formatContext(row, copy.terms.none),
            header: copy.list.columns.context,
            id: 'context',
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
          petsQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void petsQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={petsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={petsQuery.data?.totalItems ?? 0}
      />

      <Drawer
        isOpen={Boolean(formState)}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setFormState(null)
          }
        }}
        title={formState ? copy.form.modeTitle[formState.mode] : copy.form.modeTitle.create}
        width={640}
      >
        {formState ? (
          <PetForm
            authorizationStatusOptions={authorizationStatusOptions}
            canManageAuthorization={canManagePets}
            initialValue={formState.pet}
            isSubmitting={formMutation.isPending}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onSubmit={async (values) => {
              await formMutation.mutateAsync(values)
            }}
            speciesOptions={speciesOptions}
          />
        ) : null}
      </Drawer>
    </section>
  )
}
