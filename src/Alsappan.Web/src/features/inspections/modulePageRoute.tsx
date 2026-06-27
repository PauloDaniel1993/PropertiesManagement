import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { InspectionsListPage } from './InspectionsListPage'

export const modulePageRoute = {
  Component: InspectionsListPage,
  routeId: 'inspections',
} satisfies ModulePageRouteContribution
