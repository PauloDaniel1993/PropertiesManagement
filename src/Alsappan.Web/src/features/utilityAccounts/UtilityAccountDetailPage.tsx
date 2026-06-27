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
import {
  getUtilityAccount,
  type UtilityAccountDetail,
  type UtilityAccountStatus,
} from '../../lib/api/utilityAccounts'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDate, formatDateTime, formatMoney } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getUtilityAccountCopy } from './utilityAccountCopy'

export type UtilityAccountDetailPageProps = {
  onBack: () => void
  onCancel?: (utilityAccount: UtilityAccountDetail) => void
  onEdit?: (utilityAccount: UtilityAccountDetail) => void
  onMarkPaid?: (utilityAccount: UtilityAccountDetail) => void
  utilityAccountId: string
}

const statusTones: Record<UtilityAccountStatus, StatusBadgeTone> = {
  archived: 'archived',
  cancelled: 'archived',
  disputed: 'danger',
  open: 'warning',
  overdue: 'danger',
  paid: 'success',
}

function formatEntity(entity: UtilityAccountDetail['property'], emptyLabel: string) {
  if (!entity) {
    return emptyLabel
  }

  return entity.description ? `${entity.name} | ${entity.description}` : entity.name
}

function formatPeriod(utilityAccount: UtilityAccountDetail, locale: AppLocale) {
  const start = formatDate(utilityAccount.billingPeriodStart, { locale })
  const end = utilityAccount.billingPeriodEnd
    ? formatDate(utilityAccount.billingPeriodEnd, { locale })
    : undefined

  return end ? `${start} - ${end}` : start
}

function getRelationshipPanels(
  utilityAccount: UtilityAccountDetail,
  copy: ReturnType<typeof getUtilityAccountCopy>,
) {
  const relationshipItems: Array<RelationshipPanelItem | undefined> = [
    utilityAccount.property
      ? {
          description: utilityAccount.property.description,
          href: utilityAccount.property.route,
          id: utilityAccount.property.id,
          title: utilityAccount.property.name,
        }
      : undefined,
    utilityAccount.contract
      ? {
          description: utilityAccount.contract.description,
          href: utilityAccount.contract.route,
          id: utilityAccount.contract.id,
          title: utilityAccount.contract.name,
        }
      : undefined,
    utilityAccount.resident
      ? {
          description: utilityAccount.resident.description,
          href: utilityAccount.resident.route,
          id: utilityAccount.resident.id,
          title: utilityAccount.resident.name,
        }
      : undefined,
  ]

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

      <RelationshipPanel
        emptyState={copy.detail.emptyRelationship}
        items={utilityAccount.billDocuments.map((document) => ({
          href: document.route,
          id: document.documentId,
          title: document.label ?? document.documentId,
        }))}
        title={copy.detail.billDocumentsTitle}
      />

      <RelationshipPanel
        emptyState={copy.detail.emptyRelationship}
        items={utilityAccount.receiptDocuments.map((document) => ({
          href: document.route,
          id: document.documentId,
          title: document.label ?? document.documentId,
        }))}
        title={copy.detail.receiptDocumentsTitle}
      />

      <RelationshipPanel
        items={[
          {
            description: utilityAccount.timelineRoute,
            href: utilityAccount.timelineRoute,
            id: 'timeline-link',
            title: copy.detail.relationships.timeline,
          },
        ]}
        title={copy.detail.relationships.timeline}
      />

      <RelationshipPanel
        items={[
          {
            description: utilityAccount.auditRoute,
            href: utilityAccount.auditRoute,
            id: 'audit-link',
            title: copy.detail.relationships.audit,
          },
        ]}
        title={copy.detail.relationships.audit}
      />
    </div>
  )
}

