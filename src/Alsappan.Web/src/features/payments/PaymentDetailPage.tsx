import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Pencil, QrCode, ReceiptText, RotateCcw } from 'lucide-react'
import {
  ActionButton,
  DataTable,
  DetailList,
  DetailSection,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
  RelationshipPanel,
  StatusBadge,
  Tabs,
  type StatusBadgeTone,
} from '../../components'
import {
  getPayment,
  type PaymentDetail,
  type PaymentStatus,
  type PaymentTransaction,
} from '../../lib/api/payments'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDate, formatDateTime, formatMoney } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getPaymentCopy } from './paymentCopy'

export type PaymentDetailPageProps = {
  onBack: () => void
  onEdit?: (payment: PaymentDetail) => void
  onInstructions?: (payment: PaymentDetail) => void
  onReverseTransaction?: (payment: PaymentDetail, transaction: PaymentTransaction) => void
  onSettle?: (payment: PaymentDetail) => void
  paymentId: string
}

const statusTones: Record<PaymentStatus, StatusBadgeTone> = {
  archived: 'archived',
  cancelled: 'archived',
  disputed: 'danger',
  overdue: 'danger',
  paid: 'success',
  'partially-paid': 'warning',
  pending: 'warning',
}

const allowedTones: StatusBadgeTone[] = [
  'archived',
  'danger',
  'info',
  'neutral',
  'success',
  'warning',
]

function coerceTone(tone: string | undefined, fallback: StatusBadgeTone) {
  return allowedTones.includes(tone as StatusBadgeTone) ? (tone as StatusBadgeTone) : fallback
}

function formatEntity(entity: PaymentDetail['property'], emptyLabel: string) {
  if (!entity) {
    return emptyLabel
  }

  return entity.description ? `${entity.name} | ${entity.description}` : entity.name
}

function getRelationshipPanels(payment: PaymentDetail, copy: ReturnType<typeof getPaymentCopy>) {
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
        items={payment.receiptDocuments.map((document) => ({
          href: document.route,
          id: document.documentId,
          title: document.label ?? document.documentId,
        }))}
        title={copy.detail.documentsTitle}
      />

      <RelationshipPanel
        items={[
          {
            description: payment.timelineRoute,
            href: payment.timelineRoute,
            id: 'timeline-link',
            title: 'Timeline',
          },
        ]}
        title="Timeline"
      />

      <RelationshipPanel
        items={[
          {
            description: payment.auditRoute,
            href: payment.auditRoute,
            id: 'audit-link',
            title: 'Auditoria',
          },
        ]}
        title="Auditoria"
      />
    </div>
  )
}

