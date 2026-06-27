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
import {
  petAuthorizationStatuses,
  petSpecies,
  type PetAuthorizationStatus,
  type PetDetail,
  type PetFormRequest,
  type PetListItem,
  type PetSpecies,
} from '../../lib/api/pets'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getPetCopy } from './petCopy'

export type PetFormMode = 'create' | 'edit'

export type PetFormProps = {
  authorizationStatusOptions: Array<ApiSelectOption<PetAuthorizationStatus>>
  initialValue?: Partial<PetDetail | PetListItem>
  isSubmitting?: boolean
  mode: PetFormMode
  onCancel?: () => void
  onSubmit: (values: PetFormRequest) => Promise<void> | void
  speciesOptions: Array<ApiSelectOption<PetSpecies>>
}

type PetFormValues = {
  authorizationFormDocumentId: string
  authorizationNotes: string
  authorizationStatus: string
  breed: string
  contractId: string
  name: string
  notes: string
  propertyId: string
  residentId: string
  species: string
  vaccinationRecordDocumentId: string
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

const petFormSchema = z.object({
  authorizationFormDocumentId: z.string().trim(),
  authorizationNotes: z.string().trim(),
  authorizationStatus: z
    .string()
    .refine(
      (value): value is PetAuthorizationStatus =>
        petAuthorizationStatuses.includes(value as PetAuthorizationStatus) && value !== 'archived',
      'authorizationStatusRequired',
    ),
  breed: z.string().trim(),
  contractId: z.string().trim(),
  name: z.string().trim().min(1, 'nameRequired'),
  notes: z.string().trim(),
  propertyId: z.string().trim(),
  residentId: z.string().trim().min(1, 'residentRequired'),
  species: z
    .string()
    .refine(
      (value): value is PetSpecies => petSpecies.includes(value as PetSpecies),
      'speciesRequired',
    ),
  vaccinationRecordDocumentId: z.string().trim(),
})

function getMessage(key: string | undefined, copy: ReturnType<typeof getPetCopy>) {
  if (key === 'authorizationStatusRequired') {
    return copy.form.authorizationStatusRequired
  }

  if (key === 'nameRequired') {
    return copy.form.nameRequired
  }

  if (key === 'residentRequired') {
    return copy.form.residentRequired
  }

  if (key === 'speciesRequired') {
    return copy.form.speciesRequired
  }

  return copy.form.nameRequired
}

function buildErrorSummary(
  errors: FieldErrors<PetFormValues>,
  copy: ReturnType<typeof getPetCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function formatFirstDocumentId(documents: PetListItem['vaccinationRecordDocuments']) {
  return documents[0]?.documentId ?? ''
}

function getDefaultValues(initialValue?: Partial<PetDetail | PetListItem>): PetFormValues {
  return {
    authorizationFormDocumentId: formatFirstDocumentId(
      initialValue?.authorizationFormDocuments ?? [],
    ),
    authorizationNotes: initialValue?.authorizationNotes ?? '',
    authorizationStatus: initialValue?.authorizationStatus?.code ?? 'pending',
    breed: initialValue?.breed ?? '',
    contractId: initialValue?.contract?.id ?? '',
    name: initialValue?.name ?? '',
    notes:
      'notes' in (initialValue ?? {}) ? ((initialValue as Partial<PetDetail>).notes ?? '') : '',
    propertyId: initialValue?.property?.id ?? '',
    residentId: initialValue?.resident?.id ?? '',
    species: initialValue?.species?.code ?? 'dog',
    vaccinationRecordDocumentId: formatFirstDocumentId(
      initialValue?.vaccinationRecordDocuments ?? [],
    ),
  }
}

export function PetForm({
  authorizationStatusOptions,
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
  speciesOptions,
}: PetFormProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getPetCopy(locale)
  const form = useForm<PetFormValues>({
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
        const result = petFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof PetFormValues, { message: issue.message })
            }
          }

          return
        }

        await onSubmit({
          authorizationFormDocumentId: optionalText(result.data.authorizationFormDocumentId),
          authorizationNotes: optionalText(result.data.authorizationNotes),
          authorizationStatus: result.data.authorizationStatus,
          breed: optionalText(result.data.breed),
          concurrencyToken:
            'concurrencyToken' in (initialValue ?? {})
              ? (initialValue as Partial<PetDetail>).concurrencyToken
              : undefined,
          contractId: optionalText(result.data.contractId),
          name: result.data.name,
          notes: optionalText(result.data.notes),
          propertyId: optionalText(result.data.propertyId),
          residentId: result.data.residentId,
          species: result.data.species,
          vaccinationRecordDocumentId: optionalText(result.data.vaccinationRecordDocumentId),
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          authorizationStatus: copy.form.authorizationStatus,
          name: copy.form.name,
          residentId: copy.form.residentId,
          species: copy.form.species,
        }}
        title={copy.form.validationTitle}
      />

      <FormField error={errorSummary.name} label={copy.form.name} required>
        {({ describedBy, id, isInvalid, isRequired }) => (
          <TextInput
            {...form.register('name')}
            id={id}
            aria-describedby={describedBy}
            isInvalid={isInvalid}
            required={isRequired}
          />
        )}
      </FormField>

      <div style={fieldGridStyle}>
        <FormField error={errorSummary.species} label={copy.form.species} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <SelectInput
              {...form.register('species')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              options={speciesOptions}
              required={isRequired}
            />
          )}
        </FormField>
        <FormField label={copy.form.breed}>
          {({ describedBy, id, isInvalid }) => (
            <TextInput
              {...form.register('breed')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
            />
          )}
        </FormField>
      </div>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.contextSection}</legend>
        <div style={fieldGridStyle}>
          <FormField error={errorSummary.residentId} label={copy.form.residentId} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('residentId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                required={isRequired}
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
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>
          {copy.form.authorizationStatus}
        </legend>
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
              options={authorizationStatusOptions.filter((option) => option.value !== 'archived')}
              required={isRequired}
            />
          )}
        </FormField>
        <FormField label={copy.form.authorizationNotes}>
          {({ describedBy, id, isInvalid }) => (
            <TextAreaInput
              {...form.register('authorizationNotes')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
            />
          )}
        </FormField>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.documentsSection}</legend>
        <p style={{ color: 'var(--als-color-text-muted, #64748b)', margin: 0 }}>
          {copy.form.documentHint}
        </p>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.vaccinationRecordDocumentId}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('vaccinationRecordDocumentId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
              />
            )}
          </FormField>
          <FormField label={copy.form.authorizationFormDocumentId}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('authorizationFormDocumentId')}
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
