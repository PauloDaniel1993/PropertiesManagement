import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Ban, CheckCircle2, Pencil } from 'lucide-react'
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
  type RelationshipPanelItem,
  type StatusBadgeTone,
} from '../../components'
import type { AppLocale } from '../../i18n'
import type { CurrentUserDto } from '../../lib/api/identity'
import {
  getVehicle,
  type VehicleAuthorizationStatus,
  type VehicleDetail,
} from '../../lib/api/vehicles'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDateTime } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { DocumentLinkAction } from '../crossModule/DocumentLinkAction'
import {
  buildEntityAuditRoute,
  buildEntityTimelineRoute,
  canLinkDocumentsForEntity,
  canReadRelationshipModule,
  type RelationshipContext,
} from '../crossModule/relationships'
import { EntityTimelinePanel } from '../timeline'
import { getVehicleCopy } from './vehicleCopy'

export type VehicleDetailPageProps = {
  onAuthorize?: (vehicle: VehicleDetail) => void
  onBack: () => void
  onDeny?: (vehicle: VehicleDetail) => void
  onEdit?: (vehicle: VehicleDetail) => void
  vehicleId: string
}

const statusTones: Record<VehicleAuthorizationStatus, StatusBadgeTone> = {
  archived: 'archived',
  authorized: 'success',
  denied: 'danger',
  inactive: 'neutral',
  pending: 'warning',
}

function formatEntity(entity: VehicleDetail['property'], emptyLabel: string) {
  if (!entity) {
    return emptyLabel
  }

  return entity.description ? `${entity.name} | ${entity.description}` : entity.name
}

function formatBrandModel(vehicle: VehicleDetail, emptyLabel: string) {
  const values = [vehicle.brand, vehicle.model].filter(Boolean)

  return values.length > 0 ? values.join(' / ') : emptyLabel
}

function getRelationshipPanels(
  vehicle: VehicleDetail,
  copy: ReturnType<typeof getVehicleCopy>,
  locale: AppLocale,
  authUser: CurrentUserDto | null,
) {
  const context: RelationshipContext = { entityId: vehicle.id, entityType: 'vehicle' }
  const relationshipItems: Array<RelationshipPanelItem | undefined> = [
    vehicle.resident && canReadRelationshipModule('residents', authUser)
      ? {
          description: vehicle.resident.description,
          href: vehicle.resident.route,
          id: vehicle.resident.id,
          title: vehicle.resident.name,
        }
      : undefined,
    vehicle.property && canReadRelationshipModule('properties', authUser)
      ? {
          description: vehicle.property.description,
          href: vehicle.property.route,
          id: vehicle.property.id,
          title: vehicle.property.name,
        }
      : undefined,
    vehicle.contract && canReadRelationshipModule('contracts', authUser)
      ? {
          description: vehicle.contract.description,
          href: vehicle.contract.route,
          id: vehicle.contract.id,
          title: vehicle.contract.name,
        }
      : undefined,
  ]
  const canLinkDocuments = canLinkDocumentsForEntity(context, authUser)

  return (
    <div
      style={{
        display: 'grid',
        gap: 12,
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
      }}
    >
      <RelationshipPanel
        emptyState={copy.detail.emptyRelationship}
        items={relationshipItems.filter((item): item is RelationshipPanelItem => Boolean(item))}
        title={copy.detail.relationshipTitle}
      />

      {canReadRelationshipModule('documents', authUser) ? (
        <RelationshipPanel
          action={
            canLinkDocuments ? <DocumentLinkAction context={context} locale={locale} /> : undefined
          }
          emptyState={copy.detail.emptyRelationship}
          items={[]}
          title={copy.detail.documentsTitle}
        />
      ) : null}

      {canReadRelationshipModule('timeline', authUser) ? (
        <RelationshipPanel
          items={[
            {
              description: vehicle.timelineRoute,
              href: buildEntityTimelineRoute(context),
              id: 'timeline-link',
              title: copy.detail.relationships.timeline,
            },
          ]}
          title={copy.detail.relationships.timeline}
        />
      ) : null}

      {canReadRelationshipModule('audit', authUser) ? (
        <RelationshipPanel
          items={[
            {
              description: vehicle.auditRoute,
              href: buildEntityAuditRoute(context),
              id: 'audit-link',
              title: copy.detail.relationships.audit,
            },
          ]}
          title={copy.detail.relationships.audit}
        />
      ) : null}
    </div>
  )
}