export function PaymentDetailPage({
  onBack,
  onEdit,
  onInstructions,
  onReverseTransaction,
  onSettle,
  paymentId,
}: PaymentDetailPageProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getPaymentCopy(locale)
  const paymentQuery = useQuery({
    queryFn: () => getPayment(apiClient, paymentId, locale),
    queryKey: ['payments', 'detail', paymentId, locale],
  })
  const payment = paymentQuery.data

  if (paymentQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (paymentQuery.isError || !payment) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void paymentQuery.refetch()} />
      </section>
    )
  }

  const canEditPayment = onEdit && !['archived', 'cancelled'].includes(payment.status.code)
  const canSettlePayment =
    onSettle && !['archived', 'cancelled', 'paid'].includes(payment.status.code)

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        actions={
          <>
            <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
              {copy.detail.actions.back}
            </ActionButton>
            {canSettlePayment ? (
              <ActionButton
                icon={<ReceiptText aria-hidden="true" size={16} />}
                onClick={() => onSettle(payment)}
              >
                {copy.detail.actions.settle}
              </ActionButton>
            ) : null}
            {onInstructions ? (
              <ActionButton
                icon={<QrCode aria-hidden="true" size={16} />}
                onClick={() => onInstructions(payment)}
              >
                {copy.detail.actions.instructions}
              </ActionButton>
            ) : null}
          </>
        }
        description={copy.detail.pageDescription}
        primaryAction={
          canEditPayment
            ? {
                icon: <Pencil aria-hidden="true" size={18} />,
                label: copy.detail.actions.edit,
                onClick: () => onEdit(payment),
              }
            : undefined
        }
        title={payment.title}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.status,
              value: (
                <StatusBadge label={payment.status.label} tone={statusTones[payment.status.code]} />
              ),
            },
            {
              label: copy.detail.labels.reconciliation,
              value: (
                <StatusBadge
                  label={payment.reconciliationStatus.label}
                  tone={coerceTone(payment.reconciliationStatus.tone, 'neutral')}
                />
              ),
            },
            {
              label: copy.detail.labels.dueDate,
              value: formatDate(payment.dueDate, { locale }),
            },
            {
              label: copy.detail.labels.amount,
              value: formatMoney(payment.amount.amount, {
                currency: payment.amount.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.grossAmount,
              value: formatMoney(payment.grossAmount.amount, {
                currency: payment.grossAmount.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.balance,
              value: formatMoney(payment.balance.amount, {
                currency: payment.balance.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.discount,
              value: formatMoney(payment.discountAmount.amount, {
                currency: payment.discountAmount.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.penalty,
              value: formatMoney(payment.penaltyAmount.amount, {
                currency: payment.penaltyAmount.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.settledAmount,
              value: formatMoney(payment.settledAmount.amount, {
                currency: payment.settledAmount.currency,
                locale,
              }),
            },
            {
              label: copy.detail.labels.method,
              value: payment.preferredMethodLabel,
            },
            {
              label: copy.detail.labels.property,
              value: formatEntity(payment.property, copy.terms.none),
            },
            {
              label: copy.detail.labels.resident,
              value: formatEntity(payment.resident, copy.terms.none),
            },
            {
              label: copy.detail.labels.contract,
              value: formatEntity(payment.contract, copy.terms.none),
            },
            {
              label: copy.detail.audit.createdAt,
              value: formatDateTime(payment.createdAt, { locale }),
            },
            {
              label: copy.detail.audit.updatedAt,
              value: payment.updatedAt ? formatDateTime(payment.updatedAt, { locale }) : '-',
            },
            {
              label: copy.detail.labels.description,
              value: payment.description ?? copy.detail.noNotes,
            },
            {
              label: copy.detail.labels.notes,
              value: payment.notes ?? copy.detail.noNotes,
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: (
              <DataTable
                columns={[
                  {
                    cell: (transaction) =>
                      formatDate(transaction.settledOn, {
                        locale,
                      }),
                    header: copy.settlement.settledOn,
                    id: 'settledOn',
                    width: 140,
                  },
                  {
                    cell: (transaction) => transaction.methodLabel,
                    header: copy.settlement.method,
                    id: 'method',
                    width: 150,
                  },
                  {
                    cell: (transaction) =>
                      formatMoney(transaction.amount.amount, {
                        currency: transaction.amount.currency,
                        locale,
                      }),
                    header: copy.settlement.amount,
                    id: 'amount',
                    width: 140,
                  },
                  {
                    cell: (transaction) =>
                      transaction.providerReference ??
                      transaction.bankReference ??
                      transaction.receiptDocumentId ??
                      '-',
                    header: copy.instructions.providerReference,
                    id: 'reference',
                  },
                  {
                    align: 'right',
                    cell: (transaction) =>
                      transaction.isReversed ? (
                        <StatusBadge label={copy.detail.reversed} tone="archived" />
                      ) : onReverseTransaction ? (
                        <ActionButton
                          icon={<RotateCcw aria-hidden="true" size={16} />}
                          onClick={() => onReverseTransaction(payment, transaction)}
                          size="sm"
                        >
                          {copy.detail.actions.reverse}
                        </ActionButton>
                      ) : null,
                    header: copy.list.columns.actions,
                    id: 'actions',
                    width: 140,
                  },
                ]}
                emptyState={<EmptyState title={copy.detail.emptyTransactions} />}
                getRowKey={(transaction) => transaction.id}
                rows={payment.transactions}
              />
            ),
            id: 'transactions',
            label: copy.detail.transactionsTitle,
          },
          {
            content: getRelationshipPanels(payment, copy),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
        ]}
      />
    </section>
  )
}
