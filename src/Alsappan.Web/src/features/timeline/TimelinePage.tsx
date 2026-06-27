import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import {
  FilterBar,
  FormField,
  PageHeader,
  Pagination,
  SelectInput,
  TextInput,
} from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import {
  listEntityTimeline,
  listTimeline,
  timelineEntityTypes,
  type TimelineEntityType,
  type TimelineFilters,
} from '../../lib/api/timeline'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { TimelineFeed } from './TimelineFeed'
import { getTimelineCopy } from './timelineCopy'

const timelineFilterScope = 'timeline.list'

const defaultFilters: TimelineFilters = {
  actorUserId: '',
  entityType: '',
  eventType: '',
  from: '',
  page: 1,
  pageSize: 10,
  relatedEntityId: '',
  relatedEntityType: '',
  to: '',
}

const entityQueryParams: Array<{ entityType: TimelineEntityType; paramName: string }> = [
  { entityType: 'property', paramName: 'propertyId' },
  { entityType: 'resident', paramName: 'residentId' },
  { entityType: 'contract', paramName: 'contractId' },
  { entityType: 'payment', paramName: 'paymentId' },
  { entityType: 'utilityAccount', paramName: 'utilityAccountId' },
  { entityType: 'document', paramName: 'documentId' },
  { entityType: 'pet', paramName: 'petId' },
  { entityType: 'vehicle', paramName: 'vehicleId' },
  { entityType: 'occurrence', paramName: 'occurrenceId' },
  { entityType: 'inspection', paramName: 'inspectionId' },
  { entityType: 'identityUser', paramName: 'administratorId' },
]

function isTimelineEntityType(value: FilterValue | undefined): value is TimelineEntityType {
  return typeof value === 'string' && timelineEntityTypes.includes(value as TimelineEntityType)
}

function getStringFilter(value: FilterValue | undefined) {
  return typeof value === 'string' ? value : ''
}

function getNumberFilter(value: FilterValue | undefined, fallback: number) {
  return typeof value === 'number' && Number.isFinite(value) && value > 0 ? value : fallback
}

function normalizeTimelineFilters(filters: FilterSet | undefined): TimelineFilters {
  const entityType = filters?.entityType
  const relatedEntityType = filters?.relatedEntityType

  return {
    actorUserId: getStringFilter(filters?.actorUserId),
    entityType: isTimelineEntityType(entityType) ? entityType : '',
    eventType: getStringFilter(filters?.eventType),
    from: getStringFilter(filters?.from),
    page: getNumberFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getNumberFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    relatedEntityId: getStringFilter(filters?.relatedEntityId),
    relatedEntityType: isTimelineEntityType(relatedEntityType) ? relatedEntityType : '',
    to: getStringFilter(filters?.to),
  }
}

