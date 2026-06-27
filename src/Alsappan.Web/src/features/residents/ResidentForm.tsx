import { useEffect, useMemo, useState } from 'react'
import { useForm, type FieldErrors } from 'react-hook-form'
import { SearchCheck } from 'lucide-react'
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
  residentPortalStatuses,
  residentPrivacyFlags,
  residentStatuses,
  type ResidentDuplicateWarning,
  type ResidentDuplicateWarningRequest,
  type ResidentFormRequest,
  type ResidentPortalStatus,
  type ResidentStatus,
} from '../../lib/api/residents'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getResidentCopy } from './residentCopy'

export type ResidentFormMode = 'create' | 'edit'

export type ResidentFormProps = {
  initialValue?: Partial<ResidentFormRequest>
  isSubmitting?: boolean
  mode: ResidentFormMode
  onCancel?: () => void
  onCheckDuplicates?: (
    request: ResidentDuplicateWarningRequest,
  ) => Promise<ResidentDuplicateWarning[]>
  onSubmit: (values: ResidentFormRequest) => Promise<void> | void
  residentId?: string
}

type ResidentFormValues = {
  birthDate: string
  documentIdentifier: string
  documentType: string
  email: string
  emergencyContactName: string
  emergencyContactPhone: string
  emergencyContactRelationship: string
  fullName: string
  notes: string
  phone: string
  portalStatus: ResidentPortalStatus
  preferredName: string
  privacyFlags: string[]
  secondaryPhone: string
  status: ResidentStatus
}

const defaultStatus: ResidentStatus = 'active'
const defaultPortalStatus: ResidentPortalStatus = 'not-invited'

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

function isResidentStatus(value: string): value is ResidentStatus {
  return residentStatuses.includes(value as ResidentStatus)
}

function isResidentPortalStatus(value: string): value is ResidentPortalStatus {
  return residentPortalStatuses.includes(value as ResidentPortalStatus)
}

function isOptionalEmail(value: string) {
  const trimmedValue = value.trim()

  return trimmedValue.length === 0 || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedValue)
}

const residentFormSchema = z
  .object({
    birthDate: z.string().trim(),
    documentIdentifier: z.string().trim(),
    documentType: z.string().trim(),
    email: z.string().trim().refine(isOptionalEmail, 'emailInvalid'),
    emergencyContactName: z.string().trim(),
    emergencyContactPhone: z.string().trim(),
    emergencyContactRelationship: z.string().trim(),
    fullName: z.string().trim().min(1, 'fullNameRequired'),
    notes: z.string().trim(),
    phone: z.string().trim(),
    portalStatus: z
      .string()
      .refine(isResidentPortalStatus, 'portalStatusRequired')
      .transform((value) => value as ResidentPortalStatus),
    preferredName: z.string().trim(),
    privacyFlags: z.array(z.string()).default([]),
    secondaryPhone: z.string().trim(),
    status: z
      .string()
      .refine(isResidentStatus, 'statusRequired')
      .transform((value) => value as ResidentStatus),
  })
  .superRefine((values, context) => {
    if (values.email.length === 0 && values.phone.length === 0) {
      context.addIssue({
        code: 'custom',
        message: 'contactRequired',
        path: ['phone'],
      })
    }
  })

function getMessage(key: string | undefined, copy: ReturnType<typeof getResidentCopy>) {
  if (key === 'contactRequired') {
    return copy.form.contactRequired
  }

  if (key === 'emailInvalid') {
    return copy.form.emailInvalid
  }

  if (key === 'fullNameRequired') {
    return copy.form.fullNameRequired
  }

  if (key === 'portalStatusRequired') {
    return copy.form.portalStatusRequired
  }

  if (key === 'statusRequired') {
    return copy.form.statusRequired
  }

  return copy.form.fullNameRequired
}

