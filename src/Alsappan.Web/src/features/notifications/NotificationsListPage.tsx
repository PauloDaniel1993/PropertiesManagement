import { useEffect, useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, Bell, CheckCheck, ExternalLink, MailOpen, Settings } from 'lucide-react'
import {
  ActionButton,
  DataTable,
  Drawer,
  EmptyState,
  ErrorState,
  FilterBar,
  PageHeader,
  Pagination,
  SearchInput,
  StatusBadge,
} from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import {
  archiveNotification,
  getNotificationPreferences,
  getNotificationUnreadCount,
  listNotificationCategoryOptions,
  listNotificationChannelOptions,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  updateNotificationPreferences,
  type NotificationListFilters,
  type NotificationListItem,
  type NotificationPreference,
} from '../../lib/api/notifications'
import { formatDateTime } from '../../lib/format'
import { coerceStatusBadgeTone } from '../../lib/statusBadges'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { type FilterSet, type FilterValue, useFiltersStore } from '../../stores/useFiltersStore'
import { hasAnyPermission } from '../identity/session'
import { getNotificationCopy } from './notificationCopy'

const notificationFilterScope = 'notifications.list'

const defaultFilters: NotificationListFilters = {
  category: '',
  channel: '',
  includeArchived: false,
  isRead: '',
  page: 1,
  pageSize: 10,
  search: '',
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

  if (value === 'true') {
    return true
  }

  if (value === 'false') {
    return false
  }

  return ''
}

function normalizeNotificationFilters(filters: FilterSet | undefined): NotificationListFilters {
  return {
    category: getStringFilter(filters?.category),
    channel: getStringFilter(filters?.channel),
    includeArchived: filters?.includeArchived === true,
    isRead: getBooleanFilter(filters?.isRead),
    page: getPageFilter(filters?.page, defaultFilters.page ?? 1),
    pageSize: getPageFilter(filters?.pageSize, defaultFilters.pageSize ?? 10),
    search: getStringFilter(filters?.search),
  }
}

function toFilterSet(filters: NotificationListFilters): FilterSet {
  const nextFilters: FilterSet = {
    page: filters.page ?? defaultFilters.page ?? 1,
    pageSize: filters.pageSize ?? defaultFilters.pageSize ?? 10,
  }

  if (filters.category) {
    nextFilters.category = filters.category
  }

  if (filters.channel) {
    nextFilters.channel = filters.channel
  }

  if (filters.includeArchived) {
    nextFilters.includeArchived = true
  }

  if (filters.isRead !== undefined && filters.isRead !== '') {
    nextFilters.isRead = filters.isRead
  }

  if (filters.search) {
    nextFilters.search = filters.search
  }

  return nextFilters
}

