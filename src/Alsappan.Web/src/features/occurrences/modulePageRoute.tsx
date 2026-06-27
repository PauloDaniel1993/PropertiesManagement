import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { OccurrencesListPage } from './OccurrencesListPage'

export const modulePageRoute = {
  Component: OccurrencesListPage,
  routeId: 'occurrences',
} satisfies ModulePageRouteContribution
