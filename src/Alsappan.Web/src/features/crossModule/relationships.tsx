import type { ReactNode } from 'react'
import type { CurrentUserDto } from '../../lib/api/identity'
import { hasAnyPermission } from '../identity/session'

export type RelationshipSummary = {
  count: number
  label: string
  module: string
  route?: string
}

export type CrossModuleEntityType =
  | 'contract'
  | 'document'
  | 'identityUser'
  | 'inspection'
  | 'occurrence'
  | 'payment'
  | 'pet'
  | 'property'
  | 'resident'
  | 'utility-account'
  | 'utilityAccount'
  | 'vehicle'

export type RelationshipContext = {
  entityId: string
  entityType: CrossModuleEntityType
}

const moduleReadPermissions: Record<string, string> = {
  administrators: 'administrators.read',
  audit: 'audit.read',
  contract: 'contracts.read',
  contracts: 'contracts.read',
  document: 'documents.read',
  documents: 'documents.read',
  identityUser: 'administrators.read',
  inspection: 'inspections.read',
  inspections: 'inspections.read',
  occurrence: 'occurrences.read',
  occurrences: 'occurrences.read',
  payment: 'payments.read',
  payments: 'payments.read',
  pet: 'pets.read',
  pets: 'pets.read',
  properties: 'properties.read',
  property: 'properties.read',
  resident: 'residents.read',
  residents: 'residents.read',
  timeline: 'timeline.read',
  'utility-account': 'utility-accounts.read',
  'utility-accounts': 'utility-accounts.read',
  utilityAccount: 'utility-accounts.read',
  utilities: 'utility-accounts.read',
  vehicle: 'vehicles.read',
  vehicles: 'vehicles.read',
}

const entityShortcutParams: Record<CrossModuleEntityType, string> = {
  contract: 'contractId',
  document: 'documentId',
  identityUser: 'administratorId',
  inspection: 'inspectionId',
  occurrence: 'occurrenceId',
  payment: 'paymentId',
  pet: 'petId',
  property: 'propertyId',
  resident: 'residentId',
  'utility-account': 'utilityAccountId',
  utilityAccount: 'utilityAccountId',
  vehicle: 'vehicleId',
}

const documentEntityTypes: Partial<Record<CrossModuleEntityType, string>> = {
  contract: 'contract',
  inspection: 'inspection',
  occurrence: 'occurrence',
  payment: 'payment',
  pet: 'pet',
  property: 'property',
  resident: 'resident',
  'utility-account': 'utility-account',
  utilityAccount: 'utility-account',
  vehicle: 'vehicle',
}

function normalizeModule(module: string) {
  return module.trim()
}

function getRelationshipPermission(module: string) {
  return moduleReadPermissions[normalizeModule(module)]
}

function getEntityRouteParam(entityType: CrossModuleEntityType) {
  return entityShortcutParams[entityType]
}

function appendQuery(route: string, entries: Array<[string, string | undefined]>) {
  const [path, hash = ''] = route.split('#')
  const [basePath, queryString = ''] = path.split('?')
  const query = new URLSearchParams(queryString)

  entries.forEach(([key, value]) => {
    if (value && !query.has(key)) {
      query.set(key, value)
    }
  })

  const nextQuery = query.toString()
  const nextPath = nextQuery ? `${basePath}?${nextQuery}` : basePath

  return hash ? `${nextPath}#${hash}` : nextPath
}

export function canReadRelationshipModule(module: string, user: CurrentUserDto | null) {
  const permission = getRelationshipPermission(module)

  return permission ? hasAnyPermission([permission], user) : false
}

export function filterRelationshipsByPermission<T extends RelationshipSummary>(
  relationships: T[],
  user: CurrentUserDto | null,
) {
  return relationships.filter((relationship) =>
    canReadRelationshipModule(relationship.module, user),
  )
}

export function buildRelationshipRoute(
  relationship: RelationshipSummary,
  context: RelationshipContext,
) {
  const route = relationship.route?.trim()

  if (!route) {
    return undefined
  }

  if (relationship.module === 'timeline' || relationship.module === 'audit') {
    return appendQuery(route, [
      ['entityType', toTimelineEntityType(context.entityType)],
      ['entityId', context.entityId],
    ])
  }

  return appendQuery(route, [[getEntityRouteParam(context.entityType), context.entityId]])
}

export function buildEntityTimelineRoute(context: RelationshipContext) {
  return appendQuery('/timeline', [
    ['entityType', toTimelineEntityType(context.entityType)],
    ['entityId', context.entityId],
  ])
}

export function buildEntityAuditRoute(context: RelationshipContext) {
  return appendQuery('/auditoria', [
    ['entityType', toTimelineEntityType(context.entityType)],
    ['entityId', context.entityId],
  ])
}

export function buildDocumentRelationshipRoute(context: RelationshipContext) {
  const entityType = documentEntityTypes[context.entityType]

  return appendQuery('/documentos', [
    ['entityType', entityType],
    ['entityId', context.entityId],
  ])
}

export function canLinkDocumentsForEntity(
  context: RelationshipContext,
  user: CurrentUserDto | null,
) {
  return (
    Boolean(documentEntityTypes[context.entityType]) && hasAnyPermission(['documents.read'], user)
  )
}

export function relationshipSummaryItems({
  context,
  countLabel,
  relationship,
}: {
  context: RelationshipContext
  countLabel?: (count: number) => ReactNode
  relationship: RelationshipSummary
}) {
  if (relationship.count <= 0) {
    return []
  }

  return [
    {
      description: relationship.label,
      href: buildRelationshipRoute(relationship, context),
      id: `${relationship.module}-link`,
      meta: String(relationship.count),
      title: countLabel ? countLabel(relationship.count) : relationship.label,
    },
  ]
}

export function toTimelineEntityType(entityType: CrossModuleEntityType) {
  return entityType === 'utility-account' ? 'utilityAccount' : entityType
}
