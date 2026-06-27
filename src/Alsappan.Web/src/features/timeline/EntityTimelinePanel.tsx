import { useQuery } from '@tanstack/react-query'
import type { CSSProperties } from 'react'
import { Timeline } from 'lucide-react'
import { useApiClient } from '../../lib/api/ApiClientContext'
import {
  listEntityTimeline,
  type TimelineEntityType,
  type TimelineFilters,
} from '../../lib/api/timeline'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { TimelineFeed } from './TimelineFeed'
import { getTimelineCopy } from './timelineCopy'

export type EntityTimelinePanelProps = {
  entityId: string
  entityType: TimelineEntityType | string
  filters?: Omit<TimelineFilters, 'entityType'>
  pageSize?: number
  style?: CSSProperties
  title?: string
}

export function EntityTimelinePanel({
  entityId,
  entityType,
  filters,
  pageSize = 5,
  style,
  title,
}: EntityTimelinePanelProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getTimelineCopy(locale)
  const panelFilters = { page: 1, pageSize, ...filters }
  const timelineQuery = useQuery({
    queryFn: () => listEntityTimeline(apiClient, entityType, entityId, panelFilters, locale),
    queryKey: ['timeline', 'entity-panel', entityType, entityId, panelFilters, locale],
  })

  return (
    <section
      aria-label={title ?? copy.panel.title}
      style={{
        background: 'var(--als-color-surface, #ffffff)',
        border: '1px solid var(--als-color-border, #d7deea)',
        borderRadius: 'var(--als-radius-sm, 6px)',
        display: 'grid',
        gap: 14,
        padding: 16,
        ...style,
      }}
    >
      <h2
        style={{
          alignItems: 'center',
          color: 'var(--als-color-text, #1f2937)',
          display: 'inline-flex',
          fontSize: '1rem',
          gap: 8,
          margin: 0,
        }}
      >
        <Timeline aria-hidden="true" size={18} />
        {title ?? copy.panel.title}
      </h2>

      <TimelineFeed
        copy={copy}
        entries={timelineQuery.data?.items ?? []}
        isCompact
        isError={timelineQuery.isError}
        isLoading={timelineQuery.isLoading}
        locale={locale}
        onRetry={() => void timelineQuery.refetch()}
      />
    </section>
  )
}
