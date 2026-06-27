import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { VehiclesListPage } from './VehiclesListPage'

export const modulePageRoute = {
  Component: VehiclesListPage,
  routeId: 'vehicles',
} satisfies ModulePageRouteContribution
