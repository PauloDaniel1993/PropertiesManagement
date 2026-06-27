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
import {
  occurrencePriorities,
  occurrenceTypes,
  type OccurrenceDetail,
  type OccurrenceFormRequest,
  type OccurrenceListItem,
  type OccurrencePriority,
  type OccurrenceType,
} from '../../lib/api/occurrences'
import { listProperties } from '../../lib/api/properties'
import { listResidents } from '../../lib/api/residents'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getOccurrenceCopy } from './occurrenceCopy'

export type OccurrenceFormMode = 'create' | 'edit'

export type OccurrenceFormProps = {
  initialValue?: Partial<OccurrenceDetail | OccurrenceListItem>
  isSubmitting?: boolean
  mode: OccurrenceFormMode
  onCancel?: () => void
  onSubmit: (values: OccurrenceFormRequest) => Promise<void> | void
  priorityOptions: Array<ApiSelectOption<OccurrencePriority>>
  typeOptions: Array<ApiSelectOption<OccurrenceType>>
}

type OccurrenceFormValues = {
  assignedUserId: string
  contractId: string
  description: string
  dueDate: string
  priority: string
  propertyId: string
  residentId: string
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

const occurrenceFormSchema = z
  .object({
    assignedUserId: z.string().trim(),
    contractId: z.string().trim(),
    description: z.string().trim().min(1, 'descriptionRequired'),
    dueDate: z.string().trim(),
    priority: z
      .string()
      .refine(
        (value): value is OccurrencePriority =>
          occurrencePriorities.includes(value as OccurrencePriority),
        'priorityRequired',
      ),
    propertyId: z.string().trim(),
    residentId: z.string().trim(),
    title: z.string().trim().min(1, 'titleRequired'),
    type: z
      .string()
      .refine(
        (value): value is OccurrenceType => occurrenceTypes.includes(value as OccurrenceType),
        'typeRequired',
      ),
  })
  .refine(
    (value) =>
      value.propertyId.length > 0 || value.residentId.length > 0 || value.contractId.length > 0,
    {
      message: 'propertyRequired',
      path: ['propertyId'],
    },
  )

function getMessage(key: string | undefined, copy: ReturnType<typeof getOccurrenceCopy>) {
  if (key === 'descriptionRequired') {
    return copy.form.descriptionRequired
  }

  if (key === 'priorityRequired') {
    return copy.form.priorityRequired
  }

  if (key === 'propertyRequired') {
    return copy.form.propertyRequired
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
  errors: FieldErrors<OccurrenceFormValues>,
  copy: ReturnType<typeof getOccurrenceCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function getDefaultValues(
  initialValue?: Partial<OccurrenceDetail | OccurrenceListItem>,
): OccurrenceFormValues {
  return {
    assignedUserId: initialValue?.assignedUser?.id ?? '',
    contractId: initialValue?.contract?.id ?? '',
    description: initialValue?.description ?? '',
    dueDate: initialValue?.dueDate?.slice(0, 10) ?? '',
    priority: initialValue?.priority?.code ?? 'medium',
    propertyId: initialValue?.property?.id ?? '',
    residentId: initialValue?.resident?.id ?? '',
    title: initialValue?.title ?? '',
    type: initialValue?.type?.code ?? 'maintenance',
  }
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

export function OccurrenceForm({
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
  priorityOptions,
  typeOptions,
}: OccurrenceFormProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getOccurrenceCopy(locale)
  const form = useForm<OccurrenceFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)
  const administratorsQuery = useQuery({
    queryFn: () => listAdministrators(apiClient, { pageSize: 100, status: 'active' }),
    queryKey: ['administrators', 'occurrence-assignee-picker'],
  })
  const residentsQuery = useQuery({
    queryFn: () => listResidents(apiClient, { locale, pageSize: 100 }),
    queryKey: ['residents', 'occurrence-picker', locale],
  })
  const propertiesQuery = useQuery({
    queryFn: () => listProperties(apiClient, { pageSize: 100 }),
    queryKey: ['properties', 'occurrence-picker'],
  })
  const contractsQuery = useQuery({
    queryFn: () => listLeaseContracts(apiClient, { includeArchived: false, locale, pageSize: 100 }),
    queryKey: ['contracts', 'occurrence-picker', locale],
  })
  const assigneeOptions = withCurrentOption(
    (administratorsQuery.data?.items ?? []).map((administrator) => ({
      label: `${administrator.displayName} (${administrator.email})`,
      value: administrator.id,
    })),
    initialValue?.assignedUser?.id,
    initialValue?.assignedUser?.displayName,
  )
  const residentOptions = withCurrentOption(
    (residentsQuery.data?.items ?? []).map((resident) => ({
      label: resident.fullName,
      value: resident.id,
    })),
    initialValue?.resident?.id,
    initialValue?.resident?.name,
  )
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

  useEffect(() => {
    form.reset(getDefaultValues(initialValue))
  }, [form, initialValue])

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const result = occurrenceFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof OccurrenceFormValues, { message: issue.message })
            }
          }

          return
        }

        await onSubmit({
          assignedUserId: optionalText(result.data.assignedUserId),
          concurrencyToken:
            'concurrencyToken' in (initialValue ?? {})
              ? (initialValue as Partial<OccurrenceDetail>).concurrencyToken
              : undefined,
          contractId: optionalText(result.data.contractId),
          description: result.data.description,
          dueDate: optionalText(result.data.dueDate),
          priority: result.data.priority,
          propertyId: optionalText(result.data.propertyId),
          residentId: optionalText(result.data.residentId),
          title: result.data.title,
          type: result.data.type,
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          description: copy.form.description,
          priority: copy.form.priority,
          propertyId: copy.form.propertyId,
          title: copy.form.title,
          type: copy.form.type,
        }}
        title={copy.form.validationTitle}
      />

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.metadataSection}</legend>
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
          <FormField error={errorSummary.priority} label={copy.form.priority} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('priority')}
                id={id}
                aria-describedby={describedBy}
                disabled={mode === 'edit'}
                isInvalid={isInvalid}
                options={priorityOptions}
                required={isRequired}
              />
            )}
          </FormField>
          <FormField label={copy.form.dueDate}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('dueDate')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                type="date"
              />
            )}
          </FormField>
        </div>
        <FormField error={errorSummary.description} label={copy.form.description} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <TextAreaInput
              {...form.register('description')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              required={isRequired}
            />
          )}
        </FormField>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>
          {copy.form.identifiersSection}
        </legend>
        <div style={fieldGridStyle}>
          <FormField error={errorSummary.propertyId} label={copy.form.propertyId}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                {...form.register('propertyId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={propertyOptions}
                placeholder={copy.form.propertyId}
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
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.assigneeSection}</legend>
        <FormField label={copy.form.assignedUserId}>
          {({ describedBy, id, isInvalid }) => (
            <SelectInput
              {...form.register('assignedUserId')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              options={assigneeOptions}
              placeholder={copy.form.assignedUserId}
            />
          )}
        </FormField>
      </fieldset>

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
