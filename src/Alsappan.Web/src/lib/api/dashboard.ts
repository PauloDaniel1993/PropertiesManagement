import type { AppLocale } from '../../i18n'
import type { ApiClient } from './client'

export type DashboardMetric = {
  description: string
  displayValue?: string
  hiddenReason?: string
  isVisible: boolean
  key: string
  label: string
  metadata: Record<string, string>
  route: string
  tone: 'danger' | 'info' | 'neutral' | 'success' | 'warning' | string
  unit?: string
  value?: number
}

export type DashboardActivity = {
  entityType: string
  entityTypeLabel: string
  eventType: string
  eventTypeLabel: string
  id: string
  occurredAt: string
  route?: string
  subjectDisplayName?: string
  summary: string
}

export type DashboardRecentActivitySection = {
  description: string
  hiddenReason?: string
  isVisible: boolean
  items: DashboardActivity[]
  label: string
  route: string
}

export type DashboardEmptyState = {
  actionLabel: string
  description: string
  route: string
  title: string
}

export type DashboardOverview = {
  emptyState?: DashboardEmptyState
  generatedAt: string
  metrics: DashboardMetric[]
  recentActivity: DashboardRecentActivitySection
}

type ApiDashboardMetric = Omit<DashboardMetric, 'metadata'> & {
  metadata?: Record<string, string>
}

type ApiDashboardOverview = Omit<DashboardOverview, 'metrics' | 'recentActivity'> & {
  metrics?: ApiDashboardMetric[]
  recentActivity?: DashboardRecentActivitySection
}

function mapMetric(metric: ApiDashboardMetric): DashboardMetric {
  return {
    ...metric,
    metadata: metric.metadata ?? {},
  }
}

export async function getDashboardOverview(client: ApiClient, locale?: AppLocale) {
  const overview = await client.get<ApiDashboardOverview>('/v1/dashboard', {
    query: { locale },
  })

  return {
    ...overview,
    metrics: (overview.metrics ?? []).map(mapMetric),
    recentActivity: overview.recentActivity ?? {
      description: '',
      isVisible: false,
      items: [],
      label: '',
      route: '/timeline',
    },
  }
}
