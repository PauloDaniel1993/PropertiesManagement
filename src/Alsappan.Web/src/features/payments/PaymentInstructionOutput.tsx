import { DetailList, DetailSection } from '../../components'
import { formatDate, formatDateTime, formatMoney } from '../../lib/format'
import type { AppLocale } from '../../i18n'
import type { PaymentInstruction } from '../../lib/api/payments'
import { getPaymentCopy } from './paymentCopy'

export type PaymentInstructionOutputProps = {
  instruction?: PaymentInstruction
  locale: AppLocale
}

function getInstructionTitle(
  instruction: PaymentInstruction,
  copy: ReturnType<typeof getPaymentCopy>,
) {
  if (instruction.kind === 'boleto') {
    return copy.instructions.boleto
  }

  if (instruction.kind === 'pix') {
    return copy.instructions.pix
  }

  if (instruction.kind === 'paypal') {
    return copy.instructions.payPal
  }

  return copy.instructions.unknown
}

function CodeBlock({ value }: { value: string }) {
  return (
    <code
      style={{
        background: 'var(--als-color-surface-muted, #f8fafc)',
        border: '1px solid var(--als-color-border, #d7deea)',
        borderRadius: 'var(--als-radius-sm, 6px)',
        display: 'block',
        maxWidth: '100%',
        overflowWrap: 'anywhere',
        padding: 10,
        whiteSpace: 'pre-wrap',
      }}
    >
      {value}
    </code>
  )
}

export function PaymentInstructionOutput({ instruction, locale }: PaymentInstructionOutputProps) {
  const copy = getPaymentCopy(locale)

  if (!instruction) {
    return (
      <p
        style={{
          color: 'var(--als-color-text-muted, #64748b)',
          lineHeight: 1.5,
          margin: 0,
        }}
      >
        {copy.instructions.noOutput}
      </p>
    )
  }

  return (
    <DetailSection title={getInstructionTitle(instruction, copy)}>
      <DetailList
        columns={2}
        items={[
          {
            label: copy.instructions.provider,
            value: instruction.providerCode,
          },
          {
            label: copy.instructions.providerReference,
            value: instruction.providerReference,
          },
          {
            label: copy.instructions.status,
            value: instruction.status,
          },
          {
            label: copy.detail.labels.amount,
            value: formatMoney(instruction.amount.amount, {
              currency: instruction.amount.currency,
              locale,
            }),
          },
          {
            label: copy.instructions.dueDate,
            value: formatDate(instruction.dueDate, { locale }),
          },
          {
            label: copy.instructions.expiresAt,
            value: instruction.expiresAt ? formatDateTime(instruction.expiresAt, { locale }) : '-',
          },
          {
            label: copy.instructions.payer,
            value: instruction.payerSummary,
          },
        ]}
      />

      <div style={{ display: 'grid', gap: 12, marginTop: 14 }}>
        {instruction.barcode ? (
          <div style={{ display: 'grid', gap: 6 }}>
            <strong>{copy.instructions.barcode}</strong>
            <CodeBlock value={instruction.barcode} />
          </div>
        ) : null}

        {instruction.linhaDigitavel ? (
          <div style={{ display: 'grid', gap: 6 }}>
            <strong>{copy.instructions.linhaDigitavel}</strong>
            <CodeBlock value={instruction.linhaDigitavel} />
          </div>
        ) : null}

        {instruction.qrPayload ? (
          <div style={{ display: 'grid', gap: 6 }}>
            <strong>{copy.instructions.qrPayload}</strong>
            <CodeBlock value={instruction.qrPayload} />
          </div>
        ) : null}

        {instruction.copyPasteCode ? (
          <div style={{ display: 'grid', gap: 6 }}>
            <strong>{copy.instructions.copyPasteCode}</strong>
            <CodeBlock value={instruction.copyPasteCode} />
          </div>
        ) : null}

        {instruction.paymentIntentId ? (
          <div style={{ display: 'grid', gap: 6 }}>
            <strong>{copy.instructions.paymentIntentId}</strong>
            <CodeBlock value={instruction.paymentIntentId} />
          </div>
        ) : null}

        {instruction.approvalUrl ? (
          <div style={{ display: 'grid', gap: 6 }}>
            <strong>{copy.instructions.approvalUrl}</strong>
            <a
              href={instruction.approvalUrl}
              style={{ color: 'var(--als-color-primary, #1877f2)', overflowWrap: 'anywhere' }}
            >
              {instruction.approvalUrl}
            </a>
          </div>
        ) : null}
      </div>
    </DetailSection>
  )
}
