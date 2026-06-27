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
  UtilityAccountListItem,
  UtilityAccountMarkPaidRequest,
  UtilityPaymentMethod,
} from '../../lib/api/utilityAccounts'
import { utilityPaymentMethods } from '../../lib/api/utilityAccounts'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { UtilityAccountDocumentAttachmentField } from './UtilityAccountDocumentAttachmentField'
import { getUtilityAccountCopy } from './utilityAccountCopy'

export type UtilityAccountMarkPaidDialogProps = {
  isOpen: boolean
  isSubmitting?: boolean
  onOpenChange: (isOpen: boolean) => void
  onSubmit: (request: UtilityAccountMarkPaidRequest) => Promise<void> | void
  utilityAccount?: UtilityAccountListItem
}

type MarkPaidFormValues = {
  bankReference: string
  notes: string
  paidAmount: string
  paidOn: string
  paymentMethod: string
  receiptDocumentId: string
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

const markPaidSchema = z.object({
  bankReference: z.string().trim(),
  notes: z.string().trim(),
  paidAmount: z
    .string()
    .trim()
    .transform(parseRequiredNumber)
    .refine((value) => Number.isFinite(value) && value > 0, 'amountRequired'),
  paidOn: z.string().trim().min(1, 'paidOnRequired'),
  paymentMethod: z
    .string()
    .refine(
      (value): value is UtilityPaymentMethod =>
        utilityPaymentMethods.includes(value as UtilityPaymentMethod),
      'methodRequired',
    ),
  receiptDocumentId: z.string().trim(),
})

function getMessage(key: string | undefined, copy: ReturnType<typeof getUtilityAccountCopy>) {
  if (key === 'amountRequired') {
    return copy.markPaid.amountRequired
  }

  if (key === 'paidOnRequired') {
    return copy.markPaid.paidOnRequired
  }

  return copy.markPaid.amountRequired
}

function buildErrorSummary(
  errors: FieldErrors<MarkPaidFormValues>,
  copy: ReturnType<typeof getUtilityAccountCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function getDefaultValues(utilityAccount?: UtilityAccountListItem): MarkPaidFormValues {
  const suggestedAmount =
    typeof utilityAccount?.balance.amount === 'number' && utilityAccount.balance.amount > 0
      ? utilityAccount.balance.amount
      : utilityAccount?.amount.amount
  const paymentMethod =
    utilityAccount?.paymentMethod &&
    utilityPaymentMethods.includes(utilityAccount.paymentMethod as UtilityPaymentMethod)
      ? utilityAccount.paymentMethod
      : 'pix'

  return {
    bankReference: utilityAccount?.bankReference ?? '',
    notes: '',
    paidAmount: typeof suggestedAmount === 'number' ? String(suggestedAmount) : '',
    paidOn: utilityAccount?.paidOn ?? todayInputValue(),
    paymentMethod,
    receiptDocumentId: '',
  }
}

export function UtilityAccountMarkPaidDialog({
  isOpen,
  isSubmitting = false,
  onOpenChange,
  onSubmit,
  utilityAccount,
}: UtilityAccountMarkPaidDialogProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getUtilityAccountCopy(locale)
  const form = useForm<MarkPaidFormValues>({
    defaultValues: getDefaultValues(utilityAccount),
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)
  const receiptDocumentId = form.watch('receiptDocumentId')
  const paymentMethodOptions = utilityPaymentMethods.map((method) => ({
    label: copy.terms.methods[method],
    value: method,
  }))

  useEffect(() => {
    if (isOpen) {
      form.reset(getDefaultValues(utilityAccount))
    }
  }, [form, isOpen, utilityAccount])

  return (
    <Dialog isOpen={isOpen} onOpenChange={onOpenChange} size="md" title={copy.markPaid.title}>
      <form
        noValidate
        onSubmit={form.handleSubmit(async (values) => {
          form.clearErrors()
          const result = markPaidSchema.safeParse(values)

          if (!result.success) {
            for (const issue of result.error.issues) {
              const field = issue.path[0]

              if (typeof field === 'string' && field in values) {
                form.setError(field as keyof MarkPaidFormValues, { message: issue.message })
              }
            }

            return
          }

          await onSubmit({
            amount: result.data.paidAmount,
            bankReference: optionalText(result.data.bankReference),
            notes: optionalText(result.data.notes),
            paidOn: result.data.paidOn,
            paymentMethod: result.data.paymentMethod,
            receiptDocumentId: optionalText(result.data.receiptDocumentId),
          })
        })}
        style={{ display: 'grid', gap: 16 }}
      >
        <ValidationSummary
          errors={errorSummary}
          fieldLabels={{
            paidAmount: copy.markPaid.amount,
            paidOn: copy.markPaid.paidOn,
          }}
          title={copy.markPaid.validationTitle}
        />

        <div style={fieldGridStyle}>
          <FormField error={errorSummary.paidAmount} label={copy.markPaid.amount} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('paidAmount')}
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
          <FormField error={errorSummary.paidOn} label={copy.markPaid.paidOn} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('paidOn')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                required={isRequired}
                type="date"
              />
            )}
          </FormField>
        </div>

        <FormField label={copy.markPaid.paymentMethod}>
          {({ describedBy, id, isInvalid }) => (
            <SelectInput
              {...form.register('paymentMethod')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              options={paymentMethodOptions}
            />
          )}
        </FormField>

        <FormField label={copy.markPaid.bankReference}>
          {({ describedBy, id, isInvalid }) => (
            <TextInput
              {...form.register('bankReference')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
            />
          )}
        </FormField>

        <UtilityAccountDocumentAttachmentField
          entityId={utilityAccount?.id}
          label={copy.markPaid.receiptDocumentId}
          linkLabel={copy.detail.receiptDocumentsTitle}
          onChange={(documentId) =>
            form.setValue('receiptDocumentId', documentId, {
              shouldDirty: true,
              shouldTouch: true,
            })
          }
          uploadDefaultTitle={
            utilityAccount?.title
              ? `${copy.markPaid.receiptDocumentUploadTitle} - ${utilityAccount.title}`
              : copy.markPaid.receiptDocumentUploadTitle
          }
          value={receiptDocumentId}
        />

        <FormField label={copy.markPaid.notes}>
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
            {copy.markPaid.cancel}
          </ActionButton>
          <ActionButton isLoading={isSubmitting} tone="primary" type="submit">
            {copy.markPaid.submit}
          </ActionButton>
        </div>
      </form>
    </Dialog>
  )
}
