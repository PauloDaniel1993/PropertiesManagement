import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { NotificationsListPage } from './NotificationsListPage'

export const modulePageRoute = {
  Component: NotificationsListPage,
  routeId: 'notifications',
} satisfies ModulePageRouteContribution
