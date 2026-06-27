import { useEffect } from 'react'
import { useForm, type FieldErrors } from 'react-hook-form'
import { z } from 'zod'
import {
  ActionButton,
  FormField,
  SelectInput,
  TextAreaInput,
  TextInput,
  ValidationSummary,
} from '../../components'
import type {
  PaymentDetail,
  PaymentFormRequest,
  PaymentListItem,
  PaymentMethod,
  PaymentReconciliationStatus,
} from '../../lib/api/payments'
import { paymentMethods, paymentReconciliationStatuses } from '../../lib/api/payments'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getPaymentCopy } from './paymentCopy'

export type PaymentFormMode = 'create' | 'edit'

export type PaymentFormProps = {
  initialValue?: Partial<PaymentDetail | PaymentListItem>
  isSubmitting?: boolean
  methodOptions: Array<{ disabled?: boolean; label: string; value: PaymentMethod }>
  mode: PaymentFormMode
  onCancel?: () => void
  onSubmit: (values: PaymentFormRequest) => Promise<void> | void
  reconciliationOptions: Array<{
    code: PaymentReconciliationStatus
    label: string
    tone?: string
  }>
}

type PaymentFormValues = {
  amount: string
  contractId: string
  description: string
  discountAmount: string
  dueDate: string
  notes: string
  penaltyAmount: string
  preferredMethod: string
  propertyId: string
  reconciliationStatus: string
  residentId: string
  title: string
  utilityAccountId: string
}

const fieldsetStyle = {
  border: '1px solid var(--als-color-border, #d7deea)',
  borderRadius: 'var(--als-radius-md, 8px)',
  display: 'grid',
  gap: 14,
  margin: 0,
  padding: 14,
} as const

const fieldGridStyle = {
  display: 'grid',
  gap: 12,
  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
} as const

function optionalText(value: string) {
  const trimmedValue = value.trim()

  return trimmedValue.length > 0 ? trimmedValue : undefined
}

function parseOptionalNumber(value: string) {
  const trimmedValue = value.trim()

  if (trimmedValue.length === 0) {
    return undefined
  }

  return Number(trimmedValue)
}

function parseRequiredNumber(value: string) {
  const trimmedValue = value.trim()

  if (trimmedValue.length === 0) {
    return Number.NaN
  }

  return Number(trimmedValue)
}

const paymentFormSchema = z.object({
  amount: z
    .string()
    .trim()
    .transform(parseRequiredNumber)
    .refine((value) => Number.isFinite(value) && value > 0, 'amountRequired'),
  contractId: z.string().trim(),
  description: z.string().trim(),
  discountAmount: z
    .string()
    .trim()
    .transform(parseOptionalNumber)
    .refine((value) => value === undefined || value >= 0, 'amountRequired'),
  dueDate: z.string().trim().min(1, 'dueDateRequired'),
  notes: z.string().trim(),
  penaltyAmount: z
    .string()
    .trim()
    .transform(parseOptionalNumber)
    .refine((value) => value === undefined || value >= 0, 'amountRequired'),
  preferredMethod: z
    .string()
    .refine(
      (value): value is PaymentMethod => paymentMethods.includes(value as PaymentMethod),
      'preferredMethodRequired',
    ),
  propertyId: z.string().trim(),
  reconciliationStatus: z
    .string()
    .refine(
      (value): value is PaymentReconciliationStatus =>
        paymentReconciliationStatuses.includes(value as PaymentReconciliationStatus),
      'reconciliationStatusRequired',
    ),
  residentId: z.string().trim(),
  title: z.string().trim().min(1, 'titleRequired'),
  utilityAccountId: z.string().trim(),
})

function getMessage(key: string | undefined, copy: ReturnType<typeof getPaymentCopy>) {
  if (key === 'amountRequired') {
    return copy.form.amountRequired
  }

  if (key === 'dueDateRequired') {
    return copy.form.dueDateRequired
  }

  if (key === 'preferredMethodRequired') {
    return copy.form.preferredMethodRequired
  }

  if (key === 'reconciliationStatusRequired') {
    return copy.form.reconciliationStatusRequired
  }

  if (key === 'titleRequired') {
    return copy.form.titleRequired
  }

  return copy.form.titleRequired
}

