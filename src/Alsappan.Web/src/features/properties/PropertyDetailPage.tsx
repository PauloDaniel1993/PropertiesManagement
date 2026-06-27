import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Pencil } from 'lucide-react'
import {
  ActionButton,
  DetailList,
  DetailSection,
  ErrorState,
  LoadingState,
  PageHeader,
  RelationshipPanel,
  StatusBadge,
  Tabs,
  type StatusBadgeTone,
} from '../../components'
import { EntityTimelinePanel } from '../timeline'
import { DocumentLinkAction } from '../crossModule/DocumentLinkAction'
import {
  buildEntityAuditRoute,
  buildEntityTimelineRoute,
  canLinkDocumentsForEntity,
  canReadRelationshipModule,
  filterRelationshipsByPermission,
  relationshipSummaryItems,
  type RelationshipContext,
  type RelationshipSummary,
} from '../crossModule/relationships'
import type { AppLocale } from '../../i18n'
import {
  getProperty,
  type PropertyDetail,
  type PropertyRelationshipSummary,
  type PropertyStatus,
} from '../../lib/api/properties'
import type { CurrentUserDto } from '../../lib/api/identity'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDateTime, formatMoney } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { getPropertyCopy } from './propertyCopy'

export type PropertyDetailPageProps = {
  onBack: () => void
  onEdit?: (property: PropertyDetail) => void
  propertyId: string
}

const statusTones: Record<PropertyStatus, StatusBadgeTone> = {
  archived: 'archived',
  available: 'success',
  inactive: 'neutral',
  maintenance: 'warning',
  rented: 'info',
  reserved: 'warning',
}

function formatAddress(address: PropertyDetail['address']) {
  const firstLine = [address.street, address.number].filter(Boolean).join(', ')
  const secondLine = [address.neighborhood, address.city, address.state].filter(Boolean).join(' - ')
  const postalLine = [address.postalCode, address.country].filter(Boolean).join(' - ')

  return [firstLine, address.complement, secondLine, postalLine].filter(Boolean).join(' | ')
}

function getGarageLabel(property: PropertyDetail, copy: ReturnType<typeof getPropertyCopy>) {
  if (!property.hasGarage) {
    return copy.list.garageNo
  }

  return copy.list.garageSpaces(property.garageSpaces ?? 0)
}

const relationshipModules = [
  'contracts',
  'residents',
  'payments',
  'utilities',
  'documents',
  'pets',
  'vehicles',
  'occurrences',
  'inspections',
] as const

function getRelationship(relationships: RelationshipSummary[], module: string) {
  const aliases =
    module === 'utilities' ? ['utilities', 'utility-accounts', 'utilityAccounts'] : [module]

  return relationships.find((relationship) => aliases.includes(relationship.module))
}

function getRelationshipTitle(
  module: (typeof relationshipModules)[number],
  copy: ReturnType<typeof getPropertyCopy>,
) {
  return copy.detail.relationships[module].title
}

function getFallbackRelationships(
  context: RelationshipContext,
  copy: ReturnType<typeof getPropertyCopy>,
): PropertyRelationshipSummary[] {
  const relationships: PropertyRelationshipSummary[] = relationshipModules.map((module) => ({
    count: 0,
    label: getRelationshipTitle(module, copy),
    module,
    route: module === 'utilities' ? '/contas-de-consumo' : `/${module}`,
  }))

  return [
    ...relationships,
    {
      count: 1,
      label: copy.detail.relationships.timeline.linkTitle,
      module: 'timeline',
      route: buildEntityTimelineRoute(context),
    },
    {
      count: 1,
      label: copy.detail.relationships.audit.linkTitle,
      module: 'audit',
      route: buildEntityAuditRoute(context),
    },
  ]
}

