import { useEffect } from 'react'
import { useForm, type FieldErrors } from 'react-hook-form'
import { z } from 'zod'
import {
  ActionButton,
  CheckboxInput,
  FormField,
  TextInput,
  ValidationSummary,
} from '../../components'
import type { AdministratorFormRequest } from '../../lib/api/administrators'
import type { ApiSelectOption } from '../../lib/api/contracts'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getAdministratorCopy } from './adminCopy'

export type AdministratorFormMode = 'create' | 'edit'

export type AdministratorFormProps = {
  initialValue?: Partial<AdministratorFormRequest>
  isSubmitting?: boolean
  mode: AdministratorFormMode
  onCancel?: () => void
  onSubmit: (values: AdministratorFormRequest) => Promise<void> | void
  roleOptions: ApiSelectOption[]
}

type AdministratorFormValues = {
  displayName: string
  email: string
  roleCodes: string[]
  temporaryPassword: string
}

const administratorFormSchema = z.object({
  displayName: z.string().trim().min(1, 'displayNameRequired'),
  email: z.string().trim().min(1, 'emailRequired').email('emailInvalid'),
  roleCodes: z.array(z.string()).min(1, 'roleRequired'),
  temporaryPassword: z.preprocess(
    (value) => (typeof value === 'string' && value.trim().length === 0 ? undefined : value),
    z.string().trim().min(8, 'temporaryPasswordMin').optional(),
  ),
})

function getMessage(key: string | undefined, copy: ReturnType<typeof getAdministratorCopy>) {
  if (key === 'displayNameRequired') {
    return copy.form.displayNameRequired
  }

  if (key === 'emailInvalid' || key === 'emailRequired') {
    return copy.form.emailInvalid
  }

  if (key === 'roleRequired') {
    return copy.form.roleRequired
  }

  if (key === 'temporaryPasswordMin') {
    return copy.form.temporaryPasswordMin
  }

  return copy.form.displayNameRequired
}

function buildErrorSummary(
  errors: FieldErrors<AdministratorFormValues>,
  copy: ReturnType<typeof getAdministratorCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

export function AdministratorForm({
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
  roleOptions,
}: AdministratorFormProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getAdministratorCopy(locale)
  const form = useForm<AdministratorFormValues>({
    defaultValues: {
      displayName: initialValue?.displayName ?? '',
      email: initialValue?.email ?? '',
      roleCodes: initialValue?.roleCodes ?? [],
      temporaryPassword: initialValue?.temporaryPassword ?? '',
    },
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)

  useEffect(() => {
    form.reset({
      displayName: initialValue?.displayName ?? '',
      email: initialValue?.email ?? '',
      roleCodes: initialValue?.roleCodes ?? [],
      temporaryPassword: initialValue?.temporaryPassword ?? '',
    })
  }, [form, initialValue])

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const result = administratorFormSchema.safeParse(values)

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (
              field === 'displayName' ||
              field === 'email' ||
              field === 'roleCodes' ||
              field === 'temporaryPassword'
            ) {
              form.setError(field, { message: issue.message })
            }
          }

          return
        }

        await onSubmit(result.data)
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          displayName: copy.form.displayName,
          email: copy.form.email,
          roleCodes: copy.form.roles,
          temporaryPassword: copy.form.temporaryPassword,
        }}
        title={copy.form.validationTitle}
      />

      <FormField error={errorSummary.displayName} label={copy.form.displayName} required>
        {({ describedBy, id, isInvalid, isRequired }) => (
          <TextInput
            {...form.register('displayName')}
            id={id}
            aria-describedby={describedBy}
            autoComplete="name"
            isInvalid={isInvalid}
            required={isRequired}
          />
        )}
      </FormField>

      <FormField error={errorSummary.email} label={copy.form.email} required>
        {({ describedBy, id, isInvalid, isRequired }) => (
          <TextInput
            {...form.register('email')}
            id={id}
            aria-describedby={describedBy}
            autoComplete="email"
            isInvalid={isInvalid}
            required={isRequired}
            type="email"
          />
        )}
      </FormField>

      <fieldset
        style={{
          border: '1px solid var(--als-color-border, #d7deea)',
          borderRadius: 'var(--als-radius-md, 8px)',
          display: 'grid',
          gap: 10,
          margin: 0,
          padding: 14,
        }}
      >
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.roles}</legend>
        {roleOptions.map((role) => (
          <CheckboxInput
            key={role.value}
            {...form.register('roleCodes')}
            disabled={role.disabled}
            label={role.label}
            value={role.value}
          />
        ))}
        {errorSummary.roleCodes ? (
          <span role="alert" style={{ color: 'var(--als-color-danger, #b42318)' }}>
            {errorSummary.roleCodes.join(' ')}
          </span>
        ) : null}
      </fieldset>

      <FormField
        description={copy.form.temporaryPasswordHint}
        error={errorSummary.temporaryPassword}
        label={copy.form.temporaryPassword}
      >
        {({ describedBy, id, isInvalid }) => (
          <TextInput
            {...form.register('temporaryPassword')}
            id={id}
            aria-describedby={describedBy}
            autoComplete="new-password"
            isInvalid={isInvalid}
            type="password"
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
