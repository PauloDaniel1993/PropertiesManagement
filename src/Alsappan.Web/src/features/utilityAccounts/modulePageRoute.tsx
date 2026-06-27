import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { UtilityAccountsListPage } from './UtilityAccountsListPage'

export const modulePageRoute = {
  Component: UtilityAccountsListPage,
  routeId: 'utilityAccounts',
} satisfies ModulePageRouteContribution