export function UtilityAccountDetailPage({
  onBack,
  onCancel,
  onEdit,
  onMarkPaid,
  utilityAccountId,
}: UtilityAccountDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getUtilityAccountCopy(locale)
  const utilityAccountQuery = useQuery({
    queryFn: () => getUtilityAccount(apiClient, utilityAccountId, locale),
    queryKey: ['utility-accounts', 'detail', utilityAccountId, locale],
  })
  const utilityAccount = utilityAccountQuery.data

  if (utilityAccountQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (utilityAccountQuery.isError || !utilityAccount) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void utilityAccountQuery.refetch()} />
      </section>
    )
  }

  const canEdit = onEdit && !['archived', 'cancelled'].includes(utilityAccount.status.code)
  const canMarkPaid =
    onMarkPaid && !['archived', 'cancelled', 'paid'].includes(utilityAccount.status.code)
  const canCancel =
    onCancel && !['archived', 'cancelled', 'paid'].includes(utilityAccount.status.code)

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        actions={
          <>
            <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
              {copy.detail.actions.back}
            </ActionButton>
            {canMarkPaid ? (
              <ActionButton
                icon={<CheckCircle2 aria-hidden="true" size={16} />}
                onClick={() => onMarkPaid(utilityAccount)}
              >
                {copy.detail.actions.markPaid}
              </ActionButton>
            ) : null}
            {canCancel ? (
              <ActionButton
                icon={<Ban aria-hidden="true" size={16} />}
                onClick={() => onCancel(utilityAccount)}
                tone="danger"
              >
                {copy.detail.actions.cancel}
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
                onClick: () => onEdit(utilityAccount),
              }
            : undefined
        }
        title={utilityAccount.title}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.status,
              value: (
                <StatusBadge
                  label={utilityAccount.status.label}
                  tone={statusTones[utilityAccount.status.code]}
                />
              ),
            },
            {
              label: copy.detail.labels.type,
              value: utilityAccount.type.label,
            },
            {
              label: copy.detail.labels.responsibility,
              value: utilityAccount.responsibility.label,
            },
            {
              label: copy.detail.labels.billingPeriod,
              value: formatPeriod(utilityAccount, locale),
            },
            {
              label: copy.detail.labels.dueDate,
              value: formatDate(utilityAccount.dueDate, { locale }),
            },
            {
              label: copy.detail.labels.amount,
              value: formatMoney(utilityAccount.amount.amount, {
                currency: utilityAccount.amount.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.paidAmount,
              value: formatMoney(utilityAccount.paidAmount.amount, {
                currency: utilityAccount.paidAmount.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.balance,
              value: formatMoney(utilityAccount.balance.amount, {
                currency: utilityAccount.balance.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.paidOn,
              value: utilityAccount.paidOn ? formatDate(utilityAccount.paidOn, { locale }) : '-',
            },
            {
              label: copy.detail.labels.paymentMethod,
              value: utilityAccount.paymentMethod ?? copy.terms.none,
            },
            {
              label: copy.detail.labels.bankReference,
              value: utilityAccount.bankReference ?? copy.terms.none,
            },
            {
              label: copy.detail.labels.property,
              value: formatEntity(utilityAccount.property, copy.terms.none),
            },
            {
              label: copy.detail.labels.contract,
              value: formatEntity(utilityAccount.contract, copy.terms.none),
            },
            {
              label: copy.detail.labels.resident,
              value: formatEntity(utilityAccount.resident, copy.terms.none),
            },
            {
              label: copy.detail.audit.createdAt,
              value: formatDateTime(utilityAccount.createdAt, { locale }),
            },
            {
              label: copy.detail.audit.updatedAt,
              value: utilityAccount.updatedAt
                ? formatDateTime(utilityAccount.updatedAt, { locale })
                : '-',
            },
            {
              label: copy.detail.audit.archivedAt,
              value: utilityAccount.archivedAt
                ? formatDateTime(utilityAccount.archivedAt, { locale })
                : '-',
            },
            {
              label: copy.detail.labels.description,
              value: utilityAccount.description ?? copy.detail.noNotes,
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: getRelationshipPanels(utilityAccount, copy),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
        ]}
      />
    </section>
  )
}
