import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, Pencil, Power, RotateCcw, UserPlus } from 'lucide-react'
import {
  DataTable,
  Drawer,
  EmptyState,
  ErrorState,
  FilterBar,
  PageHeader,
  Pagination,
  RowActions,
  SearchInput,
  StatusBadge,
  type StatusBadgeTone,
} from '../../components'
import {
  archiveAdministrator,
  createAdministrator,
  deactivateAdministrator,
  listAdministratorRoleOptions,
  listAdministrators,
  reactivateAdministrator,
  updateAdministrator,
  type AdministratorFormRequest,
  type AdministratorListFilters,
  type AdministratorListItem,
  type AdministratorStatus,
} from '../../lib/api/administrators'
import { useApiClient } from '../../lib/api/ApiClientContext'
import type { ApiSelectOption } from '../../lib/api/contracts'
import { PermissionGate } from '../../routes/guards'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { hasAnyPermission } from '../identity/session'
import { getAdministratorCopy } from './adminCopy'
import { AdministratorForm, type AdministratorFormMode } from './AdministratorForm'

const administratorStatusOptions: AdministratorStatus[] = [
  'active',
  'invited',
  'inactive',
  'locked',
  'archived',
]

const emptyRoleOptions: ApiSelectOption[] = []

const statusTones: Record<AdministratorStatus, StatusBadgeTone> = {
  active: 'success',
  archived: 'archived',
  inactive: 'neutral',
  invited: 'info',
  locked: 'danger',
}

type FormState = {
  administrator?: AdministratorListItem
  mode: AdministratorFormMode
}

function getRoleLabels(row: AdministratorListItem) {
  return row.roleLabels?.length ? row.roleLabels : row.roleCodes
}

function getMutationKey(filters: AdministratorListFilters) {
  return ['administrators', filters] as const
}

