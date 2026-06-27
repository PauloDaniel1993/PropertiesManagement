import { useEffect } from 'react'
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
import {
  propertyStatuses,
  propertyTypes,
  type PropertyFormRequest,
  type PropertyStatus,
  type PropertyType,
} from '../../lib/api/properties'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getPropertyCopy } from './propertyCopy'

export type PropertyFormMode = 'create' | 'edit'

export type PropertyFormProps = {
  initialValue?: Partial<PropertyFormRequest>
  isSubmitting?: boolean
  mode: PropertyFormMode
  onCancel?: () => void
  onSubmit: (values: PropertyFormRequest) => Promise<void> | void
}

type PropertyFormValues = {
  city: string
  complement: string
  country: string
  garageSpaces: string
  hasGarage: boolean
  name: string
  neighborhood: string
  notes: string
  number: string
  postalCode: string
  state: string
  status: PropertyStatus
  street: string
  suggestedRent: string
  type: PropertyType
}

const defaultPropertyType: PropertyType = 'apartment'
const defaultPropertyStatus: PropertyStatus = 'available'

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

const propertyFormSchema = z
  .object({
    city: z.string().trim().min(1, 'cityRequired'),
    complement: z.string().trim(),
    country: z.string().trim(),
    garageSpaces: z
      .string()
      .trim()
      .transform(parseOptionalNumber)
      .refine(
        (value) => value === undefined || (Number.isInteger(value) && value >= 0),
        'garageSpacesRequired',
      ),
    hasGarage: z.boolean(),
    name: z.string().trim().min(1, 'nameRequired'),
    neighborhood: z.string().trim().min(1, 'neighborhoodRequired'),
    notes: z.string().trim(),
    number: z.string().trim(),
    postalCode: z.string().trim(),
    state: z.string().trim().min(1, 'stateRequired'),
    status: z
      .string()
      .refine(
        (value): value is PropertyStatus => propertyStatuses.includes(value as PropertyStatus),
        'statusRequired',
      ),
    street: z.string().trim().min(1, 'streetRequired'),
    suggestedRent: z
      .string()
      .trim()
      .transform(parseOptionalNumber)
      .refine(
        (value) => typeof value === 'number' && Number.isFinite(value) && value >= 0,
        'suggestedRentRequired',
      )
      .transform((value) => value as number),
    type: z
      .string()
      .refine((value): value is PropertyType => propertyTypes.includes(value as PropertyType))
      .transform((value) => value as PropertyType),
  })
  .superRefine((values, context) => {
    if (values.hasGarage && (!values.garageSpaces || values.garageSpaces < 1)) {
      context.addIssue({
        code: 'custom',
        message: 'garageSpacesRequired',
        path: ['garageSpaces'],
      })
    }
  })

function getMessage(key: string | undefined, copy: ReturnType<typeof getPropertyCopy>) {
  if (key === 'cityRequired') {
    return copy.form.cityRequired
  }

  if (key === 'garageSpacesRequired') {
    return copy.form.garageSpacesRequired
  }

  if (key === 'nameRequired') {
    return copy.form.nameRequired
  }

  if (key === 'neighborhoodRequired') {
    return copy.form.neighborhoodRequired
  }

  if (key === 'stateRequired') {
    return copy.form.stateRequired
  }

  if (key === 'statusRequired') {
    return copy.form.statusRequired
  }

  if (key === 'streetRequired') {
    return copy.form.streetRequired
  }

  if (key === 'suggestedRentRequired') {
    return copy.form.suggestedRentRequired
  }

  return copy.form.nameRequired
}

