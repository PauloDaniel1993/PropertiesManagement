import type { AppLocale } from '../../i18n'

export type DashboardCopy = ReturnType<typeof getDashboardCopy>

const copies = {
  'en-US': {
    activityEmpty: 'No recent activity',
    activityHidden: 'Activity is hidden by your permissions.',
    activityOpen: 'Open timeline',
    dashboardUpdated: 'Updated',
    emptyAction: 'Open properties',
    error: 'Could not load dashboard',
    hiddenMetric: 'Hidden',
    loading: 'Loading dashboard...',
    pageDescription: 'Operational indicators, attention points, and recent activity.',
    pageTitle: 'Dashboard',
    retry: 'Try again',
    viewDetails: 'View details',
  },
  'pt-BR': {
    activityEmpty: 'Nenhuma atividade recente',
    activityHidden: 'Atividade oculta pelas suas permissoes.',
    activityOpen: 'Abrir timeline',
    dashboardUpdated: 'Atualizado',
    emptyAction: 'Abrir imoveis',
    error: 'Nao foi possivel carregar o dashboard',
    hiddenMetric: 'Oculto',
    loading: 'Carregando dashboard...',
    pageDescription: 'Indicadores operacionais, pontos de atencao e atividades recentes.',
    pageTitle: 'Dashboard',
    retry: 'Tentar novamente',
    viewDetails: 'Ver detalhes',
  },
} as const

export function getDashboardCopy(locale: AppLocale = 'pt-BR') {
  return copies[locale] ?? copies['pt-BR']
}