function getPreferenceKey(preference: Pick<NotificationPreference, 'category' | 'channel'>) {
  return `${preference.category}:${preference.channel}`
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

export function NotificationsListPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const activeOrganizationId = useActiveOrganizationStore((state) => state.activeOrganizationId)
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const storedFilters = useFiltersStore((state) => state.filtersByScope[notificationFilterScope])
  const setStoredFilters = useFiltersStore((state) => state.setFilters)
  const copy = getNotificationCopy(locale)
  const canReadNotifications = hasAnyPermission(['notifications.read'], authUser)
  const filters = useMemo(() => normalizeNotificationFilters(storedFilters), [storedFilters])
  const apiFilters = useMemo(
    () => ({
      ...filters,
      locale,
    }),
    [filters, locale],
  )
  const [isPreferencesOpen, setIsPreferencesOpen] = useState(false)
  const [preferenceDraft, setPreferenceDraft] = useState<Record<string, boolean>>({})
  const notificationsQuery = useQuery({
    queryFn: () => listNotifications(apiClient, apiFilters),
    queryKey: ['notifications', activeOrganizationId, 'list', apiFilters],
  })
  const unreadCountQuery = useQuery({
    queryFn: () => getNotificationUnreadCount(apiClient),
    queryKey: ['notifications', activeOrganizationId, 'unread-count'],
  })
  const categoryOptionsQuery = useQuery({
    queryFn: () => listNotificationCategoryOptions(apiClient, locale),
    queryKey: ['notifications', activeOrganizationId, 'category-options', locale],
  })
  const channelOptionsQuery = useQuery({
    queryFn: () => listNotificationChannelOptions(apiClient, locale),
    queryKey: ['notifications', activeOrganizationId, 'channel-options', locale],
  })
  const preferencesQuery = useQuery({
    enabled: isPreferencesOpen,
    queryFn: () => getNotificationPreferences(apiClient, locale),
    queryKey: ['notifications', activeOrganizationId, 'preferences', locale],
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: {
      action: 'archive' | 'mark-read' | 'mark-all-read'
      id?: string
    }) => {
      if (request.action === 'archive' && request.id) {
        await archiveNotification(apiClient, request.id)
        return
      }

      if (request.action === 'mark-read' && request.id) {
        await markNotificationRead(apiClient, request.id, locale)
        return
      }

      await markAllNotificationsRead(apiClient)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })
  const preferencesMutation = useMutation({
    mutationFn: async () =>
      updateNotificationPreferences(
        apiClient,
        {
          preferences: (preferencesQuery.data?.preferences ?? []).map((preference) => ({
            category: preference.category,
            channel: preference.channel,
            isEnabled: preferenceDraft[getPreferenceKey(preference)] ?? preference.isEnabled,
          })),
        },
        locale,
      ),
    onSuccess: async (preferences) => {
      setPreferenceDraft(
        Object.fromEntries(
          preferences.preferences.map((preference) => [
            getPreferenceKey(preference),
            preference.isEnabled,
          ]),
        ),
      )
      await queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })

  useEffect(() => {
    if (!preferencesQuery.data) {
      return
    }

    setPreferenceDraft(
      Object.fromEntries(
        preferencesQuery.data.preferences.map((preference) => [
          getPreferenceKey(preference),
          preference.isEnabled,
        ]),
      ),
    )
  }, [preferencesQuery.data])

  const activeFilterCount = [
    filters.search,
    filters.category,
    filters.channel,
    filters.isRead === '' ? '' : String(filters.isRead),
    filters.includeArchived ? 'archived' : '',
  ].filter((value) => value !== undefined && value !== '').length
  const page = notificationsQuery.data?.page ?? filters.page ?? 1
  const pageSize = notificationsQuery.data?.pageSize ?? filters.pageSize ?? 10
  const rows = notificationsQuery.data?.items ?? []
  const categoryOptions = useMemo(
    () => [
      { label: copy.list.allCategories, value: '' },
      ...(categoryOptionsQuery.data ?? []).map((option) => ({
        label: option.label,
        value: option.value,
      })),
    ],
    [categoryOptionsQuery.data, copy],
  )
  const channelOptions = useMemo(
    () => [
      { label: copy.list.allChannels, value: '' },
      ...(channelOptionsQuery.data ?? []).map((option) => ({
        label: option.label,
        value: option.value,
      })),
    ],
    [channelOptionsQuery.data, copy],
  )
  const readStateOptions = useMemo(
    () => [
      { label: copy.list.allReadStates, value: '' },
      { label: copy.list.unread, value: 'false' },
      { label: copy.list.read, value: 'true' },
    ],
    [copy],
  )
  const archivedOptions = useMemo(
    () => [
      { label: copy.list.includeArchivedNo, value: 'false' },
      { label: copy.list.includeArchivedYes, value: 'true' },
    ],
    [copy],
  )

  function updateFilters(nextFilters: Partial<NotificationListFilters>) {
    setStoredFilters(
      notificationFilterScope,
      toFilterSet({
        ...filters,
        ...nextFilters,
        page: nextFilters.page ?? 1,
      }),
    )
  }

  function clearFilters() {
    setStoredFilters(
      notificationFilterScope,
      toFilterSet({
        ...defaultFilters,
        page: 1,
        pageSize,
      }),
    )
  }

  function getRowActions(row: NotificationListItem) {
    const actions: ReactNode[] = []

    if (row.deepLink) {
      actions.push(
        <a
          key="source"
          aria-label={`${copy.list.deepLink} ${row.title}`}
          href={row.deepLink}
          style={iconButtonStyle}
          title={copy.list.deepLink}
        >
          <ExternalLink aria-hidden="true" size={16} />
        </a>,
      )
    }

    if (canReadNotifications && !row.isRead) {
      actions.push(
        <IconActionButton
          key="mark-read"
          icon={<MailOpen aria-hidden="true" size={16} />}
          label={`${copy.list.markRead} ${row.title}`}
          onClick={() => lifecycleMutation.mutate({ action: 'mark-read', id: row.id })}
        />,
      )
    }

    if (canReadNotifications) {
      actions.push(
        <IconActionButton
          key="archive"
          icon={<Archive aria-hidden="true" size={16} />}
          isDestructive
          label={`${copy.list.archive} ${row.title}`}
          onClick={() => lifecycleMutation.mutate({ action: 'archive', id: row.id })}
        />,
      )
    }

    return actions
  }

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        description={copy.list.pageDescription}
        eyebrow={copy.list.unreadCount(unreadCountQuery.data?.count ?? 0)}
        title={copy.list.pageTitle}
        actions={
          <>
            <ActionButton
              icon={<Settings aria-hidden="true" size={16} />}
              onClick={() => setIsPreferencesOpen(true)}
            >
              {copy.preferences.open}
            </ActionButton>
            <ActionButton
              disabled={!canReadNotifications}
              icon={<CheckCheck aria-hidden="true" size={16} />}
              isLoading={lifecycleMutation.isPending}
              onClick={() => lifecycleMutation.mutate({ action: 'mark-all-read' })}
            >
              {copy.list.markAllRead}
            </ActionButton>
          </>
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
          <span style={{ fontWeight: 800 }}>{copy.list.categories}</span>
          <select
            aria-label={copy.list.categories}
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
          <span style={{ fontWeight: 800 }}>{copy.list.readState}</span>
          <select
            aria-label={copy.list.readState}
            onChange={(event) =>
              updateFilters({
                isRead:
                  event.currentTarget.value === '' ? '' : event.currentTarget.value === 'true',
              })
            }
            value={filters.isRead === '' ? '' : String(filters.isRead)}
          >
            {readStateOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.channels}</span>
          <select
            aria-label={copy.list.channels}
            onChange={(event) => updateFilters({ channel: event.currentTarget.value })}
            value={filters.channel}
          >
            {channelOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          <span style={{ fontWeight: 800 }}>{copy.list.includeArchived}</span>
          <select
            aria-label={copy.list.includeArchived}
            onChange={(event) =>
              updateFilters({ includeArchived: event.currentTarget.value === 'true' })
            }
            value={String(Boolean(filters.includeArchived))}
          >
            {archivedOptions.map((option) => (
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
                <strong>{row.title}</strong>
                <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>{row.message}</span>
                {row.subjectDisplayName ? (
                  <span
                    style={{ color: 'var(--als-color-text-muted, #64748b)', fontSize: '0.85rem' }}
                  >
                    {row.subjectDisplayName}
                  </span>
                ) : null}
              </div>
            ),
            header: copy.list.columns.notification,
            id: 'notification',
          },
          {
            cell: (row) => (
              <StatusBadge
                icon={<Bell aria-hidden="true" size={14} />}
                label={row.category.label}
                tone={coerceStatusBadgeTone(row.category.tone, 'info')}
              />
            ),
            header: copy.list.columns.category,
            id: 'category',
            width: 150,
          },
          {
            cell: (row) => (
              <StatusBadge
                label={row.channel.label}
                tone={coerceStatusBadgeTone(row.channel.tone, 'neutral')}
              />
            ),
            header: copy.list.columns.channel,
            id: 'channel',
            width: 136,
          },
          {
            cell: (row) => (
              <StatusBadge
                label={row.readState.label}
                tone={coerceStatusBadgeTone(row.readState.tone, row.isRead ? 'neutral' : 'info')}
              />
            ),
            header: copy.list.columns.readState,
            id: 'readState',
            width: 132,
          },
          {
            cell: (row) => formatDateTime(row.occurredAt, { locale }),
            header: copy.list.columns.occurredAt,
            id: 'occurredAt',
            width: 152,
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
            width: 150,
          },
        ]}
        emptyState={<EmptyState title={copy.list.empty} />}
        errorState={
          notificationsQuery.isError ? (
            <ErrorState title={copy.list.error} onRetry={() => void notificationsQuery.refetch()} />
          ) : undefined
        }
        getRowKey={(row) => row.id}
        isLoading={notificationsQuery.isLoading}
        loadingLabel={copy.list.loading}
        rows={rows}
      />

      <Pagination
        labels={copy.pagination}
        onPageChange={(nextPage) => updateFilters({ page: nextPage })}
        page={page}
        pageSize={pageSize}
        totalItems={notificationsQuery.data?.totalItems ?? 0}
      />

      <Drawer
        isOpen={isPreferencesOpen}
        onOpenChange={setIsPreferencesOpen}
        title={copy.preferences.title}
        width={520}
      >
        <div style={{ display: 'grid', gap: 14 }}>
          {(preferencesQuery.data?.preferences ?? []).map((preference) => {
            const key = getPreferenceKey(preference)
            const checked = preferenceDraft[key] ?? preference.isEnabled

            return (
              <label
                key={key}
                style={{
                  alignItems: 'center',
                  display: 'grid',
                  gap: 10,
                  gridTemplateColumns: 'minmax(0, 1fr) auto',
                }}
              >
                <span style={{ display: 'grid', gap: 4 }}>
                  <strong>{preference.categoryLabel}</strong>
                  <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                    {preference.channelLabel}
                  </span>
                </span>
                <input
                  checked={checked}
                  disabled={preference.isMandatory}
                  onChange={(event) =>
                    setPreferenceDraft((current) => ({
                      ...current,
                      [key]: event.currentTarget.checked,
                    }))
                  }
                  type="checkbox"
                />
              </label>
            )
          })}

          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, justifyContent: 'flex-end' }}>
            <ActionButton onClick={() => setIsPreferencesOpen(false)} tone="ghost">
              {copy.preferences.cancel}
            </ActionButton>
            <ActionButton
              disabled={!preferencesQuery.data}
              isLoading={preferencesMutation.isPending}
              onClick={() => preferencesMutation.mutate()}
              tone="primary"
            >
              {copy.preferences.save}
            </ActionButton>
          </div>
        </div>
      </Drawer>
    </section>
  )
}