function buildErrorSummary(
  errors: FieldErrors<ResidentFormValues>,
  copy: ReturnType<typeof getResidentCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

function getDefaultValues(initialValue?: Partial<ResidentFormRequest>): ResidentFormValues {
  return {
    birthDate: initialValue?.birthDate ?? '',
    documentIdentifier: initialValue?.documentIdentifier ?? '',
    documentType: initialValue?.documentType ?? '',
    email: initialValue?.email ?? '',
    emergencyContactName: initialValue?.emergencyContact?.name ?? '',
    emergencyContactPhone: initialValue?.emergencyContact?.phone ?? '',
    emergencyContactRelationship: initialValue?.emergencyContact?.relationship ?? '',
    fullName: initialValue?.fullName ?? '',
    notes: initialValue?.notes ?? '',
    phone: initialValue?.phone ?? '',
    portalStatus: initialValue?.portalStatus ?? defaultPortalStatus,
    preferredName: initialValue?.preferredName ?? '',
    privacyFlags: initialValue?.privacyFlags ?? [],
    secondaryPhone: initialValue?.secondaryPhone ?? '',
    status: initialValue?.status ?? defaultStatus,
  }
}

function getDuplicateFieldLabel(field: string, copy: ReturnType<typeof getResidentCopy>) {
  if (field === 'email') {
    return copy.form.email
  }

  if (field === 'phone') {
    return copy.form.phone
  }

  if (field === 'documentIdentifier') {
    return copy.form.documentIdentifier
  }

  return field
}

export function ResidentForm({
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onCheckDuplicates,
  onSubmit,
  residentId,
}: ResidentFormProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getResidentCopy(locale)
  const form = useForm<ResidentFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const [duplicateWarnings, setDuplicateWarnings] = useState<ResidentDuplicateWarning[]>([])
  const [duplicateState, setDuplicateState] = useState<'error' | 'idle' | 'success'>('idle')
  const [isCheckingDuplicates, setIsCheckingDuplicates] = useState(false)
  const watchedEmail = form.watch('email')
  const watchedPhone = form.watch('phone')
  const watchedDocumentIdentifier = form.watch('documentIdentifier')
  const hasDuplicateCriteria = Boolean(
    optionalText(watchedEmail ?? '') ||
    optionalText(watchedPhone ?? '') ||
    optionalText(watchedDocumentIdentifier ?? ''),
  )
  const errorSummary = buildErrorSummary(form.formState.errors, copy)
  const statusOptions = useMemo(
    () =>
      residentStatuses.map((status) => ({
        label: copy.list.status[status],
        value: status,
      })),
    [copy],
  )
  const portalStatusOptions = useMemo(
    () =>
      residentPortalStatuses.map((portalStatus) => ({
        label: copy.list.portalStatus[portalStatus],
        value: portalStatus,
      })),
    [copy],
  )

  useEffect(() => {
    form.reset(getDefaultValues(initialValue))
    setDuplicateWarnings([])
    setDuplicateState('idle')
  }, [form, initialValue])

  useEffect(() => {
    setDuplicateWarnings([])
    setDuplicateState('idle')
  }, [watchedDocumentIdentifier, watchedEmail, watchedPhone])

  async function handleDuplicateCheck() {
    if (!onCheckDuplicates || !hasDuplicateCriteria) {
      setDuplicateState('success')
      setDuplicateWarnings([])
      return
    }

    const values = form.getValues()
    setIsCheckingDuplicates(true)
    setDuplicateState('idle')

    try {
      const warnings = await onCheckDuplicates({
        documentIdentifier: optionalText(values.documentIdentifier),
        email: optionalText(values.email),
        ignoreResidentId: residentId,
        phone: optionalText(values.phone),
      })
      setDuplicateWarnings(warnings)
      setDuplicateState('success')
    } catch {
      setDuplicateWarnings([])
      setDuplicateState('error')
    } finally {
      setIsCheckingDuplicates(false)
    }
  }

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const result = residentFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof ResidentFormValues, { message: issue.message })
            }
          }

          return
        }

        await onSubmit({
          birthDate: optionalText(result.data.birthDate),
          documentIdentifier: optionalText(result.data.documentIdentifier),
          documentType: optionalText(result.data.documentType),
          email: optionalText(result.data.email),
          emergencyContact: {
            name: optionalText(result.data.emergencyContactName),
            phone: optionalText(result.data.emergencyContactPhone),
            relationship: optionalText(result.data.emergencyContactRelationship),
          },
          fullName: result.data.fullName,
          notes: optionalText(result.data.notes),
          phone: optionalText(result.data.phone),
          portalStatus: result.data.portalStatus,
          preferredName: optionalText(result.data.preferredName),
          privacyFlags: result.data.privacyFlags,
          secondaryPhone: optionalText(result.data.secondaryPhone),
          status: result.data.status,
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          email: copy.form.email,
          fullName: copy.form.fullName,
          phone: copy.form.phone,
          portalStatus: copy.form.portalStatus,
          status: copy.form.status,
        }}
        title={copy.form.validationTitle}
      />

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.personalSection}</legend>
        <FormField error={errorSummary.fullName} label={copy.form.fullName} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <TextInput
              {...form.register('fullName')}
              id={id}
              aria-describedby={describedBy}
              autoComplete="name"
              isInvalid={isInvalid}
              required={isRequired}
            />
          )}
        </FormField>

        <div style={fieldGridStyle}>
          <FormField label={copy.form.preferredName}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('preferredName')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="given-name"
                isInvalid={isInvalid}
              />
            )}
          </FormField>

          <FormField label={copy.form.birthDate}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('birthDate')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                type="date"
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
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.contactSection}</legend>
        <div style={fieldGridStyle}>
          <FormField error={errorSummary.email} label={copy.form.email}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('email')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="email"
                isInvalid={isInvalid}
                type="email"
              />
            )}
          </FormField>

          <FormField error={errorSummary.phone} label={copy.form.phone}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('phone')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="tel"
                isInvalid={isInvalid}
              />
            )}
          </FormField>

          <FormField label={copy.form.secondaryPhone}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('secondaryPhone')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="tel"
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>
          {copy.form.identificationSection}
        </legend>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.documentType}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('documentType')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="off"
                isInvalid={isInvalid}
              />
            )}
          </FormField>

          <FormField label={copy.form.documentIdentifier}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('documentIdentifier')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="off"
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.duplicateSection}</legend>
        <ActionButton
          disabled={!hasDuplicateCriteria || !onCheckDuplicates}
          icon={<SearchCheck aria-hidden="true" size={16} />}
          isLoading={isCheckingDuplicates}
          loadingLabel={copy.form.checkDuplicates}
          onClick={() => void handleDuplicateCheck()}
          size="sm"
          style={{ justifySelf: 'start' }}
          type="button"
        >
          {copy.form.checkDuplicates}
        </ActionButton>

        <div aria-live="polite" style={{ display: 'grid', gap: 8 }}>
          {!hasDuplicateCriteria ? (
            <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
              {copy.form.duplicateNoCriteria}
            </span>
          ) : null}

          {duplicateState === 'error' ? (
            <span role="alert" style={{ color: 'var(--als-color-danger, #b42318)' }}>
              {copy.form.duplicateError}
            </span>
          ) : null}

          {duplicateState === 'success' && duplicateWarnings.length === 0 ? (
            <span style={{ color: 'var(--als-color-success, #027a48)' }}>
              {copy.form.duplicateNone}
            </span>
          ) : null}

          {duplicateWarnings.length > 0 ? (
            <section
              aria-label={copy.form.duplicateWarningTitle}
              style={{
                background: 'var(--als-color-warning-soft, #fffaeb)',
                border: '1px solid var(--als-color-warning-border, #fedf89)',
                borderRadius: 'var(--als-radius-md, 8px)',
                color: 'var(--als-color-warning, #b54708)',
                display: 'grid',
                gap: 8,
                padding: 12,
              }}
            >
              <strong>{copy.form.duplicateWarningTitle}</strong>
              <ul style={{ margin: 0, paddingInlineStart: 20 }}>
                {duplicateWarnings.map((warning) => (
                  <li key={`${warning.field}-${warning.residentId}`}>
                    <strong>{getDuplicateFieldLabel(warning.field, copy)}:</strong>{' '}
                    {warning.message || `${warning.value} - ${warning.residentName}`}
                  </li>
                ))}
              </ul>
            </section>
          ) : null}
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.emergencySection}</legend>
        <div style={fieldGridStyle}>
          <FormField label={copy.form.emergencyContactName}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('emergencyContactName')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="name"
                isInvalid={isInvalid}
              />
            )}
          </FormField>

          <FormField label={copy.form.emergencyContactRelationship}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('emergencyContactRelationship')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="off"
                isInvalid={isInvalid}
              />
            )}
          </FormField>

          <FormField label={copy.form.emergencyContactPhone}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                {...form.register('emergencyContactPhone')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="tel"
                isInvalid={isInvalid}
              />
            )}
          </FormField>
        </div>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.portalSection}</legend>
        <FormField error={errorSummary.portalStatus} label={copy.form.portalStatus} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <SelectInput
              {...form.register('portalStatus')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              options={portalStatusOptions}
              required={isRequired}
            />
          )}
        </FormField>
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.privacySection}</legend>
        <div style={{ display: 'grid', gap: 10 }}>
          {residentPrivacyFlags.map((flag) => (
            <CheckboxInput
              key={flag}
              {...form.register('privacyFlags')}
              label={copy.form.privacyFlags[flag]}
              value={flag}
            />
          ))}
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
