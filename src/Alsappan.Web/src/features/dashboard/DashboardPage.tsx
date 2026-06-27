import { useQuery } from '@tanstack/react-query'
import { ArrowRight, Building2, Clock3, LockKeyhole, RefreshCw } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { ActionButton, EmptyState, ErrorState, LoadingState, PageHeader } from '../../components'
import type { AppLocale } from '../../i18n'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { getDashboardOverview, type DashboardMetric } from '../../lib/api/dashboard'
import { formatDateTime, formatMoney } from '../../lib/format'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getDashboardCopy } from './dashboardCopy'

const metricToneColors: Record<string, string> = {
  danger: 'var(--als-color-danger, #b42318)',
  info: 'var(--als-color-primary, #1877f2)',
  neutral: 'var(--als-color-text-muted, #64748b)',
  success: 'var(--als-color-success, #12805c)',
  warning: 'var(--als-color-warning, #b54708)',
}

function getMetricDisplay(metric: DashboardMetric, locale: AppLocale) {
  if (!metric.isVisible) {
    return metric.hiddenReason ?? ''
  }

  if (metric.key === 'overduePayments' && metric.metadata.totalBalance) {
    const amount = Number(metric.metadata.totalBalance)
    const currency = metric.metadata.currency ?? 'BRL'
    const formatted = Number.isFinite(amount)
      ? formatMoney(amount, { currency, locale })
      : undefined

    return formatted
      ? `${metric.displayValue ?? metric.value ?? 0} - ${formatted}`
      : metric.displayValue
  }

  return metric.displayValue ?? String(metric.value ?? 0)
}

function MetricWidget({ metric, locale }: { locale: AppLocale; metric: DashboardMetric }) {
  const toneColor = metricToneColors[metric.tone] ?? metricToneColors.neutral
  const content = (
    <>
      <span
        aria-hidden="true"
        style={{
          background: 'color-mix(in srgb, currentColor 12%, transparent)',
          borderRadius: 8,
          color: toneColor,
          display: 'inline-grid',
          height: 38,
          placeItems: 'center',
          width: 38,
        }}
      >
        {metric.isVisible ? <Building2 size={18} /> : <LockKeyhole size={18} />}
      </span>
      <span style={{ display: 'grid', gap: 5, minWidth: 0 }}>
        <strong style={{ fontSize: '0.95rem' }}>{metric.label}</strong>
        <span
          style={{
            color: 'var(--als-color-text-muted, #64748b)',
            fontSize: '0.84rem',
            lineHeight: 1.35,
          }}
        >
          {metric.description}
        </span>
      </span>
      <strong
        style={{
          color: metric.isVisible ? toneColor : 'var(--als-color-text-muted, #64748b)',
          fontSize: '1.55rem',
          lineHeight: 1.1,
        }}
      >
        {metric.isVisible ? getMetricDisplay(metric, locale) : '---'}
      </strong>
      {metric.isVisible ? (
        <span style={{ alignItems: 'center', display: 'inline-flex', fontWeight: 800, gap: 6 }}>
          <ArrowRight aria-hidden="true" size={16} />
        </span>
      ) : (
        <span style={{ color: 'var(--als-color-text-muted, #64748b)', fontSize: '0.85rem' }}>
          {metric.hiddenReason}
        </span>
      )}
    </>
  )

  const style = {
    alignContent: 'start',
    border: '1px solid var(--als-color-border, #d7deea)',
    borderRadius: 8,
    color: 'inherit',
    display: 'grid',
    gap: 12,
    minHeight: 178,
    padding: 16,
    textAlign: 'left' as const,
  }

  return metric.isVisible ? (
    <Link className="dashboard-metric" style={style} to={metric.route}>
      {content}
    </Link>
  ) : (
    <article className="dashboard-metric" style={style}>
      {content}
    </article>
  )
}

