import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { TimelinePage } from './TimelinePage'

export const modulePageRoute = {
  Component: TimelinePage,
  routeId: 'timeline',
} satisfies ModulePageRouteContribution
