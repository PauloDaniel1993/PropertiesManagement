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
import type { ApiSelectOption } from '../../lib/api/contracts'
import type {
  UtilityAccountDetail,
  UtilityAccountFormRequest,
  UtilityAccountListItem,
  UtilityAccountResponsibility,
  UtilityAccountType,
} from '../../lib/api/utilityAccounts'
import { utilityAccountResponsibilities, utilityAccountTypes } from '../../lib/api/utilityAccounts'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { UtilityAccountDocumentAttachmentField } from './UtilityAccountDocumentAttachmentField'
import { getUtilityAccountCopy } from './utilityAccountCopy'

export type UtilityAccountFormMode = 'create' | 'edit'

export type UtilityAccountFormProps = {
  canReadDocuments?: boolean
  initialValue?: Partial<UtilityAccountDetail | UtilityAccountListItem>
  isSubmitting?: boolean
  mode: UtilityAccountFormMode
  onCancel?: () => void
  onSubmit: (values: UtilityAccountFormRequest) => Promise<void> | void
  responsibilityOptions: Array<ApiSelectOption<UtilityAccountResponsibility>>
  typeOptions: Array<ApiSelectOption<UtilityAccountType>>
}

type UtilityAccountFormValues = {
  amount: string
  billDocumentId: string
  billingPeriodEnd: string
  billingPeriodStart: string
  contractId: string
  description: string
  dueDate: string
  propertyId: string
  residentId: string
  responsibility: string
  title: string
  type: string
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

function parseRequiredNumber(value: string) {
  const trimmedValue = value.trim()

  if (trimmedValue.length === 0) {
    return Number.NaN
  }

  return Number(trimmedValue)
}

const utilityAccountFormSchema = z.object({
  amount: z
    .string()
    .trim()
    .transform(parseRequiredNumber)
    .refine((value) => Number.isFinite(value) && value > 0, 'amountRequired'),
  billDocumentId: z.string().trim(),
  billingPeriodEnd: z.string().trim().min(1, 'billingPeriodEndRequired'),
  billingPeriodStart: z.string().trim().min(1, 'billingPeriodStartRequired'),
  contractId: z.string().trim(),
  description: z.string().trim(),
  dueDate: z.string().trim().min(1, 'dueDateRequired'),
  propertyId: z.string().trim(),
  residentId: z.string().trim(),
  responsibility: z
    .string()
    .refine(
      (value): value is UtilityAccountResponsibility =>
        utilityAccountResponsibilities.includes(value as UtilityAccountResponsibility),
      'responsibilityRequired',
    ),
  title: z.string().trim().min(1, 'titleRequired'),
  type: z
    .string()
    .refine(
      (value): value is UtilityAccountType =>
        utilityAccountTypes.includes(value as UtilityAccountType),
      'typeRequired',
    ),
})

function getMessage(key: string | undefined, copy: ReturnType<typeof getUtilityAccountCopy>) {
  if (key === 'amountRequired') {
    return copy.form.amountRequired
  }

  if (key === 'billingPeriodStartRequired') {
    return copy.form.billingPeriodStartRequired
  }

  if (key === 'billingPeriodEndRequired') {
    return copy.form.billingPeriodEndRequired
  }

  if (key === 'dueDateRequired') {
    return copy.form.dueDateRequired
  }

  if (key === 'responsibilityRequired') {
    return copy.form.responsibilityRequired
  }

  if (key === 'titleRequired') {
    return copy.form.titleRequired
  }

  if (key === 'typeRequired') {
    return copy.form.typeRequired
  }

  return copy.form.titleRequired
}

function buildErrorSummary(
  errors: FieldErrors<UtilityAccountFormValues>,
  copy: ReturnType<typeof getUtilityAccountCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function formatFirstDocumentId(documents: UtilityAccountListItem['billDocuments']) {
  return documents[0]?.documentId ?? ''
}

function getDefaultValues(
  initialValue?: Partial<UtilityAccountDetail | UtilityAccountListItem>,
): UtilityAccountFormValues {
  return {
    amount:
      typeof initialValue?.amount?.amount === 'number' ? String(initialValue.amount.amount) : '',
    billDocumentId: formatFirstDocumentId(initialValue?.billDocuments ?? []),
    billingPeriodEnd: initialValue?.billingPeriodEnd ?? '',
    billingPeriodStart: initialValue?.billingPeriodStart ?? '',
    contractId: initialValue?.contract?.id ?? '',
    description: initialValue?.description ?? '',
    dueDate: initialValue?.dueDate ?? '',
    propertyId: initialValue?.property?.id ?? '',
    residentId: initialValue?.resident?.id ?? '',
    responsibility: initialValue?.responsibility?.code ?? 'organization',
    title: initialValue?.title ?? '',
    type: initialValue?.type?.code ?? 'electricity',
  }
}

export function UtilityAccountForm({
  canReadDocuments = false,
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
  responsibilityOptions,
  typeOptions,
}: UtilityAccountFormProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getUtilityAccountCopy(locale)
  const form = useForm<UtilityAccountFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)
  const billDocumentId = form.watch('billDocumentId')

  useEffect(() => {
    form.reset(getDefaultValues(initialValue))
  }, [form, initialValue])

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const result = utilityAccountFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof UtilityAccountFormValues, { message: issue.message })
            }
          }

          return
        }

        await onSubmit({
          amount: result.data.amount,
          billDocumentId: optionalText(result.data.billDocumentId),
          billingPeriodEnd: result.data.billingPeriodEnd,
          billingPeriodStart: result.data.billingPeriodStart,
          concurrencyToken:
            'concurrencyToken' in (initialValue ?? {})
              ? (initialValue as Partial<UtilityAccountDetail>).concurrencyToken
              : undefined,
          contractId: optionalText(result.data.contractId),
          description: optionalText(result.data.description),
          dueDate: result.data.dueDate,
          propertyId: optionalText(result.data.propertyId),
          residentId: optionalText(result.data.residentId),
          responsibility: result.data.responsibility,
          title: result.data.title,
          type: result.data.type,
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          amount: copy.form.amount,
          billingPeriodEnd: copy.form.billingPeriodEnd,
          billingPeriodStart: copy.form.billingPeriodStart,
          dueDate: copy.form.dueDate,
          responsibility: copy.form.responsibility,
          title: copy.form.title,
          type: copy.form.type,
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
          <FormField error={errorSummary.type} label={copy.form.type} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('type')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={typeOptions}
                required={isRequired}
              />
            )}
          </FormField>
          <FormField error={errorSummary.responsibility} label={copy.form.responsibility} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('responsibility')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={responsibilityOptions}
                required={isRequired}
              />
            )}
          </FormField>
        </div>
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
          <FormField
            error={errorSummary.billingPeriodStart}
            label={copy.form.billingPeriodStart}
            required
          >
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('billingPeriodStart')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                required={isRequired}
                type="date"
              />
            )}
          </FormField>
          <FormField
            error={errorSummary.billingPeriodEnd}
            label={copy.form.billingPeriodEnd}
            required
          >
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('billingPeriodEnd')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                required={isRequired}
                type="date"
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
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>Documentos</legend>
        <p style={{ color: 'var(--als-color-text-muted, #64748b)', margin: 0 }}>
          {copy.form.documentHint}
        </p>
        <UtilityAccountDocumentAttachmentField
          canReadDocuments={canReadDocuments}
          label={copy.form.billDocumentId}
          onChange={(documentId) =>
            form.setValue('billDocumentId', documentId, {
              shouldDirty: true,
              shouldTouch: true,
            })
          }
          value={billDocumentId}
        />
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
