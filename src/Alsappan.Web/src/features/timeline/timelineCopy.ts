import type { AppLocale } from '../../i18n'
import type { TimelineEntityType } from '../../lib/api/timeline'

export type TimelineCopy = ReturnType<typeof getTimelineCopy>

const entityLabels: Record<AppLocale, Record<TimelineEntityType, string>> = {
  'en-US': {
    contract: 'Contract',
    document: 'Document',
    identityUser: 'Administrator',
    inspection: 'Inspection',
    occurrence: 'Occurrence',
    payment: 'Payment',
    pet: 'Pet',
    property: 'Property',
    resident: 'Resident',
    utilityAccount: 'Utility account',
    vehicle: 'Vehicle',
  },
  'pt-BR': {
    contract: 'Contrato',
    document: 'Documento',
    identityUser: 'Administrador',
    inspection: 'Vistoria',
    occurrence: 'Ocorrencia',
    payment: 'Pagamento',
    pet: 'Pet',
    property: 'Imovel',
    resident: 'Morador',
    utilityAccount: 'Conta de consumo',
    vehicle: 'Veiculo',
  },
}

const copies = {
  'en-US': {
    activeFilters: (count: number) => `${count} active filter${count === 1 ? '' : 's'}`,
    actor: 'Actor',
    actorUserId: 'Actor user ID',
    allEntityTypes: 'All entities',
    clearFilters: 'Clear filters',
    deepLink: 'Open source record',
    empty: 'No timeline events',
    entityContext: (label: string) => `${label} timeline`,
    entityTypes: entityLabels['en-US'],
    error: 'Could not load timeline',
    eventType: 'Event type',
    filters: 'Filters',
    from: 'From',
    loading: 'Loading timeline...',
    pageDescription: 'Chronological activity across operational records.',
    pageTitle: 'Timeline',
    pagination: {
      next: 'Next',
      page: (page: number, totalPages: number) => `Page ${page} of ${totalPages}`,
      previous: 'Previous',
      range: (first: number, last: number, total: number) => `Showing ${first}-${last} of ${total}`,
    },
    panel: {
      title: 'Timeline',
    },
    related: 'Related',
    relatedEntityId: 'Related entity ID',
    relatedEntityType: 'Related entity',
    retry: 'Try again',
    subjectEntityType: 'Entity',
    systemActor: 'System',
    to: 'To',
  },
  'pt-BR': {
    activeFilters: (count: number) => `${count} filtro(s) ativo(s)`,
    actor: 'Ator',
    actorUserId: 'ID do usuario',
    allEntityTypes: 'Todas as entidades',
    clearFilters: 'Limpar filtros',
    deepLink: 'Abrir registro de origem',
    empty: 'Nenhum evento na timeline',
    entityContext: (label: string) => `Timeline de ${label}`,
    entityTypes: entityLabels['pt-BR'],
    error: 'Nao foi possivel carregar a timeline',
    eventType: 'Tipo de evento',
    filters: 'Filtros',
    from: 'De',
    loading: 'Carregando timeline...',
    pageDescription: 'Atividades em ordem cronologica dos registros operacionais.',
    pageTitle: 'Timeline',
    pagination: {
      next: 'Proxima',
      page: (page: number, totalPages: number) => `Pagina ${page} de ${totalPages}`,
      previous: 'Anterior',
      range: (first: number, last: number, total: number) =>
        `Exibindo ${first}-${last} de ${total}`,
    },
    panel: {
      title: 'Timeline',
    },
    related: 'Relacionados',
    relatedEntityId: 'ID relacionado',
    relatedEntityType: 'Entidade relacionada',
    retry: 'Tentar novamente',
    subjectEntityType: 'Entidade',
    systemActor: 'Sistema',
    to: 'Ate',
  },
} as const

export function getTimelineCopy(locale: AppLocale = 'pt-BR') {
  return copies[locale] ?? copies['pt-BR']
}