function buildErrorSummary(
  errors: FieldErrors<PropertyFormValues>,
  copy: ReturnType<typeof getPropertyCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function getDefaultValues(initialValue?: Partial<PropertyFormRequest>): PropertyFormValues {
  return {
    city: initialValue?.address?.city ?? '',
    complement: initialValue?.address?.complement ?? '',
    country: initialValue?.address?.country ?? 'BR',
    garageSpaces:
      typeof initialValue?.garageSpaces === 'number' ? String(initialValue.garageSpaces) : '',
    hasGarage: initialValue?.hasGarage ?? false,
    name: initialValue?.name ?? '',
    neighborhood: initialValue?.address?.neighborhood ?? '',
    notes: initialValue?.notes ?? '',
    number: initialValue?.address?.number ?? '',
    postalCode: initialValue?.address?.postalCode ?? '',
    state: initialValue?.address?.state ?? '',
    status: initialValue?.status ?? defaultPropertyStatus,
    street: initialValue?.address?.street ?? '',
    suggestedRent:
      typeof initialValue?.suggestedRent === 'number' ? String(initialValue.suggestedRent) : '',
    type: initialValue?.type ?? defaultPropertyType,
  }
}

export function PropertyForm({
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
}: PropertyFormProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getPropertyCopy(locale)
  const form = useForm<PropertyFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const hasGarage = form.watch('hasGarage')
  const errorSummary = buildErrorSummary(form.formState.errors, copy)
  const statusOptions = propertyStatuses.map((status) => ({
    label: copy.list.status[status],
    value: status,
  }))
  const typeOptions = propertyTypes.map((type) => ({
    label: copy.list.types[type],
    value: type,
  }))

  useEffect(() => {
    form.reset(getDefaultValues(initialValue))
  }, [form, initialValue])

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const result = propertyFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof PropertyFormValues, { message: issue.message })
            }
          }

          return
        }

        await onSubmit({
          address: {
            city: result.data.city,
            complement: optionalText(result.data.complement),
            country: optionalText(result.data.country),
            neighborhood: result.data.neighborhood,
            number: optionalText(result.data.number),
            postalCode: optionalText(result.data.postalCode),
            state: result.data.state,
            street: result.data.street,
          },
          garageSpaces: result.data.hasGarage ? result.data.garageSpaces : 0,
          hasGarage: result.data.hasGarage,
          name: result.data.name,
          notes: optionalText(result.data.notes),
          status: result.data.status,
          suggestedRent: result.data.suggestedRent,
          type: result.data.type,
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          city: copy.form.city,
          garageSpaces: copy.form.garageSpaces,
          name: copy.form.name,
          neighborhood: copy.form.neighborhood,
          state: copy.form.state,
          status: copy.form.status,
          street: copy.form.street,
          suggestedRent: copy.form.suggestedRent,
        }}
        title={copy.form.validationTitle}
      />

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.name}</legend>
        <FormField error={errorSummary.name} label={copy.form.name} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <TextInput
              {...form.register('name')}
              id={id}
              aria-describedby={describedBy}
              autoComplete="off"
              isInvalid={isInvalid}
              required={isRequired}
            />
          )}
        </FormField>

        <div style={fieldGridStyle}>
          <FormField label={copy.form.type} required>
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

          <FormField error={errorSummary.status} label={copy.form.status} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <SelectInput
                {...form.register('status')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={statusOptions}
                required={isRequired}
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.addressSection}</legend>
        <FormField error={errorSummary.street} label={copy.form.street} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <TextInput
              {...form.register('street')}
              id={id}
              aria-describedby={describedBy}
              autoComplete="street-address"
              isInvalid={isInvalid}
              required={isRequired}
            />
          )}
        </FormField>

        <div style={fieldGridStyle}>
          <FormField label={copy.form.number}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('number')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="off"
                isInvalid={isInvalid}
              />
            )}
          </FormField>

          <FormField label={copy.form.complement}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('complement')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="off"
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>

        <div style={fieldGridStyle}>
          <FormField error={errorSummary.neighborhood} label={copy.form.neighborhood} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('neighborhood')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="address-level3"
                isInvalid={isInvalid}
                required={isRequired}
              />
            )}
          </FormField>

          <FormField error={errorSummary.city} label={copy.form.city} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('city')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="address-level2"
                isInvalid={isInvalid}
                required={isRequired}
              />
            )}
          </FormField>
        </div>

        <div style={fieldGridStyle}>
          <FormField error={errorSummary.state} label={copy.form.state} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('state')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="address-level1"
                isInvalid={isInvalid}
                required={isRequired}
              />
            )}
          </FormField>

          <FormField label={copy.form.postalCode}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('postalCode')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="postal-code"
                isInvalid={isInvalid}
              />
            )}
          </FormField>

          <FormField label={copy.form.country}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('country')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="country"
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.rentSection}</legend>
        <FormField error={errorSummary.suggestedRent} label={copy.form.suggestedRent} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <TextInput
              {...form.register('suggestedRent')}
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
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.garageSection}</legend>
        <CheckboxInput {...form.register('hasGarage')} label={copy.form.hasGarage} />
        <FormField error={errorSummary.garageSpaces} label={copy.form.garageSpaces}>
          {({ describedBy, id, isInvalid }) => (
            <TextInput
              {...form.register('garageSpaces')}
              id={id}
              aria-describedby={describedBy}
              disabled={!hasGarage}
              inputMode="numeric"
              isInvalid={isInvalid}
              min={0}
              step={1}
              type="number"
            />
          )}
        </FormField>
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
