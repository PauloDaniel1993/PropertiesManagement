import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { ResidentsListPage } from './ResidentsListPage'

export const modulePageRoute = {
  Component: ResidentsListPage,
  routeId: 'residents',
} satisfies ModulePageRouteContribution
