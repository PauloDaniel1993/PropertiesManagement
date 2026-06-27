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
import type { AppLocale } from '../../i18n'
import {
  getResident,
  type ResidentDetail,
  type ResidentPortalStatus,
  type ResidentRelationshipSummary,
  type ResidentStatus,
} from '../../lib/api/residents'
import type { CurrentUserDto } from '../../lib/api/identity'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDate, formatDateTime } from '../../lib/format'
import { coerceStatusBadgeTone } from '../../lib/statusBadges'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { DocumentLinkAction } from '../crossModule/DocumentLinkAction'
import {
  buildEntityAuditRoute,
  buildEntityTimelineRoute,
  canLinkDocumentsForEntity,
  canReadRelationshipModule,
  relationshipSummaryItems,
  type RelationshipContext,
} from '../crossModule/relationships'
import { EntityTimelinePanel } from '../timeline'
import { getResidentCopy } from './residentCopy'

export type ResidentDetailPageProps = {
  onBack: () => void
  onEdit?: (resident: ResidentDetail) => void
  residentId: string
}

const statusTones: Record<ResidentStatus, StatusBadgeTone> = {
  active: 'success',
  archived: 'archived',
  inactive: 'neutral',
}

const portalStatusTones: Record<ResidentPortalStatus, StatusBadgeTone> = {
  active: 'success',
  disabled: 'danger',
  invited: 'info',
  'not-invited': 'neutral',
}

const relationshipModules = [
  'contracts',
  'properties',
  'payments',
  'documents',
  'pets',
  'vehicles',
  'occurrences',
] as const

function getSensitiveValue(
  value: string | undefined,
  resident: ResidentDetail,
  copy: ReturnType<typeof getResidentCopy>,
) {
  if (resident.isSensitiveMasked) {
    return copy.detail.maskedValue
  }

  return value || copy.detail.noValue
}

function formatDocument(resident: ResidentDetail, copy: ReturnType<typeof getResidentCopy>) {
  if (resident.isSensitiveMasked) {
    return copy.detail.maskedValue
  }

  const document = [resident.documentType, resident.documentIdentifier].filter(Boolean).join(' ')

  return document || copy.detail.noValue
}

function formatEmergencyContact(
  resident: ResidentDetail,
  copy: ReturnType<typeof getResidentCopy>,
) {
  if (resident.isSensitiveMasked) {
    return copy.detail.maskedValue
  }

  const contact = [
    resident.emergencyContact.name,
    resident.emergencyContact.relationship,
    resident.emergencyContact.phone,
  ]
    .filter(Boolean)
    .join(' | ')

  return contact || copy.detail.noValue
}

function formatPrivacyFlags(resident: ResidentDetail, copy: ReturnType<typeof getResidentCopy>) {
  if (resident.privacyFlags.length === 0) {
    return copy.detail.privacyNone
  }

  return resident.privacyFlags
    .map((flag) =>
      flag in copy.form.privacyFlags
        ? copy.form.privacyFlags[flag as keyof typeof copy.form.privacyFlags]
        : flag,
    )
    .join(', ')
}

function getRelationship(relationships: ResidentRelationshipSummary[], module: string) {
  return relationships.find((relationship) => relationship.module === module)
}

function getRelationshipPanels(
  resident: ResidentDetail,
  copy: ReturnType<typeof getResidentCopy>,
  locale: AppLocale,
  authUser: CurrentUserDto | null,
) {
  const context: RelationshipContext = { entityId: resident.id, entityType: 'resident' }
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
          const relationship = getRelationship(resident.relationships, module)
          const relationshipCopy = copy.detail.relationships[module]

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
                      countLabel: copy.detail.relationshipCount,
                      relationship,
                    })
                  : []
              }
              title={relationshipCopy.title}
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

export function ResidentDetailPage({ onBack, onEdit, residentId }: ResidentDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getResidentCopy(locale)
  const residentQuery = useQuery({
    queryFn: () => getResident(apiClient, residentId),
    queryKey: ['residents', 'detail', residentId],
  })
  const resident = residentQuery.data
  const canReadTimeline = canReadRelationshipModule('timeline', authUser)

  if (residentQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (residentQuery.isError || !resident) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void residentQuery.refetch()} />
      </section>
    )
  }

  const canEditResident = onEdit && !resident.isArchived && resident.status.code !== 'archived'

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
          canEditResident
            ? {
                icon: <Pencil aria-hidden="true" size={18} />,
                label: copy.detail.actions.edit,
                onClick: () => onEdit(resident),
              }
            : undefined
        }
        title={resident.fullName}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.preferredName,
              value: resident.preferredName ?? copy.detail.noValue,
            },
            {
              label: copy.detail.labels.status,
              value: (
                <StatusBadge
                  label={resident.status.label}
                  tone={coerceStatusBadgeTone(
                    resident.status.tone,
                    statusTones[resident.status.code],
                  )}
                />
              ),
            },
            {
              label: copy.detail.labels.portalStatus,
              value: (
                <StatusBadge
                  label={resident.portalStatus.label}
                  tone={coerceStatusBadgeTone(
                    resident.portalStatus.tone,
                    portalStatusTones[resident.portalStatus.code],
                  )}
                />
              ),
            },
            {
              label: copy.detail.labels.email,
              value: getSensitiveValue(resident.email, resident, copy),
            },
            {
              label: copy.detail.labels.phone,
              value: getSensitiveValue(resident.phone, resident, copy),
            },
            {
              label: copy.detail.labels.secondaryPhone,
              value: getSensitiveValue(resident.secondaryPhone, resident, copy),
            },
            {
              label: copy.detail.labels.document,
              value: formatDocument(resident, copy),
            },
            {
              label: copy.detail.labels.birthDate,
              value: resident.birthDate
                ? formatDate(resident.birthDate, { locale })
                : copy.detail.noValue,
            },
            {
              label: copy.detail.labels.emergencyContact,
              value: formatEmergencyContact(resident, copy),
            },
            {
              label: copy.detail.labels.privacyFlags,
              value: formatPrivacyFlags(resident, copy),
            },
            {
              label: copy.detail.audit.createdAt,
              value: resident.createdAt
                ? formatDateTime(resident.createdAt, { locale })
                : copy.detail.noValue,
            },
            {
              label: copy.detail.audit.updatedAt,
              value: resident.updatedAt
                ? formatDateTime(resident.updatedAt, { locale })
                : copy.detail.noValue,
            },
            {
              label: copy.detail.audit.archivedAt,
              value: resident.archivedAt
                ? formatDateTime(resident.archivedAt, { locale })
                : copy.detail.noValue,
            },
            {
              label: copy.detail.labels.notes,
              value: resident.isSensitiveMasked
                ? copy.detail.maskedValue
                : (resident.notes ?? copy.detail.noNotes),
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: getRelationshipPanels(resident, copy, locale, authUser),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
          ...(canReadTimeline
            ? [
                {
                  content: <EntityTimelinePanel entityId={resident.id} entityType="resident" />,
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