export function DashboardPage() {
  const apiClient = useApiClient()
  const navigate = useNavigate()
  const activeOrganizationId = useActiveOrganizationStore((state) => state.activeOrganizationId)
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getDashboardCopy(locale)
  const dashboardQuery = useQuery({
    queryFn: () => getDashboardOverview(apiClient, locale),
    queryKey: ['dashboard', activeOrganizationId, locale],
    staleTime: 30_000,
  })
  const overview = dashboardQuery.data
  const generatedAt = overview?.generatedAt
    ? formatDateTime(overview.generatedAt, { locale })
    : undefined
  const header = (
    <PageHeader
      description={copy.pageDescription}
      eyebrow={generatedAt ? `${copy.dashboardUpdated}: ${generatedAt}` : undefined}
      title={copy.pageTitle}
      actions={
        <ActionButton
          icon={<RefreshCw aria-hidden="true" size={16} />}
          isLoading={dashboardQuery.isFetching}
          loadingLabel={copy.loading}
          onClick={() => void dashboardQuery.refetch()}
        >
          {copy.retry}
        </ActionButton>
      }
    />
  )

  if (dashboardQuery.isLoading) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        {header}
        <LoadingState title={copy.loading} />
      </section>
    )
  }

  if (dashboardQuery.isError) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        {header}
        <ErrorState
          retryLabel={copy.retry}
          title={copy.error}
          onRetry={() => void dashboardQuery.refetch()}
        />
      </section>
    )
  }

  const activity = overview?.recentActivity

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      {header}

      {overview?.emptyState ? (
        <EmptyState
          action={{
            icon: <ArrowRight aria-hidden="true" size={16} />,
            label: overview.emptyState.actionLabel || copy.emptyAction,
            onClick: () => {
              navigate(overview.emptyState?.route ?? '/imoveis')
            },
          }}
          description={overview.emptyState.description}
          title={overview.emptyState.title}
        />
      ) : null}

      <div
        className="dashboard-metric-grid"
        style={{
          display: 'grid',
          gap: 14,
          gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))',
        }}
      >
        {(overview?.metrics ?? []).map((metric) => (
          <MetricWidget key={metric.key} locale={locale} metric={metric} />
        ))}
      </div>

      <section
        style={{
          border: '1px solid var(--als-color-border, #d7deea)',
          borderRadius: 8,
          display: 'grid',
          gap: 12,
          padding: 16,
        }}
      >
        <header
          style={{
            alignItems: 'center',
            display: 'flex',
            flexWrap: 'wrap',
            gap: 12,
            justifyContent: 'space-between',
          }}
        >
          <div style={{ display: 'grid', gap: 4 }}>
            <h2 style={{ fontSize: '1.05rem', margin: 0 }}>{activity?.label}</h2>
            <p style={{ color: 'var(--als-color-text-muted, #64748b)', margin: 0 }}>
              {activity?.description}
            </p>
          </div>
          {activity?.isVisible ? (
            <Link
              style={{ alignItems: 'center', display: 'inline-flex', fontWeight: 800, gap: 6 }}
              to={activity.route}
            >
              {copy.activityOpen}
              <ArrowRight aria-hidden="true" size={16} />
            </Link>
          ) : null}
        </header>

        {!activity?.isVisible ? (
          <EmptyState
            description={activity?.hiddenReason ?? copy.activityHidden}
            style={{ minHeight: 160 }}
            title={copy.hiddenMetric}
          />
        ) : activity.items.length === 0 ? (
          <EmptyState style={{ minHeight: 160 }} title={copy.activityEmpty} />
        ) : (
          <div style={{ display: 'grid', gap: 8 }}>
            {activity.items.map((item) => (
              <Link
                key={item.id}
                style={{
                  alignItems: 'center',
                  border: '1px solid var(--als-color-border, #d7deea)',
                  borderRadius: 8,
                  display: 'grid',
                  gap: 8,
                  gridTemplateColumns: '36px minmax(0, 1fr) auto',
                  padding: 10,
                }}
                to={item.route ?? activity.route}
              >
                <span
                  aria-hidden="true"
                  style={{
                    background: 'var(--als-color-surface-muted, #f8fafc)',
                    borderRadius: 8,
                    display: 'grid',
                    height: 36,
                    placeItems: 'center',
                    width: 36,
                  }}
                >
                  <Clock3 size={16} />
                </span>
                <span style={{ display: 'grid', gap: 3, minWidth: 0 }}>
                  <strong>{item.summary}</strong>
                  <span
                    style={{ color: 'var(--als-color-text-muted, #64748b)', fontSize: '0.86rem' }}
                  >
                    {formatDateTime(item.occurredAt, { locale })}
                  </span>
                </span>
                <ArrowRight aria-hidden="true" size={16} />
              </Link>
            ))}
          </div>
        )}
      </section>
    </section>
  )
}
