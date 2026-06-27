import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { PetsListPage } from './PetsListPage'

export const modulePageRoute = {
  Component: PetsListPage,
  routeId: 'pets',
} satisfies ModulePageRouteContribution
