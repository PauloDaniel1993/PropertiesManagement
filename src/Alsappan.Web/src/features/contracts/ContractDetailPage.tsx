import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
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
  type RelationshipPanelItem,
  type StatusBadgeTone,
} from '../../components'
import type { AppLocale } from '../../i18n'
import {
  getLeaseContract,
  type ContractDetail,
  type ContractRelationshipSummary,
  type ContractStatus,
} from '../../lib/api/leaseContracts'
import type { CurrentUserDto } from '../../lib/api/identity'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDate, formatDateTime, formatMoney } from '../../lib/format'
import { coerceStatusBadgeTone } from '../../lib/statusBadges'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { DocumentLinkAction } from '../crossModule/DocumentLinkAction'
import {
  buildEntityAuditRoute,
  buildEntityTimelineRoute,
  canLinkDocumentsForEntity,
  canReadRelationshipModule,
  filterRelationshipsByPermission,
  relationshipSummaryItems,
  type RelationshipContext,
} from '../crossModule/relationships'
import { EntityTimelinePanel } from '../timeline'
import { getContractCopy } from './contractCopy'

export type ContractDetailPageProps = {
  contractId: string
  onBack: () => void
  onEdit?: (contract: ContractDetail) => void
}

const statusTones: Record<ContractStatus, StatusBadgeTone> = {
  active: 'success',
  archived: 'archived',
  cancelled: 'danger',
  draft: 'neutral',
  ended: 'danger',
  'ending-soon': 'warning',
  terminated: 'danger',
}

const summaryRelationshipModules = ['payments', 'utility-accounts', 'inspections'] as const

function getRelationship(
  relationships: ContractRelationshipSummary[],
  module: (typeof summaryRelationshipModules)[number],
) {
  const aliases =
    module === 'utility-accounts' ? ['utility-accounts', 'utilityAccounts', 'utilities'] : [module]

  return relationships.find((relationship) => aliases.includes(relationship.module))
}

function getContractRelationshipPanels(
  contract: ContractDetail,
  copy: ReturnType<typeof getContractCopy>,
  locale: AppLocale,
  authUser: CurrentUserDto | null,
) {
  const context: RelationshipContext = { entityId: contract.id, entityType: 'contract' }
  const relationships = filterRelationshipsByPermission(
    contract.relationships.filter(
      (relationship) => !['timeline', 'audit', 'documents'].includes(relationship.module),
    ),
    authUser,
  )
  const relationshipPanels: ReactNode[] = []
  const canLinkDocuments = canLinkDocumentsForEntity(context, authUser)

  if (canReadRelationshipModule('properties', authUser)) {
    relationshipPanels.push(
      <RelationshipPanel
        key="property"
        emptyState={copy.detail.emptyRelationship}
        items={[
          {
            href: `/imoveis?propertyId=${encodeURIComponent(contract.property.id)}`,
            id: contract.property.id,
            meta: contract.property.location,
            title: contract.property.name,
          },
        ]}
        title={copy.detail.labels.property}
      />,
    )
  }

  if (canReadRelationshipModule('residents', authUser)) {
    const residentItems: RelationshipPanelItem[] = contract.residents.map((resident) => ({
      href: `/moradores?residentId=${encodeURIComponent(resident.id)}`,
      id: resident.id,
      meta: resident.isPrimary ? copy.detail.labels.primaryResident : undefined,
      title: resident.name,
    }))

    relationshipPanels.push(
      <RelationshipPanel
        key="residents"
        emptyState={copy.detail.emptyRelationship}
        items={residentItems}
        title={copy.detail.labels.residents}
      />,
    )
  }

  return (
    <div
      style={{
        display: 'grid',
        gap: 12,
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
      }}
    >
      {relationshipPanels}

      {summaryRelationshipModules
        .filter((module) => canReadRelationshipModule(module, authUser))
        .map((module) => {
          const relationship = getRelationship(relationships, module)

          return (
            <RelationshipPanel
              key={module}
              emptyState={copy.detail.emptyRelationship}
              items={
                relationship
                  ? relationshipSummaryItems({
                      context,
                      relationship,
                    })
                  : []
              }
              title={relationship?.label ?? module}
            />
          )
        })}

      {canReadRelationshipModule('documents', authUser) ? (
        <RelationshipPanel
          action={
            canLinkDocuments ? <DocumentLinkAction context={context} locale={locale} /> : undefined
          }
          items={contract.documents.map((document) => ({
            description: document.category,
            href: document.route,
            id: document.documentId ?? document.category,
            meta: `${document.count}`,
            title: document.label,
          }))}
          emptyState={copy.detail.emptyRelationship}
          title={copy.detail.documentsTitle}
        />
      ) : null}

      {canReadRelationshipModule('timeline', authUser) ? (
        <RelationshipPanel
          items={[
            {
              description: buildEntityTimelineRoute(context),
              href: buildEntityTimelineRoute(context),
              id: 'timeline-link',
              title:
                contract.relationships.find((relationship) => relationship.module === 'timeline')
                  ?.label ?? 'Timeline',
            },
          ]}
          title={
            contract.relationships.find((relationship) => relationship.module === 'timeline')
              ?.label ?? 'Timeline'
          }
        />
      ) : null}

      {canReadRelationshipModule('audit', authUser) ? (
        <RelationshipPanel
          items={[
            {
              description: buildEntityAuditRoute(context),
              href: buildEntityAuditRoute(context),
              id: 'audit-link',
              title:
                contract.relationships.find((relationship) => relationship.module === 'audit')
                  ?.label ?? 'Auditoria',
            },
          ]}
          title={
            contract.relationships.find((relationship) => relationship.module === 'audit')?.label ??
            'Auditoria'
          }
        />
      ) : null}
    </div>
  )
}

