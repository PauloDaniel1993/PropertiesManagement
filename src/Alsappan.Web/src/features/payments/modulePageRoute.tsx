import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { PaymentsListPage } from './PaymentsListPage'

export const modulePageRoute = {
  Component: PaymentsListPage,
  routeId: 'payments',
} satisfies ModulePageRouteContribution
