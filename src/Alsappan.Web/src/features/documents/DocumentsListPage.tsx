import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, Download, Eye, FileUp, Pencil, RotateCcw } from 'lucide-react'
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
} from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import {
  archiveDocument,
  documentLinkedEntityTypes,
  downloadDocument,
  listAllowedDocumentFileTypes,
  listDocumentCategoryOptions,
  listDocuments,
  restoreDocument,
  updateDocument,
  uploadDocument,
  type DocumentDetail,
  type DocumentListFilters,
  type DocumentListItem,
  type DocumentLinkedEntityType,
} from '../../lib/api/documents'
import { formatDateTime } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { getDocumentCopy } from './documentCopy'
import { DocumentDetailPage } from './DocumentDetailPage'
import { DocumentForm, type DocumentFormMode, type DocumentFormSubmitValue } from './DocumentForm'
import { formatFileSize, getDocumentStatusTone } from './documentFormat'
import { saveDownloadedDocument } from './downloadFile'

const documentFilterScope = 'documents.list'

const defaultFilters: DocumentListFilters = {
  category: '',
  includeArchived: false,
  linkedEntityId: '',
  linkedEntityType: '',
  page: 1,
  pageSize: 10,
  search: '',
  uploadedByUserId: '',
  uploadedFrom: '',
  uploadedTo: '',
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
  document?: Partial<DocumentDetail> & Pick<DocumentListItem, 'id'>
  mode: DocumentFormMode
}

