import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { AuditListPage } from './AuditListPage'

export const modulePageRoute = {
  Component: AuditListPage,
  routeId: 'audit',
} satisfies ModulePageRouteContribution
