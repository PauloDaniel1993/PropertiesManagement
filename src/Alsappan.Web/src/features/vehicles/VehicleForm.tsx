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
import type { ApiSelectOption } from '../../lib/api/contracts'
import { listLeaseContracts } from '../../lib/api/leaseContracts'
import { listProperties } from '../../lib/api/properties'
import { listResidents } from '../../lib/api/residents'
import type {
  VehicleAuthorizationStatus,
  VehicleDetail,
  VehicleFormRequest,
  VehicleListItem,
  VehicleType,
} from '../../lib/api/vehicles'
import { vehicleAuthorizationStatuses, vehicleTypes } from '../../lib/api/vehicles'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getVehicleCopy } from './vehicleCopy'

export type VehicleFormMode = 'create' | 'edit'

export type VehicleFormProps = {
  authorizationStatusOptions: Array<ApiSelectOption<VehicleAuthorizationStatus>>
  canManageAuthorization?: boolean
  initialValue?: Partial<VehicleDetail | VehicleListItem>
  isSubmitting?: boolean
  mode: VehicleFormMode
  onCancel?: () => void
  onSubmit: (values: VehicleFormRequest) => Promise<void> | void
  typeOptions: Array<ApiSelectOption<VehicleType>>
}

type VehicleFormValues = {
  authorizationStatus: string
  brand: string
  color: string
  contractId: string
  model: string
  notes: string
  parkingAllocationNotes: string
  parkingSpaceIdentifier: string
  plate: string
  propertyId: string
  residentId: string
  type: string
  year: string
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

function parseOptionalYear(value: string) {
  const trimmedValue = value.trim()

  if (trimmedValue.length === 0) {
    return undefined
  }

  return Number(trimmedValue)
}

const vehicleFormSchema = z
  .object({
    authorizationStatus: z
      .string()
      .refine(
        (value): value is VehicleAuthorizationStatus =>
          vehicleAuthorizationStatuses.includes(value as VehicleAuthorizationStatus) &&
          value !== 'archived',
        'authorizationStatusRequired',
      ),
    brand: z.string().trim(),
    color: z.string().trim(),
    contractId: z.string().trim(),
    model: z.string().trim(),
    notes: z.string().trim(),
    parkingAllocationNotes: z.string().trim(),
    parkingSpaceIdentifier: z.string().trim(),
    plate: z.string().trim().min(1, 'plateRequired'),
    propertyId: z.string().trim(),
    residentId: z.string().trim(),
    type: z
      .string()
      .refine(
        (value): value is VehicleType => vehicleTypes.includes(value as VehicleType),
        'typeRequired',
      ),
    year: z
      .string()
      .trim()
      .transform(parseOptionalYear)
      .refine(
        (value) =>
          value === undefined ||
          (Number.isInteger(value) && Number.isFinite(value) && value >= 1886 && value <= 9999),
        'yearInvalid',
      ),
  })
  .refine((value) => value.residentId.length > 0 || value.contractId.length > 0, {
    message: 'ownerRequired',
    path: ['residentId'],
  })

function getMessage(key: string | undefined, copy: ReturnType<typeof getVehicleCopy>) {
  if (key === 'authorizationStatusRequired') {
    return copy.form.authorizationStatusRequired
  }

  if (key === 'ownerRequired') {
    return copy.form.ownerRequired
  }

  if (key === 'plateRequired') {
    return copy.form.plateRequired
  }

  if (key === 'typeRequired') {
    return copy.form.typeRequired
  }

  if (key === 'yearInvalid') {
    return copy.form.yearInvalid
  }

  return copy.form.plateRequired
}

function buildErrorSummary(
  errors: FieldErrors<VehicleFormValues>,
  copy: ReturnType<typeof getVehicleCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function getDefaultValues(
  initialValue?: Partial<VehicleDetail | VehicleListItem>,
): VehicleFormValues {
  return {
    authorizationStatus: initialValue?.authorizationStatus?.code ?? 'pending',
    brand: initialValue?.brand ?? '',
    color: initialValue?.color ?? '',
    contractId: initialValue?.contract?.id ?? '',
    model: initialValue?.model ?? '',
    notes:
      'notes' in (initialValue ?? {}) ? ((initialValue as Partial<VehicleDetail>).notes ?? '') : '',
    parkingAllocationNotes: initialValue?.parkingAllocationNotes ?? '',
    parkingSpaceIdentifier: initialValue?.parkingSpaceIdentifier ?? '',
    plate: initialValue?.plate ?? '',
    propertyId: initialValue?.property?.id ?? '',
    residentId: initialValue?.resident?.id ?? '',
    type: initialValue?.type?.code ?? 'car',
    year: typeof initialValue?.year === 'number' ? String(initialValue.year) : '',
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

export function VehicleForm({
  authorizationStatusOptions,
  canManageAuthorization = false,
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
  typeOptions,
}: VehicleFormProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getVehicleCopy(locale)
  const form = useForm<VehicleFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)
  const mutableStatusOptions = authorizationStatusOptions.filter(
    (option) => option.value !== 'archived',
  )
  const residentsQuery = useQuery({
    queryFn: () => listResidents(apiClient, { pageSize: 100 }),
    queryKey: ['residents', 'vehicle-picker'],
  })
  const propertiesQuery = useQuery({
    queryFn: () => listProperties(apiClient, { pageSize: 100 }),
    queryKey: ['properties', 'vehicle-picker'],
  })
  const contractsQuery = useQuery({
    queryFn: () => listLeaseContracts(apiClient, { includeArchived: false, locale, pageSize: 100 }),
    queryKey: ['contracts', 'vehicle-picker', locale],
  })
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
        const result = vehicleFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof VehicleFormValues, { message: issue.message })
            }
          }

          return
        }

        await onSubmit({
          authorizationStatus: result.data.authorizationStatus,
          brand: optionalText(result.data.brand),
          color: optionalText(result.data.color),
          concurrencyToken:
            'concurrencyToken' in (initialValue ?? {})
              ? (initialValue as Partial<VehicleDetail>).concurrencyToken
              : undefined,
          contractId: optionalText(result.data.contractId),
          model: optionalText(result.data.model),
          notes: optionalText(result.data.notes),
          parkingAllocationNotes: optionalText(result.data.parkingAllocationNotes),
          parkingSpaceIdentifier: optionalText(result.data.parkingSpaceIdentifier),
          plate: result.data.plate,
          propertyId: optionalText(result.data.propertyId),
          residentId: optionalText(result.data.residentId),
          type: result.data.type,
          year: result.data.year,
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          authorizationStatus: copy.form.authorizationStatus,
          plate: copy.form.plate,
          residentId: copy.form.residentId,
          type: copy.form.type,
          year: copy.form.year,
        }}
        title={copy.form.validationTitle}
      />

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.metadataSection}</legend>
        <div style={fieldGridStyle}>
          <FormField error={errorSummary.plate} label={copy.form.plate} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('plate')}
                id={id}
                aria-describedby={describedBy}
                autoCapitalize="characters"
                isInvalid={isInvalid}
                required={isRequired}
              />
            )}
          </FormField>
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
        </div>
        <div style={fieldGridStyle}>
          {canManageAuthorization ? (
            <FormField
              error={errorSummary.authorizationStatus}
              label={copy.form.authorizationStatus}
              required
            >
              {({ describedBy, id, isInvalid, isRequired }) => (
                <SelectInput
                  {...form.register('authorizationStatus')}
                  id={id}
                  aria-describedby={describedBy}
                  isInvalid={isInvalid}
                  options={mutableStatusOptions}
                  required={isRequired}
                />
              )}
            </FormField>
          ) : null}
          <FormField error={errorSummary.year} label={copy.form.year}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('year')}
                id={id}
                aria-describedby={describedBy}
                inputMode="numeric"
                isInvalid={isInvalid}
                max={9999}
                min={1886}
                type="number"
              />
            )}
          </FormField>
        </div>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.vehicleBrand}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('brand')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.form.vehicleModel}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('model')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.form.color}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('color')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
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
          <FormField error={errorSummary.residentId} label={copy.form.residentId}>
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
          <FormField label={copy.form.propertyId}>
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
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.parkingSection}</legend>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.parkingSpaceIdentifier}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('parkingSpaceIdentifier')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.form.parkingAllocationNotes}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('parkingAllocationNotes')}
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
