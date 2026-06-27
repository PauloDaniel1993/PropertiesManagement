import { useMemo, useState, type CSSProperties } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Eye } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import {
  DataTable,
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
  listAuditCategoryOptions,
  listAuditEntries,
  type AuditEntry,
  type AuditListFilters,
} from '../../lib/api/audit'
import { formatDateTime } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { getAuditCopy } from './auditCopy'

const auditFilterScope = 'audit.list'

const defaultFilters: AuditListFilters = {
  category: '',
  page: 1,
  pageSize: 10,
  search: '',
  sort: '',
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

function normalizeFilters(filters: FilterSet | undefined): AuditListFilters {
  return {
    actor: getStringFilter(filters?.actor),
    category: getStringFilter(filters?.category),
    entityId: getStringFilter(filters?.entityId),
    entityType: getStringFilter(filters?.entityType),
    from: getStringFilter(filters?.from),
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    search: getStringFilter(filters?.search),
    sort: getStringFilter(filters?.sort),
    to: getStringFilter(filters?.to),
  }
}

function toFilterSet(filters: AuditListFilters): FilterSet {
  const nextFilters: FilterSet = {
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  for (const key of [
    'actor',
    'category',
    'entityId',
    'entityType',
    'from',
    'search',
    'sort',
    'to',
  ] as const) {
    if (filters[key]) {
      nextFilters[key] = filters[key]
    }
  }

  return nextFilters
}

function getRouteEntityFilters(searchParams: URLSearchParams): Partial<AuditListFilters> {
  const entityType = searchParams.get('entityType')?.trim()
  const entityId = searchParams.get('entityId')?.trim()

  if (entityType && entityId) {
    return { entityId, entityType }
  }

  const propertyId = searchParams.get('propertyId')?.trim()
  if (propertyId) {
    return { entityId: propertyId, entityType: 'property' }
  }

  const residentId = searchParams.get('residentId')?.trim()
  if (residentId) {
    return { entityId: residentId, entityType: 'resident' }
  }

  return {}
}

function getBadgeTone(tone: string | undefined): StatusBadgeTone {
  if (
    tone === 'danger' ||
    tone === 'info' ||
    tone === 'neutral' ||
    tone === 'success' ||
    tone === 'warning'
  ) {
    return tone
  }

  return 'neutral'
}

function formatDictionary(values: Record<string, string>, empty: string) {
  const entries = Object.entries(values)

  if (entries.length === 0) {
    return <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>{empty}</span>
  }

  return (
    <dl style={{ display: 'grid', gap: 6, margin: 0 }}>
      {entries.map(([key, value]) => (
        <div key={key} style={{ display: 'grid', gap: 2 }}>
          <dt style={{ color: 'var(--als-color-text-muted, #64748b)', fontWeight: 800 }}>{key}</dt>
          <dd style={{ margin: 0, overflowWrap: 'anywhere' }}>{value}</dd>
        </div>
      ))}
    </dl>
  )
}

export function AuditListPage() {
  const apiClient = useApiClient()
  const [searchParams] = useSearchParams()
  const locale = useAppPreferencesStore((state) => state.locale)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[auditFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getAuditCopy(locale)
  const persistedFilters = useMemo(() => normalizeFilters(storedFilters), [storedFilters])
  const routeSearch = searchParams.toString()
  const routeEntityFilters = useMemo(
    () => getRouteEntityFilters(new URLSearchParams(routeSearch)),
    [routeSearch],
  )
  const filters = useMemo(
    () => ({
      ...persistedFilters,
      ...routeEntityFilters,
    }),
    [persistedFilters, routeEntityFilters],
  )
  const [selectedEntry, setSelectedEntry] = useState<AuditEntry | null>(null)
  const auditQuery = useQuery({
    queryFn: () => listAuditEntries(apiClient, filters, locale),
    queryKey: ['audit', 'list', filters, locale],
  })
  const categoryOptionsQuery = useQuery({
    queryFn: () => listAuditCategoryOptions(apiClient, locale),
    queryKey: ['audit', 'category-options', locale],
  })
  const page = auditQuery.data?.page ?? filters.page ?? 1
  const pageSize = auditQuery.data?.pageSize ?? filters.pageSize ?? 10
  const rows = auditQuery.data?.items ?? []
  const activeFilterCount = [
    filters.actor,
    filters.category,
    filters.entityId,
    filters.entityType,
    filters.from,
    filters.search,
    filters.sort,
    filters.to,
  ].filter(Boolean).length
  const categoryOptions = [
    { label: copy.allCategories, value: '' },
    ...(
      categoryOptionsQuery.data ??
      Object.entries(copy.category).map(([code, label]) => ({ code, label }))
    ).map((category) => ({
      label: category.label,
      value: category.code,
    })),
  ]
  const sortOptions = [
    { label: copy.allSorts, value: '' },
    { label: copy.sortOldest, value: 'oldest' },
    { label: copy.sortAction, value: 'action' },
    { label: copy.sortCategory, value: 'category' },
    { label: copy.sortActor, value: 'actor' },
  ]

  function updateFilters(nextFilters: Partial<AuditListFilters>) {
    setStoredFilters(
      auditFilterScope,
      toFilterSet({
        ...filters,
        ...persistedFilters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      auditFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader description={copy.pageDescription} title={copy.pageTitle} />

      <FilterBar
        activeCount={activeFilterCount}
        clearLabel={copy.clearFilters}
        label={copy.filters}
        onClear={clearFilters}
        summary={activeFilterCount > 0 ? copy.activeFilters(activeFilterCount) : undefined}
      >
        <SearchInput
          clearLabel={copy.clearFilters}
          label={copy.search}
          onClear={() => updateFilters({ search: '' })}
          onValueChange={(search) => updateFilters({ search })}
          placeholder={copy.searchPlaceholder}
          value={filters.search}
        />
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.columns.category}</span>
          <select
            aria-label={copy.columns.category}
            onChange={(event) => updateFilters({ category: event.currentTarget.value })}
            value={filters.category}
          >
            {categoryOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.columns.actor}</span>
          <TextInput
            aria-label={copy.columns.actor}
            onChange={(event) => updateFilters({ actor: event.currentTarget.value })}
            value={filters.actor ?? ''}
          />
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.entityType}</span>
          <TextInput
            aria-label={copy.entityType}
            onChange={(event) => updateFilters({ entityType: event.currentTarget.value })}
            value={filters.entityType ?? ''}
          />
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.targetId}</span>
          <TextInput
            aria-label={copy.targetId}
            onChange={(event) => updateFilters({ entityId: event.currentTarget.value })}
            value={filters.entityId ?? ''}
          />
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.from}</span>
          <TextInput
            aria-label={copy.from}
            onChange={(event) => updateFilters({ from: event.currentTarget.value })}
            type="date"
            value={filters.from ?? ''}
          />
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.to}</span>
          <TextInput
            aria-label={copy.to}
            onChange={(event) => updateFilters({ to: event.currentTarget.value })}
            type="date"
            value={filters.to ?? ''}
          />
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.allSorts}</span>
          <select
            aria-label={copy.allSorts}
            onChange={(event) => updateFilters({ sort: event.currentTarget.value })}
            value={filters.sort}
          >
            {sortOptions.map((option) => (
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
              <div style={{ display: 'grid', gap: 4 }}>
                <strong>{row.actionLabel}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>{row.action}</span>
              </div>
            ),
            header: copy.columns.action,
            id: 'action',
          },
          {
            cell: (row) => (
              <StatusBadge label={row.category.label} tone={getBadgeTone(row.category.tone)} />
            ),
            header: copy.columns.category,
            id: 'category',
            width: 140,
          },
          {
            cell: (row) => (
              <div style={{ display: 'grid', gap: 4 }}>
                <strong>{row.actorDisplayName}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {row.actorKind}
                </span>
              </div>
            ),
            header: copy.columns.actor,
            id: 'actor',
          },
          {
            cell: (row) => (
              <div style={{ display: 'grid', gap: 4, minWidth: 0 }}>
                <strong>{row.targetDisplayName}</strong>
                <span
                  style={{
                    color: 'var(--als-color-text-muted, #64748b)',
                    overflowWrap: 'anywhere',
                  }}
                >
                  {row.targetEntityType} / {row.targetEntityId}
                </span>
              </div>
            ),
            header: copy.columns.target,
            id: 'target',
          },
          {
            cell: (row) => formatDateTime(row.occurredAt, { locale }),
            header: copy.columns.occurredAt,
            id: 'occurredAt',
            width: 150,
          },
          {
            align: 'right',
            cell: (row) => (
              <button
                aria-label={`${copy.view} ${row.actionLabel}`}
                onClick={() => setSelectedEntry(row)}
                style={iconButtonStyle}
                title={copy.view}
                type="button"
              >
                <Eye aria-hidden="true" size={16} />
              </button>
            ),
            header: copy.view,
            id: 'actions',
            width: 90,
          },
        ]}
        emptyState={<EmptyState title={copy.empty} />}
        errorState={
          auditQuery.isError ? (
            <ErrorState title={copy.error} onRetry={() => void auditQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={auditQuery.isLoading}
        loadingLabel={copy.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={auditQuery.data?.totalItems ?? 0}
      />

      {selectedEntry ? (
        <section
          aria-label={copy.details.selected}
          style={{
            background: 'var(--als-color-surface, #ffffff)',
            border: '1px solid var(--als-color-border, #d7deea)',
            borderRadius: 'var(--als-radius-md, 8px)',
            display: 'grid',
            gap: 14,
            padding: 16,
          }}
        >
          <div
            style={{
              alignItems: 'start',
              display: 'flex',
              gap: 12,
              justifyContent: 'space-between',
            }}
          >
            <div style={{ display: 'grid', gap: 4 }}>
              <strong>{selectedEntry.actionLabel}</strong>
              <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                {formatDateTime(selectedEntry.occurredAt, { locale })} /{' '}
                {selectedEntry.targetDisplayName}
              </span>
            </div>
            <button onClick={() => setSelectedEntry(null)} type="button">
              {copy.clearFilters}
            </button>
          </div>
          <div
            style={{
              display: 'grid',
              gap: 14,
              gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
            }}
          >
            <section>
              <h3 style={{ fontSize: '1rem', margin: '0 0 8px' }}>{copy.details.changedFields}</h3>
              {formatDictionary(selectedEntry.changedFields, copy.details.empty)}
            </section>
            <section>
              <h3 style={{ fontSize: '1rem', margin: '0 0 8px' }}>{copy.details.context}</h3>
              {formatDictionary(selectedEntry.context, copy.details.empty)}
            </section>
            <section>
              <h3 style={{ fontSize: '1rem', margin: '0 0 8px' }}>{copy.details.correlation}</h3>
              <p style={{ margin: 0, overflowWrap: 'anywhere' }}>
                {selectedEntry.correlationId ?? copy.details.empty}
              </p>
            </section>
          </div>
        </section>
      ) : null}
    </section>
  )
}