function getRelationshipPanels(
  property: PropertyDetail,
  copy: ReturnType<typeof getPropertyCopy>,
  locale: AppLocale,
  authUser: CurrentUserDto | null,
) {
  const context: RelationshipContext = { entityId: property.id, entityType: 'property' }
  const relationships = filterRelationshipsByPermission(
    property.relationships.length > 0
      ? property.relationships
      : getFallbackRelationships(context, copy),
    authUser,
  )
  const canLinkDocuments = canLinkDocumentsForEntity(context, authUser)

  return (
    <div
      style={{
        display: 'grid',
        gap: 12,
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
      }}
    >
      {relationshipModules
        .filter((module) => canReadRelationshipModule(module, authUser))
        .map((module) => {
          const relationship = getRelationship(relationships, module)

          return (
            <RelationshipPanel
              key={module}
              action={
                module === 'documents' && canLinkDocuments ? (
                  <DocumentLinkAction context={context} locale={locale} />
                ) : undefined
              }
              emptyState={copy.detail.emptyRelationship}
              items={
                relationship
                  ? relationshipSummaryItems({
                      context,
                      relationship,
                    })
                  : []
              }
              title={getRelationshipTitle(module, copy)}
            />
          )
        })}

      {canReadRelationshipModule('timeline', authUser) ? (
        <RelationshipPanel
          items={[
            {
              description: copy.detail.relationships.timeline.description,
              href: buildEntityTimelineRoute(context),
              id: 'timeline-link',
              title: copy.detail.relationships.timeline.linkTitle,
            },
          ]}
          title={copy.detail.relationships.timeline.title}
        />
      ) : null}

      {canReadRelationshipModule('audit', authUser) ? (
        <RelationshipPanel
          items={[
            {
              description: copy.detail.relationships.audit.description,
              href: buildEntityAuditRoute(context),
              id: 'audit-link',
              title: copy.detail.relationships.audit.linkTitle,
            },
          ]}
          title={copy.detail.relationships.audit.title}
        />
      ) : null}
    </div>
  )
}

export function PropertyDetailPage({ onBack, onEdit, propertyId }: PropertyDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getPropertyCopy(locale)
  const propertyQuery = useQuery({
    queryFn: () => getProperty(apiClient, propertyId),
    queryKey: ['properties', 'detail', propertyId],
  })
  const property = propertyQuery.data
  const canReadTimeline = canReadRelationshipModule('timeline', authUser)

  if (propertyQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (propertyQuery.isError || !property) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void propertyQuery.refetch()} />
      </section>
    )
  }

  const canEditProperty = onEdit && property.status !== 'archived'

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        actions={
          <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
            {copy.detail.actions.back}
          </ActionButton>
        }
        description={copy.detail.pageDescription}
        primaryAction={
          canEditProperty
            ? {
                icon: <Pencil aria-hidden="true" size={18} />,
                label: copy.detail.actions.edit,
                onClick: () => onEdit(property),
              }
            : undefined
        }
        title={property.name}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.type,
              value: property.typeLabel ?? (property.type ? copy.list.types[property.type] : '-'),
            },
            {
              label: copy.detail.labels.status,
              value: (
                <StatusBadge
                  label={property.statusLabel ?? copy.list.status[property.status]}
                  tone={statusTones[property.status]}
                />
              ),
            },
            {
              label: copy.detail.labels.rent,
              value: formatMoney(property.suggestedRent, {
                currency: property.currencyCode,
                locale,
              }),
            },
            {
              label: copy.detail.labels.garage,
              value: getGarageLabel(property, copy),
            },
            {
              label: copy.detail.audit.createdAt,
              value: property.createdAt
                ? formatDateTime(property.createdAt, { locale })
                : property.audit?.createdAt
                  ? formatDateTime(property.audit.createdAt, { locale })
                  : '-',
            },
            {
              label: copy.detail.audit.updatedAt,
              value: property.updatedAt
                ? formatDateTime(property.updatedAt, { locale })
                : property.audit?.updatedAt
                  ? formatDateTime(property.audit.updatedAt, { locale })
                  : '-',
            },
            {
              label: copy.detail.labels.address,
              value: formatAddress(property.address),
            },
            {
              label: copy.detail.labels.notes,
              value: property.notes ?? copy.detail.noNotes,
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: getRelationshipPanels(property, copy, locale, authUser),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
          ...(canReadTimeline
            ? [
                {
                  content: <EntityTimelinePanel entityId={property.id} entityType="property" />,
                  id: 'timeline',
                  label: copy.detail.relationships.timeline.title,
                },
              ]
            : []),
        ]}
      />
    </section>
  )
}