export function AdministratorsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getAdministratorCopy(locale)
  const canWriteAdministrators = hasAnyPermission(['administrators.write'], authUser)
  const [filters, setFilters] = useState<AdministratorListFilters>({
    page: 1,
    pageSize: 10,
    role: '',
    search: '',
    status: '',
  })
  const [formState, setFormState] = useState<FormState | null>(null)
  const administratorsQuery = useQuery({
    queryFn: () => listAdministrators(apiClient, filters),
    queryKey: getMutationKey(filters),
  })
  const roleOptionsQuery = useQuery({
    queryFn: () => listAdministratorRoleOptions(apiClient),
    queryKey: ['administrator-role-options'],
  })
  const roleOptions = roleOptionsQuery.data ?? emptyRoleOptions
  const activeFilterCount = [filters.search, filters.status, filters.role].filter(Boolean).length
  const page = administratorsQuery.data?.page ?? filters.page ?? 1
  const pageSize = administratorsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const rows = administratorsQuery.data?.items ?? []
  const formInitialValue = formState?.administrator
    ? {
        displayName: formState.administrator.displayName,
        email: formState.administrator.email,
        roleCodes: formState.administrator.roleCodes,
      }
    : undefined
  const formMutation = useMutation({
    mutationFn: (values: AdministratorFormRequest) => {
      if (formState?.mode === 'edit' && formState.administrator) {
        return updateAdministrator(apiClient, formState.administrator.id, values)
      }

      return createAdministrator(apiClient, values)
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['administrators'] })
    },
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: {
      action: 'archive' | 'deactivate' | 'reactivate'
      id: string
    }) => {
      if (request.action === 'archive') {
        await archiveAdministrator(apiClient, request.id)
        return
      }

      if (request.action === 'reactivate') {
        await reactivateAdministrator(apiClient, request.id)
        return
      }

      await deactivateAdministrator(apiClient, request.id)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administrators'] })
    },
  })
  const statusOptions = useMemo<ApiSelectOption[]>(
    () => [
      { label: copy.list.allStatuses, value: '' },
      ...administratorStatusOptions.map((status) => ({
        label: copy.list.status[status],
        value: status,
      })),
    ],
    [copy],
  )
  const roleFilterOptions = useMemo<ApiSelectOption[]>(
    () => [{ label: copy.list.allRoles, value: '' }, ...roleOptions],
    [copy.list.allRoles, roleOptions],
  )

  function updateFilters(nextFilters: Partial<AdministratorListFilters>) {
    setFilters((current) => ({
      ...current,
      ...nextFilters,
      page: nextFilters.page ?? 1,
    }))
  }

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        title={copy.list.pageTitle}
        description={copy.list.pageDescription}
        primaryAction={
          canWriteAdministrators
            ? {
                icon: <UserPlus aria-hidden="true" size={18} />,
                label: copy.list.invite,
                onClick: () => setFormState({ mode: 'create' }),
              }
            : undefined
        }
      />

      <FilterBar
        activeCount={activeFilterCount}
        clearLabel={copy.list.clearFilters}
        label={copy.list.filters}
        onClear={() =>
          setFilters({
            page: 1,
            pageSize,
            role: '',
            search: '',
            status: '',
          })
        }
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
              updateFilters({ status: event.currentTarget.value as AdministratorStatus | '' })
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
          <span style={{ fontWeight: 800 }}>{copy.list.columns.roles}</span>
          <select
            aria-label={copy.list.columns.roles}
            onChange={(event) => updateFilters({ role: event.currentTarget.value })}
            value={filters.role}
          >
            {roleFilterOptions.map((option) => (
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
                <strong>{row.displayName}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>{row.email}</span>
              </div>
            ),
            header: copy.list.columns.account,
            id: 'account',
          },
          {
            cell: (row) => getRoleLabels(row).join(', '),
            header: copy.list.columns.roles,
            id: 'roles',
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
            cell: (row) => row.lastLoginAt ?? row.invitedAt ?? '-',
            header: copy.list.columns.lastAccess,
            id: 'lastAccess',
            width: 180,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          administratorsQuery.isError ? (
            <ErrorState
              title={copy.list.error}
              onRetry={() => void administratorsQuery.refetch()}
            />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={administratorsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rowActions={(row) => (
          <PermissionGate permissions={['administrators.write']}>
            <RowActions
              labels={{ menu: copy.list.rowActions }}
              actions={[
                {
                  icon: <Pencil aria-hidden="true" size={16} />,
                  id: 'edit',
                  label: copy.list.edit,
                  onSelect: () => setFormState({ administrator: row, mode: 'edit' }),
                },
                row.status === 'active'
                  ? {
                      icon: <Power aria-hidden="true" size={16} />,
                      id: 'deactivate',
                      label: copy.list.deactivate,
                      onSelect: () =>
                        lifecycleMutation.mutate({ action: 'deactivate', id: row.id }),
                    }
                  : {
                      icon: <RotateCcw aria-hidden="true" size={16} />,
                      id: 'reactivate',
                      label: copy.list.reactivate,
                      onSelect: () =>
                        lifecycleMutation.mutate({ action: 'reactivate', id: row.id }),
                    },
                {
                  icon: <Archive aria-hidden="true" size={16} />,
                  id: 'archive',
                  isDestructive: true,
                  label: copy.list.archive,
                  onSelect: () => lifecycleMutation.mutate({ action: 'archive', id: row.id }),
                },
              ]}
            />
          </PermissionGate>
        )}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => setFilters((current) => ({ ...current, page: nextPage }))}
        page={page}
        pageSize={pageSize}
        totalItems={administratorsQuery.data?.totalItems ?? 0}
      />

      <Drawer
        isOpen={Boolean(formState)}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setFormState(null)
          }
        }}
        title={formState ? copy.form.modeTitle[formState.mode] : copy.form.modeTitle.create}
        width={520}
      >
        {formState ? (
          <AdministratorForm
            initialValue={formInitialValue}
            isSubmitting={formMutation.isPending}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onSubmit={async (values) => {
              await formMutation.mutateAsync(values)
            }}
            roleOptions={roleOptions}
          />
        ) : null}
      </Drawer>
    </section>
  )
}
