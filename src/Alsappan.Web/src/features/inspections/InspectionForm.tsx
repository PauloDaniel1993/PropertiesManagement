import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
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
import { useApiClient } from '../../lib/api/ApiClientContext'
import { listAdministrators } from '../../lib/api/administrators'
import type { ApiSelectOption } from '../../lib/api/contracts'
import { listLeaseContracts } from '../../lib/api/leaseContracts'
import { listProperties } from '../../lib/api/properties'
import { listResidents } from '../../lib/api/residents'
import {
  inspectionTypes,
  type InspectionDetail,
  type InspectionFormRequest,
  type InspectionListItem,
  type InspectionSignatureSlot,
  type InspectionType,
} from '../../lib/api/inspections'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getInspectionCopy } from './inspectionCopy'

export type InspectionFormMode = 'create' | 'edit'

export type InspectionFormProps = {
  initialValue?: Partial<InspectionDetail | InspectionListItem>
  isSubmitting?: boolean
  mode: InspectionFormMode
  onCancel?: () => void
  onSubmit: (values: InspectionFormRequest) => Promise<void> | void
  typeOptions: Array<ApiSelectOption<InspectionType>>
}

type InspectionFormValues = {
  assignedUserId: string
  contractId: string
  notes: string
  propertyId: string
  residentId: string
  scheduledAt: string
  signatureRole: string
  signatureSigner: string
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
  gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))',
} as const

function optionalText(value: string) {
  const trimmedValue = value.trim()

  return trimmedValue.length > 0 ? trimmedValue : undefined
}

function toDateTimeLocal(value?: string) {
  const date = value ? new Date(value) : new Date(Date.now() + 60 * 60 * 1000)

  if (Number.isNaN(date.getTime())) {
    return ''
  }

  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)

  return localDate.toISOString().slice(0, 16)
}

function toIsoDateTime(value: string) {
  return new Date(value).toISOString()
}

function getSignatureSlot(
  initialValue?: Partial<InspectionDetail | InspectionListItem>,
): InspectionSignatureSlot | undefined {
  if (!initialValue || !('signatureSlots' in initialValue)) {
    return undefined
  }

  return initialValue.signatureSlots?.[0]
}

function getDefaultValues(
  initialValue?: Partial<InspectionDetail | InspectionListItem>,
): InspectionFormValues {
  const signatureSlot = getSignatureSlot(initialValue)

  return {
    assignedUserId: initialValue?.assignee?.id ?? '',
    contractId: initialValue?.contract?.id ?? '',
    notes:
      'notes' in (initialValue ?? {})
        ? ((initialValue as Partial<InspectionDetail>).notes ?? '')
        : '',
    propertyId: initialValue?.property?.id ?? '',
    residentId: initialValue?.resident?.id ?? '',
    scheduledAt: toDateTimeLocal(initialValue?.scheduledAt),
    signatureRole: signatureSlot?.signerRole ?? '',
    signatureSigner: signatureSlot?.signerName ?? '',
    title: initialValue?.title ?? '',
    type: initialValue?.type?.code ?? 'periodic',
  }
}

const inspectionFormSchema = z.object({
  assignedUserId: z.string().trim().min(1, 'assigneeRequired'),
  contractId: z.string().trim(),
  notes: z.string().trim(),
  propertyId: z.string().trim().min(1, 'propertyRequired'),
  residentId: z.string().trim(),
  scheduledAt: z.string().trim().min(1, 'scheduledAtRequired'),
  signatureRole: z.string().trim(),
  signatureSigner: z.string().trim(),
  title: z.string().trim(),
  type: z
    .string()
    .refine(
      (value): value is InspectionType => inspectionTypes.includes(value as InspectionType),
      'typeRequired',
    ),
})

function getMessage(key: string | undefined, copy: ReturnType<typeof getInspectionCopy>) {
  if (key === 'assigneeRequired') {
    return copy.form.assigneeRequired
  }

  if (key === 'propertyRequired') {
    return copy.form.propertyRequired
  }

  if (key === 'scheduledAtRequired') {
    return copy.form.scheduledAtRequired
  }

  if (key === 'typeRequired') {
    return copy.form.typeRequired
  }

  return copy.form.typeRequired
}