function buildErrorSummary(
  errors: FieldErrors<PaymentFormValues>,
  copy: ReturnType<typeof getPaymentCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function getDefaultValues(
  initialValue?: Partial<PaymentDetail | PaymentListItem>,
): PaymentFormValues {
  return {
    amount:
      typeof initialValue?.amount?.amount === 'number' ? String(initialValue.amount.amount) : '',
    contractId: initialValue?.contract?.id ?? '',
    description: initialValue?.description ?? '',
    discountAmount:
      typeof initialValue?.discountAmount?.amount === 'number'
        ? String(initialValue.discountAmount.amount)
        : '',
    dueDate: initialValue?.dueDate ?? '',
    notes:
      'notes' in (initialValue ?? {}) ? ((initialValue as Partial<PaymentDetail>).notes ?? '') : '',
    penaltyAmount:
      typeof initialValue?.penaltyAmount?.amount === 'number'
        ? String(initialValue.penaltyAmount.amount)
        : '',
    preferredMethod: initialValue?.preferredMethod ?? 'pix',
    propertyId: initialValue?.property?.id ?? '',
    reconciliationStatus:
      'reconciliationStatus' in (initialValue ?? {})
        ? ((initialValue as Partial<PaymentDetail>).reconciliationStatus?.code ?? 'not-required')
        : 'not-required',
    residentId: initialValue?.resident?.id ?? '',
    title: initialValue?.title ?? '',
    utilityAccountId:
      'utilityAccountId' in (initialValue ?? {})
        ? ((initialValue as Partial<PaymentDetail>).utilityAccountId ?? '')
        : '',
  }
}

export function PaymentForm({
  initialValue,
  isSubmitting = false,
  methodOptions,
  mode,
  onCancel,
  onSubmit,
  reconciliationOptions,
}: PaymentFormProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getPaymentCopy(locale)
  const form = useForm<PaymentFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)

  useEffect(() => {
    form.reset(getDefaultValues(initialValue))
  }, [form, initialValue])

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const result = paymentFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof PaymentFormValues, { message: issue.message })
            }
          }

          return
        }

        await onSubmit({
          amount: result.data.amount,
          concurrencyToken:
            'concurrencyToken' in (initialValue ?? {})
              ? (initialValue as Partial<PaymentDetail>).concurrencyToken
              : undefined,
          contractId: optionalText(result.data.contractId),
          description: optionalText(result.data.description),
          discountAmount: result.data.discountAmount,
          dueDate: result.data.dueDate,
          notes: optionalText(result.data.notes),
          penaltyAmount: result.data.penaltyAmount,
          preferredMethod: result.data.preferredMethod,
          propertyId: optionalText(result.data.propertyId),
          reconciliationStatus: result.data.reconciliationStatus,
          residentId: optionalText(result.data.residentId),
          title: result.data.title,
          utilityAccountId: optionalText(result.data.utilityAccountId),
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          amount: copy.form.amount,
          dueDate: copy.form.dueDate,
          preferredMethod: copy.form.preferredMethod,
          reconciliationStatus: copy.form.reconciliationStatus,
          title: copy.form.title,
        }}
        title={copy.form.validationTitle}
      />

      <FormField error={errorSummary.title} label={copy.form.title} required>
        {({ describedBy, id, isInvalid, isRequired }) => (
          <TextInput
            {...form.register('title')}
            id={id}
            aria-describedby={describedBy}
            isInvalid={isInvalid}
            required={isRequired}
          />
        )}
      </FormField>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.financialSection}</legend>
        <div style={fieldGridStyle}>
          <FormField error={errorSummary.amount} label={copy.form.amount} required>
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
          <FormField error={errorSummary.dueDate} label={copy.form.dueDate} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('dueDate')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                required={isRequired}
                type="date"
              />
            )}
          </FormField>
        </div>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.discountAmount}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('discountAmount')}
                id={id}
                aria-describedby={describedBy}
                inputMode="decimal"
                isInvalid={isInvalid}
                min={0}
                step="0.01"
                type="number"
              />
            )}
          </FormField>
          <FormField label={copy.form.penaltyAmount}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('penaltyAmount')}
                id={id}
                aria-describedby={describedBy}
                inputMode="decimal"
                isInvalid={isInvalid}
                min={0}
                step="0.01"
                type="number"
              />
            )}
          </FormField>
        </div>
        <div style={fieldGridStyle}>
          <FormField
            error={errorSummary.preferredMethod}
            label={copy.form.preferredMethod}
            required
          >
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('preferredMethod')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={methodOptions}
                required={isRequired}
              />
            )}
          </FormField>
          <FormField
            error={errorSummary.reconciliationStatus}
            label={copy.form.reconciliationStatus}
            required
          >
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('reconciliationStatus')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={reconciliationOptions.map((option) => ({
                  label: option.label,
                  value: option.code,
                }))}
                required={isRequired}
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>
          {copy.form.identifiersSection}
        </legend>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.contractId}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('contractId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.form.propertyId}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('propertyId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.residentId}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('residentId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.form.utilityAccountId}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('utilityAccountId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <FormField label={copy.form.description}>
        {({ describedBy, id, isInvalid }) => (
          <TextAreaInput
            {...form.register('description')}
            id={id}
            aria-describedby={describedBy}
            isInvalid={isInvalid}
          />
        )}
      </FormField>

      <FormField label={copy.form.notes}>
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
        {onCancel ? (
          <ActionButton onClick={onCancel} type="button">
            {copy.form.cancel}
          </ActionButton>
        ) : null}
        <ActionButton isLoading={isSubmitting} tone="primary" type="submit">
          {mode === 'create' ? copy.form.submitCreate : copy.form.submitEdit}
        </ActionButton>
      </div>
    </form>
  )
}
