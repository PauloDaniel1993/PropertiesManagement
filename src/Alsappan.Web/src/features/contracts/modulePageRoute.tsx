import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { ContractsListPage } from './ContractsListPage'

export const modulePageRoute = {
  Component: ContractsListPage,
  routeId: 'contracts',
} satisfies ModulePageRouteContribution