type IconActionButtonProps = {
  icon: ReactNode
  isDestructive?: boolean
  label: string
  onClick: () => void
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

function isLinkedEntityType(value: string): value is DocumentLinkedEntityType {
  return documentLinkedEntityTypes.includes(value as DocumentLinkedEntityType)
}

function normalizeDocumentFilters(filters: FilterSet | undefined): DocumentListFilters {
  const linkedEntityType = getStringFilter(filters?.linkedEntityType)

  return {
    category: getStringFilter(filters?.category) as DocumentListFilters['category'],
    includeArchived: getBooleanFilter(filters?.includeArchived),
    linkedEntityId: getStringFilter(filters?.linkedEntityId),
    linkedEntityType: isLinkedEntityType(linkedEntityType) ? linkedEntityType : '',
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    search: getStringFilter(filters?.search),
    uploadedByUserId: getStringFilter(filters?.uploadedByUserId),
    uploadedFrom: getStringFilter(filters?.uploadedFrom),
    uploadedTo: getStringFilter(filters?.uploadedTo),
  }
}

function toFilterSet(filters: DocumentListFilters): FilterSet {
  const nextFilters: FilterSet = {
    includeArchived: filters.includeArchived ?? false,
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  if (filters.category) {
    nextFilters.category = filters.category
  }

  if (filters.linkedEntityId) {
    nextFilters.linkedEntityId = filters.linkedEntityId
  }

  if (filters.linkedEntityType) {
    nextFilters.linkedEntityType = filters.linkedEntityType
  }

  if (filters.search) {
    nextFilters.search = filters.search
  }

  if (filters.uploadedByUserId) {
    nextFilters.uploadedByUserId = filters.uploadedByUserId
  }

  if (filters.uploadedFrom) {
    nextFilters.uploadedFrom = filters.uploadedFrom
  }

  if (filters.uploadedTo) {
    nextFilters.uploadedTo = filters.uploadedTo
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

function getInitialDocument(document: DocumentDetail | DocumentListItem): FormState['document'] {
  return {
    category: document.category,
    concurrencyToken: document.concurrencyToken,
    description: document.description,
    id: document.id,
    links: document.links,
    title: document.title,
  }
}

function getLinkedEntityRouteFilter(filters: URLSearchParams): Partial<DocumentListFilters> {
  const routeFilters: Partial<DocumentListFilters> = {}
  const category = filters.get('category')
  if (category) {
    routeFilters.category = category as DocumentListFilters['category']
  }

  const entityType = filters.get('entityType')
  const entityId = filters.get('entityId')

  if (entityType && entityId && isLinkedEntityType(entityType)) {
    return { ...routeFilters, linkedEntityId: entityId, linkedEntityType: entityType }
  }

  for (const [shortcut, linkedEntityType] of [
    ['propertyId', 'property'],
    ['contractId', 'contract'],
    ['residentId', 'resident'],
  ] as const) {
    const shortcutValue = filters.get(shortcut)
    if (shortcutValue) {
      return { ...routeFilters, linkedEntityId: shortcutValue, linkedEntityType }
    }
  }

  return routeFilters
}

export function DocumentsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[documentFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getDocumentCopy(locale)
  const canWriteDocuments = hasAnyPermission(['documents.write'], authUser)
  const canArchiveDocuments = hasAnyPermission(['documents.archive'], authUser)
  const filters = useMemo(() => {
    const routeFilters =
      typeof window === 'undefined'
        ? {}
        : getLinkedEntityRouteFilter(new URLSearchParams(window.location.search))

    return {
      ...normalizeDocumentFilters(storedFilters),
      ...routeFilters,
    }
  }, [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      includeArchived: Boolean(filters.includeArchived),
      locale,
    }),
    [filters, locale],
  )
  const [formState, setFormState] = useState<FormState | null>(null)
  const [detailDocumentId, setDetailDocumentId] = useState<string | null>(null)
  const documentsQuery = useQuery({
    queryFn: () => listDocuments(apiClient, apiFilters),
    queryKey: ['documents', 'list', apiFilters],
  })
  const categoryOptionsQuery = useQuery({
    queryFn: () => listDocumentCategoryOptions(apiClient, locale),
    queryKey: ['documents', 'category-options', locale],
  })
  const allowedFileTypesQuery = useQuery({
    queryFn: () => listAllowedDocumentFileTypes(apiClient, locale),
    queryKey: ['documents', 'allowed-file-types', locale],
  })
  const rows = documentsQuery.data?.items ?? []
  const page = documentsQuery.data?.page ?? filters.page ?? 1
  const pageSize = documentsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const categoryOptions = useMemo(
    () => categoryOptionsQuery.data ?? [],
    [categoryOptionsQuery.data],
  )
  const categoryFilterOptions = useMemo(
    () => [{ label: copy.list.allCategories, value: '' }, ...categoryOptions],
    [categoryOptions, copy],
  )
  const linkedTypeOptions = useMemo(
    () => [
      { label: copy.list.allLinkedTypes, value: '' },
      ...documentLinkedEntityTypes.map((entityType) => ({
        label: copy.terms.entityTypes[entityType],
        value: entityType,
      })),
    ],
    [copy],
  )
  const allowedFileTypes = (allowedFileTypesQuery.data ?? []).map((option) => option.value)
  const activeFilterCount = [
    filters.search,
    filters.category,
    filters.linkedEntityType,
    filters.linkedEntityId,
    filters.uploadedByUserId,
    filters.uploadedFrom,
    filters.uploadedTo,
    filters.includeArchived ? 'true' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const formMutation = useMutation({
    mutationFn: (value: DocumentFormSubmitValue) => {
      if (value.mode === 'edit' && formState?.document) {
        return updateDocument(apiClient, formState.document.id, value.values, locale)
      }

      if (value.mode === 'create') {
        return uploadDocument(apiClient, value.values, locale)
      }

      throw new Error('Invalid document form state.')
    },
    onSuccess: async () => {
      setFormState(null)
      await queryClient.invalidateQueries({ queryKey: ['documents'] })
    },
  })
  const downloadMutation = useMutation({
    mutationFn: (documentId: string) => downloadDocument(apiClient, documentId),
    onSuccess: (download) => saveDownloadedDocument(download),
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: { action: 'archive' | 'restore'; id: string }) => {
      if (request.action === 'archive') {
        await archiveDocument(apiClient, request.id)
        return
      }

      await restoreDocument(apiClient, request.id, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['documents'] })
    },
  })

  function updateFilters(nextFilters: Partial<DocumentListFilters>) {
    setStoredFilters(
      documentFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      documentFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: DocumentListItem) {
    const actions: ReactNode[] = [
      <IconActionButton
        key="view"
        icon={<Eye aria-hidden="true" size={16} />}
        label={`${copy.list.view} ${row.title}`}
        onClick={() => setDetailDocumentId(row.id)}
      />,
      <IconActionButton
        key="download"
        icon={<Download aria-hidden="true" size={16} />}
        label={`${copy.list.download} ${row.title}`}
        onClick={() => downloadMutation.mutate(row.id)}
      />,
    ]

    if (canWriteDocuments && !row.isArchived) {
      actions.push(
        <IconActionButton
          key="edit"
          icon={<Pencil aria-hidden="true" size={16} />}
          label={`${copy.list.edit} ${row.title}`}
          onClick={() =>
            setFormState({
              document: getInitialDocument(row),
              mode: 'edit',
            })
          }
        />,
      )
    }

    if (canArchiveDocuments) {
      actions.push(
        row.isArchived ? (
          <IconActionButton
            key="restore"
            icon={<RotateCcw aria-hidden="true" size={16} />}
            label={`${copy.list.restore} ${row.title}`}
            onClick={() => lifecycleMutation.mutate({ action: 'restore', id: row.id })}
          />
        ) : (
          <IconActionButton
            key="archive"
            icon={<Archive aria-hidden="true" size={16} />}
            isDestructive
            label={`${copy.list.archive} ${row.title}`}
            onClick={() => lifecycleMutation.mutate({ action: 'archive', id: row.id })}
          />
        ),
      )
    }

    return actions
  }

  if (detailDocumentId) {
    return (
      <DocumentDetailPage
        documentId={detailDocumentId}
        onBack={() => setDetailDocumentId(null)}
        onEdit={
          canWriteDocuments
            ? (document) => {
                setDetailDocumentId(null)
                setFormState({
                  document: getInitialDocument(document),
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
          canWriteDocuments
            ? {
                icon: <FileUp aria-hidden="true" size={18} />,
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
          <span style={{ fontWeight: 800 }}>{copy.list.columns.category}</span>
          <select
            aria-label={copy.list.columns.category}
            onChange={(event) =>
              updateFilters({
                category: event.currentTarget.value as DocumentListFilters['category'],
              })
            }
            value={filters.category}
          >
            {categoryFilterOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.form.entityType}</span>
          <select
            aria-label={copy.form.entityType}
            onChange={(event) =>
              updateFilters({
                linkedEntityType: event.currentTarget.value as DocumentLinkedEntityType | '',
              })
            }
            value={filters.linkedEntityType}
          >
            {linkedTypeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <TextInput
          aria-label={copy.list.linkedEntityId}
          onChange={(event) => updateFilters({ linkedEntityId: event.currentTarget.value })}
          placeholder={copy.list.linkedEntityId}
          style={{ maxWidth: 210 }}
          value={filters.linkedEntityId}
        />
        <TextInput
          aria-label={copy.list.ownerId}
          onChange={(event) => updateFilters({ uploadedByUserId: event.currentTarget.value })}
          placeholder={copy.list.ownerId}
          style={{ maxWidth: 210 }}
          value={filters.uploadedByUserId}
        />
        <TextInput
          aria-label={copy.list.uploadedFrom}
          onChange={(event) => updateFilters({ uploadedFrom: event.currentTarget.value })}
          style={{ maxWidth: 150 }}
          type="date"
          value={filters.uploadedFrom}
        />
        <TextInput
          aria-label={copy.list.uploadTo}
          onChange={(event) => updateFilters({ uploadedTo: event.currentTarget.value })}
          style={{ maxWidth: 150 }}
          type="date"
          value={filters.uploadedTo}
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
                <strong>{row.title}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                  {row.fileName} - {formatFileSize(row.sizeBytes, locale)}
                </span>
              </div>
            ),
            header: copy.list.columns.document,
            id: 'document',
          },
          {
            cell: (row) => row.categoryLabel ?? copy.terms.categories[row.category],
            header: copy.list.columns.category,
            id: 'category',
            width: 170,
          },
          {
            cell: (row) => copy.terms.linksCount(row.links.length),
            header: copy.list.columns.links,
            id: 'links',
            width: 130,
          },
          {
            cell: (row) => (
              <StatusBadge
                label={row.status.label ?? copy.terms.status[row.status.code]}
                tone={getDocumentStatusTone(row.status)}
              />
            ),
            header: copy.list.columns.status,
            id: 'status',
            width: 140,
          },
          {
            cell: (row) => formatDateTime(row.uploadedAt, { locale }),
            header: copy.list.columns.uploadedAt,
            id: 'uploadedAt',
            width: 180,
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
            width: 210,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          documentsQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void documentsQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={documentsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={documentsQuery.data?.totalItems ?? 0}
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
          <DocumentForm
            allowedFileTypes={allowedFileTypes}
            categoryOptions={categoryOptions}
            initialValue={formState.document}
            isSubmitting={formMutation.isPending}
            mode={formState.mode}
            onCancel={() => setFormState(null)}
            onSubmit={async (value) => {
              await formMutation.mutateAsync(value)
            }}
          />
        ) : null}
      </Drawer>
    </section>
  )
}
