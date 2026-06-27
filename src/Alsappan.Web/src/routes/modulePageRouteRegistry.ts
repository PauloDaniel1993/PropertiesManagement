import type { ComponentType } from 'react'
import type { AdminRouteId } from '../navigation/menuContract'

export type ModulePageRouteContribution = Readonly<{
  Component: ComponentType
  routeId: AdminRouteId
}>

type ModulePageRouteModule = Readonly<{
  modulePageRoute: ModulePageRouteContribution
}>

const modulePageRouteModules = import.meta.glob<ModulePageRouteModule>(
  '../features/**/modulePageRoute.tsx',
  { eager: true },
)

export const modulePageRouteContributions = Object.values(modulePageRouteModules)
  .map((module) => module.modulePageRoute)
  .sort((left, right) => left.routeId.localeCompare(right.routeId))

const routeComponents = new Map<AdminRouteId, ComponentType>()

for (const contribution of modulePageRouteContributions) {
  if (routeComponents.has(contribution.routeId)) {
    throw new Error(`Duplicate module page route contribution for '${contribution.routeId}'.`)
  }

  routeComponents.set(contribution.routeId, contribution.Component)
}

export const implementedModuleRouteIds = [...routeComponents.keys()].sort()

export function getModulePageComponent(routeId: AdminRouteId) {
  return routeComponents.get(routeId)
}
