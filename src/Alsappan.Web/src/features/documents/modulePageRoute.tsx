import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { DocumentsListPage } from './DocumentsListPage'

export const modulePageRoute = {
  Component: DocumentsListPage,
  routeId: 'documents',
} satisfies ModulePageRouteContribution
