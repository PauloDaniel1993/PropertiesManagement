import type { CSSProperties } from 'react'
import { CalendarDays, Clock3, ExternalLink, Link2, UserRound } from 'lucide-react'
import { EmptyState, ErrorState, LoadingState } from '../../components'
import type { AppLocale } from '../../i18n'
import type { TimelineEntry } from '../../lib/api/timeline'
import { formatDate, formatTime } from '../../lib/format'
import type { TimelineCopy } from './timelineCopy'

type TimelineFeedProps = {
  copy: TimelineCopy
  entries: TimelineEntry[]
  isCompact?: boolean
  isError?: boolean
  isLoading?: boolean
  locale: AppLocale
  onRetry?: () => void
}

type TimelineGroup = {
  dateKey: string
  entries: TimelineEntry[]
}

const entryStyle: CSSProperties = {
  background: 'var(--als-color-surface, #ffffff)',
  border: '1px solid var(--als-color-border, #d7deea)',
  borderRadius: 'var(--als-radius-sm, 6px)',
  display: 'grid',
  gap: 10,
  padding: 14,
}

function groupEntries(entries: TimelineEntry[]): TimelineGroup[] {
  const groups = new Map<string, TimelineEntry[]>()

  for (const entry of entries) {
    const dateKey = entry.occurredAt.slice(0, 10)
    groups.set(dateKey, [...(groups.get(dateKey) ?? []), entry])
  }

  return [...groups.entries()].map(([dateKey, groupEntries]) => ({
    dateKey,
    entries: groupEntries,
  }))
}

function getActorLabel(entry: TimelineEntry, copy: TimelineCopy) {
  return entry.actor.displayName ?? entry.actor.kindLabel ?? copy.systemActor
}

function getSubjectLabel(entry: TimelineEntry) {
  return entry.subject.displayName ?? entry.subject.entityTypeLabel
}

export function TimelineFeed({
  copy,
  entries,
  isCompact = false,
  isError = false,
  isLoading = false,
  locale,
  onRetry,
}: TimelineFeedProps) {
  if (isLoading) {
    return <LoadingState title={copy.loading} />
  }

  if (isError) {
    return <ErrorState retryLabel={copy.retry} title={copy.error} onRetry={onRetry} />
  }

  if (entries.length === 0) {
    return <EmptyState title={copy.empty} />
  }

  return (
    <div style={{ display: 'grid', gap: isCompact ? 14 : 18 }}>
      {groupEntries(entries).map((group) => (
        <section key={group.dateKey} style={{ display: 'grid', gap: 10 }}>
          <h2
            style={{
              alignItems: 'center',
              color: 'var(--als-color-text-muted, #64748b)',
              display: 'inline-flex',
              fontSize: isCompact ? '0.9rem' : '1rem',
              fontWeight: 800,
              gap: 8,
              letterSpacing: 0,
              margin: 0,
            }}
          >
            <CalendarDays aria-hidden="true" size={16} />
            {formatDate(group.dateKey, { locale })}
          </h2>

          <ol style={{ display: 'grid', gap: 10, listStyle: 'none', margin: 0, padding: 0 }}>
            {group.entries.map((entry) => (
              <li key={entry.id} style={entryStyle}>
                <div
                  style={{
                    alignItems: 'flex-start',
                    display: 'grid',
                    gap: 10,
                    gridTemplateColumns: isCompact ? '1fr' : 'minmax(96px, 128px) 1fr',
                  }}
                >
                  <time
                    dateTime={entry.occurredAt}
                    style={{
                      alignItems: 'center',
                      color: 'var(--als-color-text-muted, #64748b)',
                      display: 'inline-flex',
                      fontSize: '0.86rem',
                      fontWeight: 800,
                      gap: 6,
                      whiteSpace: 'nowrap',
                    }}
                  >
                    <Clock3 aria-hidden="true" size={15} />
                    {formatTime(entry.occurredAt, { locale })}
                  </time>

                  <div style={{ display: 'grid', gap: 8, minWidth: 0 }}>
                    <div style={{ display: 'grid', gap: 4 }}>
                      <strong style={{ color: 'var(--als-color-text, #1f2937)' }}>
                        {entry.display.summary}
                      </strong>
                      <span
                        style={{
                          color: 'var(--als-color-text-muted, #64748b)',
                          fontSize: '0.9rem',
                        }}
                      >
                        {entry.eventTypeLabel} | {entry.subject.entityTypeLabel}:{' '}
                        {getSubjectLabel(entry)}
                      </span>
                    </div>

                    <div
                      style={{
                        alignItems: 'center',
                        display: 'flex',
                        flexWrap: 'wrap',
                        gap: 8,
                      }}
                    >
                      <span
                        style={{
                          alignItems: 'center',
                          color: 'var(--als-color-text-muted, #64748b)',
                          display: 'inline-flex',
                          fontSize: '0.86rem',
                          gap: 6,
                        }}
                      >
                        <UserRound aria-hidden="true" size={15} />
                        {copy.actor}: {getActorLabel(entry, copy)}
                      </span>

                      {entry.relatedEntities.length > 0 ? (
                        <span
                          style={{
                            alignItems: 'center',
                            color: 'var(--als-color-text-muted, #64748b)',
                            display: 'inline-flex',
                            flexWrap: 'wrap',
                            fontSize: '0.86rem',
                            gap: 6,
                          }}
                        >
                          <Link2 aria-hidden="true" size={15} />
                          {copy.related}:{' '}
                          {entry.relatedEntities
                            .map(
                              (related) =>
                                related.displayName ??
                                `${related.entityTypeLabel} ${related.entityId}`,
                            )
                            .join(', ')}
                        </span>
                      ) : null}
                    </div>

                    {entry.route ? (
                      <a
                        href={entry.route}
                        style={{
                          alignItems: 'center',
                          color: 'var(--als-color-primary, #1877f2)',
                          display: 'inline-flex',
                          fontSize: '0.9rem',
                          fontWeight: 800,
                          gap: 6,
                          justifySelf: 'start',
                          textDecoration: 'none',
                        }}
                      >
                        <ExternalLink aria-hidden="true" size={15} />
                        {copy.deepLink}
                      </a>
                    ) : null}
                  </div>
                </div>
              </li>
            ))}
          </ol>
        </section>
      ))}
    </div>
  )
}
