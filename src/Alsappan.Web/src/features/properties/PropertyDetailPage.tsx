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
import { getProperty, type PropertyDetail, type PropertyStatus } from '../../lib/api/properties'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDateTime, formatMoney } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
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

function getRelationshipPanels(property: PropertyDetail, copy: ReturnType<typeof getPropertyCopy>) {
  const emptyPanels = [
    copy.detail.relationships.contracts.title,
    copy.detail.relationships.residents.title,
    copy.detail.relationships.payments.title,
    copy.detail.relationships.utilities.title,
    copy.detail.relationships.documents.title,
    copy.detail.relationships.pets.title,
    copy.detail.relationships.vehicles.title,
    copy.detail.relationships.occurrences.title,
    copy.detail.relationships.inspections.title,
  ]

  return (
    <div
      style={{
        display: 'grid',
        gap: 12,
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
      }}
    >
      {emptyPanels.map((title) => (
        <RelationshipPanel
          key={String(title)}
          emptyState={copy.detail.emptyRelationship}
          items={[]}
          title={title}
        />
      ))}

      <RelationshipPanel
        items={[
          {
            description: copy.detail.relationships.timeline.description,
            href: `/timeline?propertyId=${encodeURIComponent(property.id)}`,
            id: 'timeline-link',
            title: copy.detail.relationships.timeline.linkTitle,
          },
        ]}
        title={copy.detail.relationships.timeline.title}
      />

      <RelationshipPanel
        items={[
          {
            description: copy.detail.relationships.audit.description,
            href: `/auditoria?propertyId=${encodeURIComponent(property.id)}`,
            id: 'audit-link',
            title: copy.detail.relationships.audit.linkTitle,
          },
        ]}
        title={copy.detail.relationships.audit.title}
      />
    </div>
  )
}

export function PropertyDetailPage({ onBack, onEdit, propertyId }: PropertyDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getPropertyCopy(locale)
  const propertyQuery = useQuery({
    queryFn: () => getProperty(apiClient, propertyId),
    queryKey: ['properties', 'detail', propertyId],
  })
  const property = propertyQuery.data

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
          onEdit
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
            content: getRelationshipPanels(property, copy),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
        ]}
      />
    </section>
  )
}
