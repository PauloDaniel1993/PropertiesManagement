import type { ModulePageRouteContribution } from '../../routes/modulePageRouteRegistry'
import { SettingsPage } from './SettingsPage'

export const modulePageRoute = {
  Component: SettingsPage,
  routeId: 'settings',
} satisfies ModulePageRouteContribution
