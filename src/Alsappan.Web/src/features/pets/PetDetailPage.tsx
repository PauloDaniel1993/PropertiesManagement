import { ArrowLeft, CheckCircle2, Pencil, ShieldX } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
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
import { getPet, type PetDetail, type PetEntitySummary } from '../../lib/api/pets'
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
import { getPetCopy } from './petCopy'

const statusTones: Record<string, StatusBadgeTone> = {
  archived: 'archived',
  authorized: 'success',
  denied: 'danger',
  inactive: 'neutral',
  pending: 'warning',
}

export type PetDetailPageProps = {
  onAuthorize?: (pet: PetDetail) => void
  onBack: () => void
  onDeny?: (pet: PetDetail) => void
  onEdit?: (pet: PetDetail) => void
  petId: string
}

function formatEntity(entity: PetEntitySummary | undefined, fallback: string) {
  if (!entity) {
    return fallback
  }

  return entity.description ? `${entity.name} - ${entity.description}` : entity.name
}

function getDocumentItems(pet: PetDetail) {
  const documents = [...pet.vaccinationRecordDocuments, ...pet.authorizationFormDocuments]

  return documents.map((document) => ({
    href: document.route,
    id: `${document.kind}-${document.documentId}`,
    meta: document.documentId,
    title: document.label ?? document.kindLabel,
  }))
}

function getRelationshipPanels(
  pet: PetDetail,
  copy: ReturnType<typeof getPetCopy>,
  locale: AppLocale,
  authUser: CurrentUserDto | null,
) {
  const context: RelationshipContext = { entityId: pet.id, entityType: 'pet' }
  const relationshipItems: RelationshipPanelItem[] = []

  if (canReadRelationshipModule('timeline', authUser)) {
    relationshipItems.push({
      href: buildEntityTimelineRoute(context),
      id: 'timeline',
      title: copy.detail.relationships.timeline,
    })
  }

  if (canReadRelationshipModule('audit', authUser)) {
    relationshipItems.push({
      href: buildEntityAuditRoute(context),
      id: 'audit',
      title: copy.detail.relationships.audit,
    })
  }
  const canLinkDocuments = canLinkDocumentsForEntity(context, authUser)

  return (
    <div style={{ display: 'grid', gap: 14 }}>
      {canReadRelationshipModule('documents', authUser) ? (
        <RelationshipPanel
          action={
            canLinkDocuments ? <DocumentLinkAction context={context} locale={locale} /> : undefined
          }
          emptyState={copy.detail.emptyRelationship}
          items={getDocumentItems(pet)}
          title={copy.detail.documentsTitle}
        />
      ) : null}
      <RelationshipPanel
        emptyState={copy.detail.emptyRelationship}
        items={pet.authorizationHistory.map((history) => ({
          id: `${history.status}-${history.occurredAt}`,
          meta: formatDateTime(history.occurredAt),
          status: <StatusBadge label={history.statusLabel} tone={statusTones[history.status]} />,
          title: history.notes ?? history.statusLabel,
        }))}
        title={copy.detail.historyTitle}
      />
      <RelationshipPanel
        emptyState={copy.detail.emptyRelationship}
        items={relationshipItems}
        title={copy.detail.relationshipTitle}
      />
    </div>
  )
}

export function PetDetailPage({ onAuthorize, onBack, onDeny, onEdit, petId }: PetDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getPetCopy(locale)
  const petQuery = useQuery({
    queryFn: () => getPet(apiClient, petId, locale),
    queryKey: ['pets', 'detail', petId, locale],
  })
  const pet = petQuery.data
  const canReadTimeline = canReadRelationshipModule('timeline', authUser)

  if (petQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (petQuery.isError || !pet) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void petQuery.refetch()} />
      </section>
    )
  }

  const canEdit = onEdit && pet.authorizationStatus.code !== 'archived'
  const canAuthorize =
    onAuthorize && !['archived', 'authorized'].includes(pet.authorizationStatus.code)
  const canDeny = onDeny && !['archived', 'denied'].includes(pet.authorizationStatus.code)

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
                onClick={() => onAuthorize(pet)}
              >
                {copy.detail.actions.authorize}
              </ActionButton>
            ) : null}
            {canDeny ? (
              <ActionButton
                icon={<ShieldX aria-hidden="true" size={16} />}
                onClick={() => onDeny(pet)}
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
                onClick: () => onEdit(pet),
              }
            : undefined
        }
        title={pet.name}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.authorizationStatus,
              value: (
                <StatusBadge
                  label={pet.authorizationStatus.label}
                  tone={statusTones[pet.authorizationStatus.code]}
                />
              ),
            },
            {
              label: copy.detail.labels.species,
              value: pet.species.label,
            },
            {
              label: copy.detail.labels.breed,
              value: pet.breed ?? copy.terms.none,
            },
            {
              label: copy.detail.labels.resident,
              value: formatEntity(pet.resident, copy.terms.none),
            },
            {
              label: copy.detail.labels.property,
              value: formatEntity(pet.property, copy.terms.none),
            },
            {
              label: copy.detail.labels.contract,
              value: formatEntity(pet.contract, copy.terms.none),
            },
            {
              label: copy.detail.audit.createdAt,
              value: formatDateTime(pet.createdAt, { locale }),
            },
            {
              label: copy.detail.audit.updatedAt,
              value: pet.updatedAt ? formatDateTime(pet.updatedAt, { locale }) : '-',
            },
            {
              label: copy.detail.audit.archivedAt,
              value: pet.archivedAt ? formatDateTime(pet.archivedAt, { locale }) : '-',
            },
            {
              label: copy.detail.labels.authorizationNotes,
              value: pet.authorizationNotes ?? copy.detail.noNotes,
            },
            {
              label: copy.detail.labels.notes,
              value: pet.notes ?? copy.detail.noNotes,
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: getRelationshipPanels(pet, copy, locale, authUser),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
          ...(canReadTimeline
            ? [
                {
                  content: <EntityTimelinePanel entityId={pet.id} entityType="pet" />,
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
