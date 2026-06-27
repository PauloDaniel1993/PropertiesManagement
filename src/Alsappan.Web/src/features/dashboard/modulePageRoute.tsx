import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { DashboardPage } from './DashboardPage'

export const modulePageRoute = {
  Component: DashboardPage,
  routeId: 'dashboard',
} satisfies ModulePageRouteContribution
