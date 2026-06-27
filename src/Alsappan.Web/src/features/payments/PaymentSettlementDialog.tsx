import { useEffect } from 'react'
import { useForm, type FieldErrors } from 'react-hook-form'
import { z } from 'zod'
import {
  ActionButton,
  Dialog,
  FormField,
  SelectInput,
  TextAreaInput,
  TextInput,
  ValidationSummary,
} from '../../components'
import type {
  PaymentListItem,
  PaymentMethod,
  PaymentTransactionRequest,
} from '../../lib/api/payments'
import { paymentMethods } from '../../lib/api/payments'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getPaymentCopy } from './paymentCopy'

export type PaymentSettlementDialogProps = {
  isOpen: boolean
  isSubmitting?: boolean
  methodOptions: Array<{ disabled?: boolean; label: string; value: PaymentMethod }>
  onOpenChange: (isOpen: boolean) => void
  onSubmit: (request: PaymentTransactionRequest) => Promise<void> | void
  payment?: PaymentListItem
}

type SettlementFormValues = {
  amount: string
  bankReference: string
  method: string
  notes: string
  providerCode: string
  providerReference: string
  receiptDocumentId: string
  settledOn: string
}

const fieldGridStyle = {
  display: 'grid',
  gap: 12,
  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
} as const

function todayInputValue() {
  const now = new Date()
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)

  return local.toISOString().slice(0, 10)
}

function optionalText(value: string) {
  const trimmedValue = value.trim()

  return trimmedValue.length > 0 ? trimmedValue : undefined
}

function parseRequiredNumber(value: string) {
  const trimmedValue = value.trim()

  if (trimmedValue.length === 0) {
    return Number.NaN
  }

  return Number(trimmedValue)
}

const settlementSchema = z.object({
  amount: z
    .string()
    .trim()
    .transform(parseRequiredNumber)
    .refine((value) => Number.isFinite(value) && value > 0, 'amountRequired'),
  bankReference: z.string().trim(),
  method: z
    .string()
    .refine(
      (value): value is PaymentMethod => paymentMethods.includes(value as PaymentMethod),
      'methodRequired',
    ),
  notes: z.string().trim(),
  providerCode: z.string().trim(),
  providerReference: z.string().trim(),
  receiptDocumentId: z.string().trim(),
  settledOn: z.string().trim().min(1, 'settledOnRequired'),
})

function getMessage(key: string | undefined, copy: ReturnType<typeof getPaymentCopy>) {
  if (key === 'amountRequired') {
    return copy.settlement.amountRequired
  }

  if (key === 'methodRequired') {
    return copy.settlement.methodRequired
  }

  if (key === 'settledOnRequired') {
    return copy.settlement.settledOnRequired
  }

  return copy.settlement.amountRequired
}

function buildErrorSummary(
  errors: FieldErrors<SettlementFormValues>,
  copy: ReturnType<typeof getPaymentCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function getDefaultValues(payment?: PaymentListItem): SettlementFormValues {
  const suggestedAmount =
    typeof payment?.balance.amount === 'number' && payment.balance.amount > 0
      ? payment.balance.amount
      : payment?.amount.amount

  return {
    amount: typeof suggestedAmount === 'number' ? String(suggestedAmount) : '',
    bankReference: '',
    method: payment?.preferredMethod ?? 'pix',
    notes: '',
    providerCode: '',
    providerReference: '',
    receiptDocumentId: '',
    settledOn: todayInputValue(),
  }
}

export function PaymentSettlementDialog({
  isOpen,
  isSubmitting = false,
  methodOptions,
  onOpenChange,
  onSubmit,
  payment,
}: PaymentSettlementDialogProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getPaymentCopy(locale)
  const form = useForm<SettlementFormValues>({
    defaultValues: getDefaultValues(payment),
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)

  useEffect(() => {
    if (isOpen) {
      form.reset(getDefaultValues(payment))
    }
  }, [form, isOpen, payment])

  return (
    <Dialog isOpen={isOpen} onOpenChange={onOpenChange} size="md" title={copy.settlement.title}>
      <form
        noValidate
        onSubmit={form.handleSubmit(async (values) => {
          form.clearErrors()
          const result = settlementSchema.safeParse(values)

          if (!result.success) {
            for (const issue of result.error.issues) {
              const field = issue.path[0]

              if (typeof field === 'string' && field in values) {
                form.setError(field as keyof SettlementFormValues, { message: issue.message })
              }
            }

            return
          }

          await onSubmit({
            amount: result.data.amount,
            bankReference: optionalText(result.data.bankReference),
            method: result.data.method as PaymentMethod,
            notes: optionalText(result.data.notes),
            providerCode: optionalText(result.data.providerCode),
            providerReference: optionalText(result.data.providerReference),
            receiptDocumentId: optionalText(result.data.receiptDocumentId),
            settledOn: result.data.settledOn,
          })
        })}
        style={{ display: 'grid', gap: 16 }}
      >
        <ValidationSummary
          errors={errorSummary}
          fieldLabels={{
            amount: copy.settlement.amount,
            method: copy.settlement.method,
            settledOn: copy.settlement.settledOn,
          }}
          title={copy.settlement.validationTitle}
        />

        <div style={fieldGridStyle}>
          <FormField error={errorSummary.amount} label={copy.settlement.amount} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('amount')}
                id={id}
                aria-describedby={describedBy}
                inputMode="decimal"
                isInvalid={isInvalid}
                min={0}
                required={isRequired}
                step="0.01"
                type="number"
              />
            )}
          </FormField>
          <FormField error={errorSummary.settledOn} label={copy.settlement.settledOn} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('settledOn')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                required={isRequired}
                type="date"
              />
            )}
          </FormField>
        </div>

        <FormField error={errorSummary.method} label={copy.settlement.method} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <SelectInput
              {...form.register('method')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              options={methodOptions}
              required={isRequired}
            />
          )}
        </FormField>

        <div style={fieldGridStyle}>
          <FormField label={copy.settlement.bankReference}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('bankReference')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.settlement.receiptDocumentId}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('receiptDocumentId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>

        <div style={fieldGridStyle}>
          <FormField label={copy.settlement.providerCode}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('providerCode')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.settlement.providerReference}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('providerReference')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>

        <FormField label={copy.settlement.notes}>
          {({ describedBy, id, isInvalid }) => (
            <TextAreaInput
              {...form.register('notes')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
            />
          )}
        </FormField>

        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, justifyContent: 'flex-end' }}>
          <ActionButton onClick={() => onOpenChange(false)} type="button">
            {copy.settlement.cancel}
          </ActionButton>
          <ActionButton isLoading={isSubmitting} tone="primary" type="submit">
            {copy.settlement.submit}
          </ActionButton>
        </div>
      </form>
    </Dialog>
  )
}
