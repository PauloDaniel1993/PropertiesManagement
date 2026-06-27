import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { AdministratorsListPage } from './AdministratorsListPage'

export const modulePageRoute = {
  Component: AdministratorsListPage,
  routeId: 'administrators',
} satisfies ModulePageRouteContribution