function toFilterSet(filters: TimelineFilters): FilterSet {
  const nextFilters: FilterSet = {
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  for (const key of [
    'actorUserId',
    'entityType',
    'eventType',
    'from',
    'relatedEntityId',
    'relatedEntityType',
    'to',
  ] as const) {
    if (filters[key]) {
      nextFilters[key] = filters[key]
    }
  }

  return nextFilters
}

function getEntityContext(searchParams: URLSearchParams) {
  for (const candidate of entityQueryParams) {
    const entityId = searchParams.get(candidate.paramName)
    if (entityId) {
      return {
        entityId,
        entityType: candidate.entityType,
      }
    }
  }

  return null
}

function toEntityFilters(filters: TimelineFilters): Omit<TimelineFilters, 'entityType'> {
  return {
    actorUserId: filters.actorUserId,
    eventType: filters.eventType,
    from: filters.from,
    page: filters.page,
    pageSize: filters.pageSize,
    relatedEntityId: filters.relatedEntityId,
    relatedEntityType: filters.relatedEntityType,
    to: filters.to,
  }
}

export function TimelinePage() {
  const apiClient = useApiClient()
  const [searchParams] = useSearchParams()
  const locale = useAppPreferencesStore((state) => state.locale)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[timelineFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getTimelineCopy(locale)
  const filters = useMemo(() => normalizeTimelineFilters(storedFilters), [storedFilters])
  const entityContext = useMemo(() => getEntityContext(searchParams), [searchParams])
  const entityLabel = entityContext
    ? (copy.entityTypes[entityContext.entityType] ?? entityContext.entityType)
    : undefined
  const timelineQuery = useQuery({
    queryFn: () =>
      entityContext
        ? listEntityTimeline(
            apiClient,
            entityContext.entityType,
            entityContext.entityId,
            toEntityFilters(filters),
            locale,
          )
        : listTimeline(apiClient, filters, locale),
    queryKey: ['timeline', entityContext, filters, locale],
  })
  const rows = timelineQuery.data?.items ?? []
  const page = timelineQuery.data?.page ?? filters.page ?? 1
  const pageSize = timelineQuery.data?.pageSize ?? filters.pageSize ?? 10
  const activeFilterCount = [
    entityContext ? '' : filters.entityType,
    filters.eventType,
    filters.actorUserId,
    filters.from,
    filters.to,
    filters.relatedEntityType,
    filters.relatedEntityId,
  ].filter(Boolean).length
  const entityOptions = useMemo(
    () => [
      { label: copy.allEntityTypes, value: '' },
      ...timelineEntityTypes.map((entityType) => ({
        label: copy.entityTypes[entityType],
        value: entityType,
      })),
    ],
    [copy],
  )

  function updateFilters(nextFilters: Partial<TimelineFilters>) {
    setStoredFilters(
      timelineFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      timelineFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        title={entityLabel ? copy.entityContext(entityLabel) : copy.pageTitle}
        description={copy.pageDescription}
        eyebrow={entityContext ? entityContext.entityId : undefined}
      />

      <FilterBar
        activeCount={activeFilterCount}
        clearLabel={copy.clearFilters}
        label={copy.filters}
        onClear={clearFilters}
        summary={activeFilterCount > 0 ? copy.activeFilters(activeFilterCount) : undefined}
      >
        {!entityContext ? (
          <FormField label={copy.subjectEntityType} style={{ minWidth: 188 }}>
            {({ id }) => (
              <SelectInput
                id={id}
                aria-label={copy.subjectEntityType}
                onChange={(event) =>
                  updateFilters({
                    entityType: event.currentTarget.value as TimelineEntityType | '',
                  })
                }
                options={entityOptions}
                value={filters.entityType}
              />
            )}
          </FormField>
        ) : null}

        <FormField label={copy.eventType} style={{ minWidth: 180 }}>
          {({ id }) => (
            <TextInput
              id={id}
              aria-label={copy.eventType}
              onChange={(event) => updateFilters({ eventType: event.currentTarget.value })}
              value={filters.eventType}
            />
          )}
        </FormField>

        <FormField label={copy.actorUserId} style={{ minWidth: 210 }}>
          {({ id }) => (
            <TextInput
              id={id}
              aria-label={copy.actorUserId}
              onChange={(event) => updateFilters({ actorUserId: event.currentTarget.value })}
              value={filters.actorUserId}
            />
          )}
        </FormField>

        <FormField label={copy.from} style={{ minWidth: 150 }}>
          {({ id }) => (
            <TextInput
              id={id}
              aria-label={copy.from}
              onChange={(event) => updateFilters({ from: event.currentTarget.value })}
              type="date"
              value={filters.from}
            />
          )}
        </FormField>

        <FormField label={copy.to} style={{ minWidth: 150 }}>
          {({ id }) => (
            <TextInput
              id={id}
              aria-label={copy.to}
              onChange={(event) => updateFilters({ to: event.currentTarget.value })}
              type="date"
              value={filters.to}
            />
          )}
        </FormField>

        <FormField label={copy.relatedEntityType} style={{ minWidth: 188 }}>
          {({ id }) => (
            <SelectInput
              id={id}
              aria-label={copy.relatedEntityType}
              onChange={(event) =>
                updateFilters({
                  relatedEntityType: event.currentTarget.value as TimelineEntityType | '',
                })
              }
              options={entityOptions}
              value={filters.relatedEntityType}
            />
          )}
        </FormField>

        <FormField label={copy.relatedEntityId} style={{ minWidth: 190 }}>
          {({ id }) => (
            <TextInput
              id={id}
              aria-label={copy.relatedEntityId}
              onChange={(event) => updateFilters({ relatedEntityId: event.currentTarget.value })}
              value={filters.relatedEntityId}
            />
          )}
        </FormField>
      </FilterBar>

      <TimelineFeed
        copy={copy}
        entries={rows}
        isError={timelineQuery.isError}
        isLoading={timelineQuery.isLoading}
        locale={locale}
        onRetry={() => void timelineQuery.refetch()}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={timelineQuery.data?.totalItems ?? 0}
      />
    </section>
  )
}