function buildErrorSummary(
  errors: FieldErrors<InspectionFormValues>,
  copy: ReturnType<typeof getInspectionCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function withCurrentOption(
  options: Array<ApiSelectOption<string>>,
  selectedId: string | undefined,
  selectedLabel: string | undefined,
) {
  if (!selectedId || options.some((option) => option.value === selectedId)) {
    return options
  }

  return [{ label: selectedLabel ?? selectedId, value: selectedId }, ...options]
}

export function InspectionForm({
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
  typeOptions,
}: InspectionFormProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getInspectionCopy(locale)
  const form = useForm<InspectionFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)
  const propertiesQuery = useQuery({
    queryFn: () => listProperties(apiClient, { pageSize: 100 }),
    queryKey: ['properties', 'inspection-picker'],
  })
  const contractsQuery = useQuery({
    queryFn: () => listLeaseContracts(apiClient, { includeArchived: false, locale, pageSize: 100 }),
    queryKey: ['contracts', 'inspection-picker', locale],
  })
  const residentsQuery = useQuery({
    queryFn: () => listResidents(apiClient, { pageSize: 100 }),
    queryKey: ['residents', 'inspection-picker'],
  })
  const administratorsQuery = useQuery({
    queryFn: () => listAdministrators(apiClient, { pageSize: 100, status: 'active' }),
    queryKey: ['administrators', 'inspection-picker'],
  })
  const propertyOptions = withCurrentOption(
    (propertiesQuery.data?.items ?? []).map((property) => ({
      label: `${property.name}${property.address?.city ? ` - ${property.address.city}` : ''}`,
      value: property.id,
    })),
    initialValue?.property?.id,
    initialValue?.property?.name,
  )
  const contractOptions = withCurrentOption(
    (contractsQuery.data?.items ?? []).map((contract) => ({
      label: `${contract.property.name} - ${contract.primaryResident.name}`,
      value: contract.id,
    })),
    initialValue?.contract?.id,
    initialValue?.contract?.name,
  )
  const residentOptions = withCurrentOption(
    (residentsQuery.data?.items ?? []).map((resident) => ({
      label: resident.fullName,
      value: resident.id,
    })),
    initialValue?.resident?.id,
    initialValue?.resident?.name,
  )
  const assigneeOptions = withCurrentOption(
    (administratorsQuery.data?.items ?? []).map((administrator) => ({
      label: administrator.displayName,
      value: administrator.id,
    })),
    initialValue?.assignee?.id,
    initialValue?.assignee?.name,
  )

  useEffect(() => {
    form.reset(getDefaultValues(initialValue))
  }, [form, initialValue])

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const result = inspectionFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof InspectionFormValues, { message: issue.message })
            }
          }

          return
        }

        const signatureRole = optionalText(result.data.signatureRole)
        const signatureSigner = optionalText(result.data.signatureSigner)

        await onSubmit({
          assignedUserId: result.data.assignedUserId,
          concurrencyToken:
            'concurrencyToken' in (initialValue ?? {})
              ? (initialValue as Partial<InspectionDetail>).concurrencyToken
              : undefined,
          contractId: optionalText(result.data.contractId),
          notes: optionalText(result.data.notes),
          propertyId: result.data.propertyId,
          residentId: optionalText(result.data.residentId),
          scheduledAt: toIsoDateTime(result.data.scheduledAt),
          signatureSlots: signatureRole
            ? [
                {
                  isRequired: false,
                  signerName: signatureSigner,
                  signerRole: signatureRole,
                },
              ]
            : undefined,
          title: optionalText(result.data.title),
          type: result.data.type,
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          assignedUserId: copy.form.assignedUserId,
          propertyId: copy.form.propertyId,
          scheduledAt: copy.form.scheduledAt,
          type: copy.form.type,
        }}
        title={copy.form.validationTitle}
      />

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.metadataSection}</legend>
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
          <FormField error={errorSummary.scheduledAt} label={copy.form.scheduledAt} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('scheduledAt')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                required={isRequired}
                type="datetime-local"
              />
            )}
          </FormField>
          <FormField error={errorSummary.assignedUserId} label={copy.form.assignedUserId} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('assignedUserId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={assigneeOptions}
                placeholder={copy.form.assignedUserId}
                required={isRequired}
              />
            )}
          </FormField>
        </div>
        <FormField label={copy.form.title}>
          {({ describedBy, id, isInvalid }) => (
            <TextInput
              {...form.register('title')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
            />
          )}
        </FormField>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>
          {copy.detail.relationshipTitle}
        </legend>
        <div style={fieldGridStyle}>
          <FormField error={errorSummary.propertyId} label={copy.form.propertyId} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('propertyId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={propertyOptions}
                placeholder={copy.form.propertyId}
                required={isRequired}
              />
            )}
          </FormField>
          <FormField label={copy.form.contractId}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                {...form.register('contractId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={contractOptions}
                placeholder={copy.form.contractId}
              />
            )}
          </FormField>
          <FormField label={copy.form.residentId}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                {...form.register('residentId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={residentOptions}
                placeholder={copy.form.residentId}
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.signatureSection}</legend>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.signatureRole}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('signatureRole')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.form.signatureSigner}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('signatureSigner')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>
      </fieldset>

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
