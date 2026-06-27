import type { CurrentUserDto } from '../../lib/api/identity'
import { coerceStatusBadgeTone } from '../../lib/statusBadges'
import {
  buildDocumentRelationshipRoute,
  buildEntityAuditRoute,
  buildEntityTimelineRoute,
  filterRelationshipsByPermission,
  relationshipSummaryItems,
  type RelationshipContext,
  type RelationshipSummary,
} from './relationships'

function buildUser(permissions: string[]): CurrentUserDto {
  return {
    accountType: 'admin',
    activeOrganizationId: 'org-a',
    displayName: 'Ana Admin',
    email: 'ana@example.com',
    id: 'user-a',
    organizations: [
      {
        currencyCode: 'BRL',
        displayName: 'Organizacao A',
        id: 'org-a',
        locale: 'pt-BR',
        name: 'Organizacao A',
        permissionCodes: permissions,
        roleCodes: ['Administrador'],
        slug: 'org-a',
      },
    ],
    permissions,
  }
}

describe('cross-module relationship helpers', () => {
  it('builds consistent deep links for timeline, audit, and documents', () => {
    const context: RelationshipContext = { entityId: 'property-a', entityType: 'property' }

    expect(buildEntityTimelineRoute(context)).toBe(
      '/timeline?entityType=property&entityId=property-a',
    )
    expect(buildEntityAuditRoute(context)).toBe(
      '/auditoria?entityType=property&entityId=property-a',
    )
    expect(buildDocumentRelationshipRoute(context)).toBe(
      '/documentos?entityType=property&entityId=property-a',
    )
  })

  it('filters related modules using active organization permissions', () => {
    const relationships: RelationshipSummary[] = [
      { count: 1, label: 'Contratos', module: 'contracts', route: '/contratos' },
      { count: 1, label: 'Pagamentos', module: 'payments', route: '/pagamentos' },
      { count: 1, label: 'Auditoria', module: 'audit', route: '/auditoria' },
    ]

    expect(filterRelationshipsByPermission(relationships, buildUser(['contracts.read']))).toEqual([
      relationships[0],
    ])
  })

  it('keeps localized relationship labels and coerces invalid badge tones', () => {
    const items = relationshipSummaryItems({
      context: { entityId: 'resident-a', entityType: 'resident' },
      countLabel: (count) => `${count} vinculos`,
      relationship: {
        count: 2,
        label: 'Contratos vinculados',
        module: 'contracts',
        route: '/contratos',
      },
    })

    expect(items[0]).toMatchObject({
      href: '/contratos?residentId=resident-a',
      meta: '2',
      title: '2 vinculos',
    })
    expect(coerceStatusBadgeTone('archived')).toBe('archived')
    expect(coerceStatusBadgeTone('unexpected', 'neutral')).toBe('neutral')
  })
})
