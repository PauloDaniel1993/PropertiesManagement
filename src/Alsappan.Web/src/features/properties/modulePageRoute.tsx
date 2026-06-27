import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { PropertiesListPage } from './PropertiesListPage'

export const modulePageRoute = {
  Component: PropertiesListPage,
  routeId: 'properties',
} satisfies ModulePageRouteContribution