export function ContractDetailPage({ contractId, onBack, onEdit }: ContractDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getContractCopy(locale)
  const contractQuery = useQuery({
    queryFn: () => getLeaseContract(apiClient, contractId),
    queryKey: ['contracts', 'detail', contractId],
  })
  const contract = contractQuery.data
  const canReadTimeline = canReadRelationshipModule('timeline', authUser)

  if (contractQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (contractQuery.isError || !contract) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void contractQuery.refetch()} />
      </section>
    )
  }

  const canEditContract =
    onEdit && !['archived', 'cancelled', 'terminated'].includes(contract.status.code)

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
          canEditContract
            ? {
                icon: <Pencil aria-hidden="true" size={18} />,
                label: copy.detail.actions.edit,
                onClick: () => onEdit(contract),
              }
            : undefined
        }
        title={`${contract.property.name} - ${contract.primaryResident.name}`}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.status,
              value: (
                <StatusBadge
                  label={contract.status.label}
                  tone={coerceStatusBadgeTone(
                    contract.status.tone,
                    statusTones[contract.status.code],
                  )}
                />
              ),
            },
            {
              label: copy.detail.labels.property,
              value: contract.property.location
                ? `${contract.property.name} | ${contract.property.location}`
                : contract.property.name,
            },
            {
              label: copy.detail.labels.primaryResident,
              value: contract.primaryResident.name,
            },
            {
              label: copy.detail.labels.startDate,
              value: formatDate(contract.startDate, { locale }),
            },
            {
              label: copy.detail.labels.endDate,
              value: contract.endDate
                ? formatDate(contract.endDate, { locale })
                : copy.terms.noEndDate,
            },
            {
              label: copy.detail.labels.monthlyRent,
              value: formatMoney(contract.monthlyRent.amount, {
                currency: contract.monthlyRent.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.deposit,
              value: contract.depositAmount
                ? formatMoney(contract.depositAmount.amount, {
                    currency: contract.depositAmount.currency,
                    locale,
                  })
                : '-',
            },
            {
              label: copy.detail.labels.dueDay,
              value: String(contract.dueDay),
            },
            {
              label: copy.detail.labels.adjustment,
              value: `${contract.adjustmentIndexLabel} / ${contract.adjustmentIntervalMonths}`,
            },
            {
              label: copy.detail.labels.residents,
              value: contract.residents.map((resident) => resident.name).join(', '),
              description: copy.terms.residentsCount(contract.residents.length),
            },
            {
              label: copy.detail.audit.createdAt,
              value: contract.createdAt ? formatDateTime(contract.createdAt, { locale }) : '-',
            },
            {
              label: copy.detail.audit.updatedAt,
              value: contract.updatedAt ? formatDateTime(contract.updatedAt, { locale }) : '-',
            },
            {
              label: copy.detail.labels.penalty,
              value: contract.penaltyNotes ?? copy.detail.noNotes,
            },
            {
              label: copy.detail.labels.discount,
              value: contract.discountNotes ?? copy.detail.noNotes,
            },
            {
              label: copy.detail.labels.notes,
              value: contract.notes ?? copy.detail.noNotes,
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: getContractRelationshipPanels(contract, copy, locale, authUser),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
          ...(canReadTimeline
            ? [
                {
                  content: <EntityTimelinePanel entityId={contract.id} entityType="contract" />,
                  id: 'timeline',
                  label:
                    contract.relationships.find(
                      (relationship) => relationship.module === 'timeline',
                    )?.label ?? 'Timeline',
                },
              ]
            : []),
        ]}
      />
    </section>
  )
}
