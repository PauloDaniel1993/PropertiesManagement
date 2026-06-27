import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useForm, type FieldErrors } from 'react-hook-form'
import { z } from 'zod'
import {
  ActionButton,
  CheckboxInput,
  FormField,
  SelectInput,
  TextAreaInput,
  TextInput,
  ValidationSummary,
} from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { listProperties } from '../../lib/api/properties'
import { listResidents } from '../../lib/api/residents'
import {
  contractAdjustmentIndexes,
  type ContractAdjustmentIndex,
  type ContractDetail,
  type ContractFormRequest,
} from '../../lib/api/leaseContracts'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getContractCopy } from './contractCopy'

export type ContractFormMode = 'create' | 'edit'

export type ContractFormProps = {
  initialValue?: Partial<ContractDetail>
  isSubmitting?: boolean
  mode: ContractFormMode
  onCancel?: () => void
  onSubmit: (values: ContractFormRequest) => Promise<void> | void
}

type ContractFormValues = {
  adjustmentIndex: ContractAdjustmentIndex
  adjustmentIntervalMonths: string
  depositAmount: string
  discountNotes: string
  dueDay: string
  endDate: string
  generatePaymentsAutomatically: boolean
  lifecycleAction: boolean
  monthlyRent: string
  nextAdjustmentDate: string
  notes: string
  penaltyNotes: string
  primaryResidentId: string
  propertyId: string
  residentIds: string[]
  startDate: string
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

const contractFormSchema = z
  .object({
    adjustmentIndex: z
      .string()
      .refine(
        (value): value is ContractAdjustmentIndex =>
          contractAdjustmentIndexes.includes(value as ContractAdjustmentIndex),
        'adjustmentRequired',
      )
      .transform((value) => value as ContractAdjustmentIndex),
    adjustmentIntervalMonths: z
      .string()
      .trim()
      .transform(parseRequiredNumber)
      .refine((value) => Number.isInteger(value) && value >= 0, 'adjustmentIntervalRequired'),
    depositAmount: z
      .string()
      .trim()
      .transform(parseOptionalNumber)
      .refine((value) => value === undefined || value >= 0, 'monthlyRentRequired'),
    discountNotes: z.string().trim(),
    dueDay: z
      .string()
      .trim()
      .transform(parseRequiredNumber)
      .refine((value) => Number.isInteger(value) && value >= 1 && value <= 31, 'dueDayRequired'),
    endDate: z.string().trim(),
    generatePaymentsAutomatically: z.boolean(),
    lifecycleAction: z.boolean(),
    monthlyRent: z
      .string()
      .trim()
      .transform(parseRequiredNumber)
      .refine(
        (value) => typeof value === 'number' && Number.isFinite(value) && value >= 0,
        'monthlyRentRequired',
      ),
    nextAdjustmentDate: z.string().trim(),
    notes: z.string().trim(),
    penaltyNotes: z.string().trim(),
    primaryResidentId: z.string().trim().min(1, 'primaryResidentRequired'),
    propertyId: z.string().trim().min(1, 'propertyRequired'),
    residentIds: z.array(z.string()).min(1, 'residentRequired'),
    startDate: z.string().trim().min(1, 'startDateRequired'),
  })
  .superRefine((values, context) => {
    if (values.endDate && values.endDate < values.startDate) {
      context.addIssue({
        code: 'custom',
        message: 'dateRangeRequired',
        path: ['endDate'],
      })
    }

    if (!values.residentIds.includes(values.primaryResidentId)) {
      context.addIssue({
        code: 'custom',
        message: 'residentRequired',
        path: ['residentIds'],
      })
    }
  })

function getMessage(key: string | undefined, copy: ReturnType<typeof getContractCopy>) {
  if (key === 'adjustmentRequired' || key === 'adjustmentIntervalRequired') {
    return copy.form.adjustmentRequired
  }

  if (key === 'dateRangeRequired') {
    return copy.form.dateRangeRequired
  }

  if (key === 'dueDayRequired') {
    return copy.form.dueDayRequired
  }

  if (key === 'monthlyRentRequired') {
    return copy.form.monthlyRentRequired
  }

  if (key === 'primaryResidentRequired') {
    return copy.form.primaryResidentRequired
  }

  if (key === 'propertyRequired') {
    return copy.form.propertyRequired
  }

  if (key === 'residentRequired') {
    return copy.form.residentRequired
  }

  if (key === 'startDateRequired') {
    return copy.form.startDateRequired
  }

  return copy.form.propertyRequired
}

function buildErrorSummary(
  errors: FieldErrors<ContractFormValues>,
  copy: ReturnType<typeof getContractCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function getDefaultValues(initialValue?: Partial<ContractDetail>): ContractFormValues {
  return {
    adjustmentIndex: initialValue?.adjustmentIndex ?? 'ipca',
    adjustmentIntervalMonths:
      typeof initialValue?.adjustmentIntervalMonths === 'number'
        ? String(initialValue.adjustmentIntervalMonths)
        : '12',
    depositAmount:
      typeof initialValue?.depositAmount?.amount === 'number'
        ? String(initialValue.depositAmount.amount)
        : '',
    discountNotes: initialValue?.discountNotes ?? '',
    dueDay: typeof initialValue?.dueDay === 'number' ? String(initialValue.dueDay) : '10',
    endDate: initialValue?.endDate ?? '',
    generatePaymentsAutomatically: initialValue?.generatePaymentsAutomatically ?? true,
    lifecycleAction: initialValue?.status?.code === 'active',
    monthlyRent:
      typeof initialValue?.monthlyRent?.amount === 'number'
        ? String(initialValue.monthlyRent.amount)
        : '',
    nextAdjustmentDate: initialValue?.nextAdjustmentDate ?? '',
    notes: initialValue?.notes ?? '',
    penaltyNotes: initialValue?.penaltyNotes ?? '',
    primaryResidentId: initialValue?.primaryResident?.id ?? '',
    propertyId: initialValue?.property?.id ?? '',
    residentIds: initialValue?.residents?.map((resident) => resident.id) ?? [],
    startDate: initialValue?.startDate ?? '',
  }
}

export function ContractForm({
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
}: ContractFormProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getContractCopy(locale)
  const form = useForm<ContractFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)
  const propertiesQuery = useQuery({
    queryFn: () => listProperties(apiClient, { pageSize: 100 }),
    queryKey: ['properties', 'contract-picker'],
  })
  const residentsQuery = useQuery({
    queryFn: () => listResidents(apiClient, { pageSize: 100 }),
    queryKey: ['residents', 'contract-picker'],
  })
  const propertyOptions = (propertiesQuery.data?.items ?? []).map((property) => ({
    label: `${property.name}${property.address?.city ? ` - ${property.address.city}` : ''}`,
    value: property.id,
  }))
  const residentOptions = residentsQuery.data?.items ?? []
  const adjustmentOptions = contractAdjustmentIndexes.map((index) => ({
    label: copy.terms.adjustmentIndexes[index],
    value: index,
  }))

  useEffect(() => {
    form.reset(getDefaultValues(initialValue))
  }, [form, initialValue])

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const result = contractFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof ContractFormValues, { message: issue.message })
            }
          }

          return
        }

        await onSubmit({
          adjustmentIndex: result.data.adjustmentIndex,
          adjustmentIntervalMonths: result.data.adjustmentIntervalMonths,
          concurrencyToken: initialValue?.concurrencyToken,
          depositAmount: result.data.depositAmount,
          discountNotes: optionalText(result.data.discountNotes),
          dueDay: result.data.dueDay,
          endDate: optionalText(result.data.endDate),
          generatePaymentsAutomatically: result.data.generatePaymentsAutomatically,
          lifecycleAction: result.data.lifecycleAction ? 'activate' : 'draft',
          monthlyRent: result.data.monthlyRent,
          nextAdjustmentDate: optionalText(result.data.nextAdjustmentDate),
          notes: optionalText(result.data.notes),
          penaltyNotes: optionalText(result.data.penaltyNotes),
          primaryResidentId: result.data.primaryResidentId,
          propertyId: result.data.propertyId,
          residentIds: result.data.residentIds,
          startDate: result.data.startDate,
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          adjustmentIndex: copy.form.adjustmentIndex,
          dueDay: copy.form.dueDay,
          endDate: copy.form.endDate,
          monthlyRent: copy.form.monthlyRent,
          primaryResidentId: copy.form.primaryResident,
          propertyId: copy.form.property,
          residentIds: copy.form.residents,
          startDate: copy.form.startDate,
        }}
        title={copy.form.validationTitle}
      />

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.partiesSection}</legend>
        <FormField error={errorSummary.propertyId} label={copy.form.property} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <SelectInput
              {...form.register('propertyId')}
              id={id}
              aria-describedby={describedBy}
              disabled={mode === 'edit'}
              isInvalid={isInvalid}
              options={propertyOptions}
              placeholder={copy.form.property}
              required={isRequired}
            />
          )}
        </FormField>

        <FormField
          error={errorSummary.primaryResidentId}
          label={copy.form.primaryResident}
          required
        >
          {({ describedBy, id, isInvalid, isRequired }) => (
            <SelectInput
              {...form.register('primaryResidentId')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              options={residentOptions.map((resident) => ({
                label: resident.fullName,
                value: resident.id,
              }))}
              placeholder={copy.form.primaryResident}
              required={isRequired}
            />
          )}
        </FormField>

        <div style={{ display: 'grid', gap: 8 }}>
          <strong>{copy.form.residents}</strong>
          <div
            style={{
              display: 'grid',
              gap: 8,
              gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
            }}
          >
            {residentOptions.map((resident) => (
              <label key={resident.id} style={{ alignItems: 'center', display: 'flex', gap: 8 }}>
                <input
                  {...form.register('residentIds')}
                  style={{
                    accentColor: 'var(--als-color-primary, #1877f2)',
                    height: 18,
                    width: 18,
                  }}
                  type="checkbox"
                  value={resident.id}
                />
                <span>{resident.fullName}</span>
              </label>
            ))}
          </div>
          {errorSummary.residentIds ? (
            <span style={{ color: 'var(--als-color-danger, #b42318)', fontSize: '0.86rem' }}>
              {errorSummary.residentIds.join(' ')}
            </span>
          ) : null}
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.termsSection}</legend>
        <div style={fieldGridStyle}>
          <FormField error={errorSummary.startDate} label={copy.form.startDate} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('startDate')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                required={isRequired}
                type="date"
              />
            )}
          </FormField>
          <FormField error={errorSummary.endDate} label={copy.form.endDate}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('endDate')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                type="date"
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.financialSection}</legend>
        <div style={fieldGridStyle}>
          <FormField error={errorSummary.monthlyRent} label={copy.form.monthlyRent} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('monthlyRent')}
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
          <FormField error={errorSummary.dueDay} label={copy.form.dueDay} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('dueDay')}
                id={id}
                aria-describedby={describedBy}
                inputMode="numeric"
                isInvalid={isInvalid}
                max={31}
                min={1}
                required={isRequired}
                step={1}
                type="number"
              />
            )}
          </FormField>
          <FormField label={copy.form.depositAmount}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('depositAmount')}
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
            error={errorSummary.adjustmentIndex}
            label={copy.form.adjustmentIndex}
            required
          >
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('adjustmentIndex')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={adjustmentOptions}
                required={isRequired}
              />
            )}
          </FormField>
          <FormField label={copy.form.adjustmentIntervalMonths}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('adjustmentIntervalMonths')}
                id={id}
                aria-describedby={describedBy}
                inputMode="numeric"
                isInvalid={isInvalid}
                min={0}
                step={1}
                type="number"
              />
            )}
          </FormField>
          <FormField label={copy.form.nextAdjustmentDate}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('nextAdjustmentDate')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                type="date"
              />
            )}
          </FormField>
        </div>
        <CheckboxInput
          {...form.register('generatePaymentsAutomatically')}
          label={copy.form.generatePaymentsAutomatically}
        />
        {mode === 'create' ? (
          <CheckboxInput {...form.register('lifecycleAction')} label={copy.form.lifecycleAction} />
        ) : null}
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.documentSection}</legend>
        <p style={{ color: 'var(--als-color-text-muted, #64748b)', margin: 0 }}>
          {copy.form.documentSummary}
        </p>
      </fieldset>

      <FormField label={copy.form.penaltyNotes}>
        {({ describedBy, id, isInvalid }) => (
          <TextAreaInput
            {...form.register('penaltyNotes')}
            id={id}
            aria-describedby={describedBy}
            isInvalid={isInvalid}
          />
        )}
      </FormField>
      <FormField label={copy.form.discountNotes}>
        {({ describedBy, id, isInvalid }) => (
          <TextAreaInput
            {...form.register('discountNotes')}
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
