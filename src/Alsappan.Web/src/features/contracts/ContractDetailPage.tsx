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
import {
  getLeaseContract,
  type ContractDetail,
  type ContractStatus,
} from '../../lib/api/leaseContracts'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDate, formatDateTime, formatMoney } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
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

function getRelationshipPanels(contract: ContractDetail, copy: ReturnType<typeof getContractCopy>) {
  const emptyPanels = contract.relationships.filter(
    (relationship) => !['timeline', 'audit'].includes(relationship.module),
  )
  const timeline = contract.relationships.find((relationship) => relationship.module === 'timeline')
  const audit = contract.relationships.find((relationship) => relationship.module === 'audit')

  return (
    <div
      style={{
        display: 'grid',
        gap: 12,
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
      }}
    >
      {emptyPanels.map((relationship) => (
        <RelationshipPanel
          key={relationship.module}
          emptyState={copy.detail.emptyRelationship}
          items={[]}
          title={relationship.label}
        />
      ))}

      <RelationshipPanel
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

      {timeline ? (
        <RelationshipPanel
          items={[
            {
              description: timeline.route,
              href: timeline.route,
              id: 'timeline-link',
              title: timeline.label,
            },
          ]}
          title={timeline.label}
        />
      ) : null}

      {audit ? (
        <RelationshipPanel
          items={[
            {
              description: audit.route,
              href: audit.route,
              id: 'audit-link',
              title: audit.label,
            },
          ]}
          title={audit.label}
        />
      ) : null}
    </div>
  )
}

export function ContractDetailPage({ contractId, onBack, onEdit }: ContractDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getContractCopy(locale)
  const contractQuery = useQuery({
    queryFn: () => getLeaseContract(apiClient, contractId),
    queryKey: ['contracts', 'detail', contractId],
  })
  const contract = contractQuery.data

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
                  tone={statusTones[contract.status.code]}
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
            content: getRelationshipPanels(contract, copy),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
        ]}
      />
    </section>
  )
}