export function VehicleDetailPage({
  onAuthorize,
  onBack,
  onDeny,
  onEdit,
  vehicleId,
}: VehicleDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getVehicleCopy(locale)
  const vehicleQuery = useQuery({
    queryFn: () => getVehicle(apiClient, vehicleId, locale),
    queryKey: ['vehicles', 'detail', vehicleId, locale],
  })
  const vehicle = vehicleQuery.data
  const canReadTimeline = canReadRelationshipModule('timeline', authUser)

  if (vehicleQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (vehicleQuery.isError || !vehicle) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void vehicleQuery.refetch()} />
      </section>
    )
  }

  const canMutate = vehicle.authorizationStatus.code !== 'archived'
  const canAuthorize = onAuthorize && canMutate && vehicle.authorizationStatus.code !== 'authorized'
  const canDeny = onDeny && canMutate && vehicle.authorizationStatus.code !== 'denied'
  const canEdit = onEdit && canMutate

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        actions={
          <>
            <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
              {copy.detail.actions.back}
            </ActionButton>
            {canAuthorize ? (
              <ActionButton
                icon={<CheckCircle2 aria-hidden="true" size={16} />}
                onClick={() => onAuthorize(vehicle)}
              >
                {copy.detail.actions.authorize}
              </ActionButton>
            ) : null}
            {canDeny ? (
              <ActionButton
                icon={<Ban aria-hidden="true" size={16} />}
                onClick={() => onDeny(vehicle)}
                tone="danger"
              >
                {copy.detail.actions.deny}
              </ActionButton>
            ) : null}
          </>
        }
        description={copy.detail.pageDescription}
        primaryAction={
          canEdit
            ? {
                icon: <Pencil aria-hidden="true" size={18} />,
                label: copy.detail.actions.edit,
                onClick: () => onEdit(vehicle),
              }
            : undefined
        }
        title={vehicle.plate}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.authorizationStatus,
              value: (
                <StatusBadge
                  label={vehicle.authorizationStatus.label}
                  tone={statusTones[vehicle.authorizationStatus.code]}
                />
              ),
            },
            {
              label: copy.detail.labels.type,
              value: vehicle.type.label,
            },
            {
              label: copy.detail.labels.plate,
              value: `${vehicle.plate} (${vehicle.normalizedPlate})`,
            },
            {
              label: copy.detail.labels.brandModel,
              value: formatBrandModel(vehicle, copy.terms.none),
            },
            {
              label: copy.detail.labels.color,
              value: vehicle.color ?? copy.terms.none,
            },
            {
              label: copy.detail.labels.year,
              value: vehicle.year ?? copy.terms.none,
            },
            {
              label: copy.detail.labels.resident,
              value: formatEntity(vehicle.resident, copy.terms.none),
            },
            {
              label: copy.detail.labels.property,
              value: formatEntity(vehicle.property, copy.terms.none),
            },
            {
              label: copy.detail.labels.contract,
              value: formatEntity(vehicle.contract, copy.terms.none),
            },
            {
              label: copy.detail.labels.parkingSpaceIdentifier,
              value: vehicle.parkingSpaceIdentifier ?? copy.terms.none,
              description: vehicle.normalizedParkingSpaceIdentifier,
            },
            {
              label: copy.detail.labels.parkingAllocationNotes,
              value: vehicle.parkingAllocationNotes ?? copy.terms.none,
            },
            {
              label: copy.detail.audit.createdAt,
              value: formatDateTime(vehicle.createdAt, { locale }),
            },
            {
              label: copy.detail.audit.updatedAt,
              value: vehicle.updatedAt ? formatDateTime(vehicle.updatedAt, { locale }) : '-',
            },
            {
              label: copy.detail.audit.archivedAt,
              value: vehicle.archivedAt ? formatDateTime(vehicle.archivedAt, { locale }) : '-',
            },
            {
              label: copy.detail.labels.notes,
              value: vehicle.notes ?? copy.detail.noNotes,
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: getRelationshipPanels(vehicle, copy, locale, authUser),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
          ...(canReadTimeline
            ? [
                {
                  content: <EntityTimelinePanel entityId={vehicle.id} entityType="vehicle" />,
                  id: 'timeline',
                  label: copy.detail.relationships.timeline,
                },
              ]
            : []),
        ]}
      />
    </section>
  )
}
