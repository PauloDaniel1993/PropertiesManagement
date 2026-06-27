import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Building2, KeyRound } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Navigate, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import {
  ActionButton,
  FormField,
  SelectInput,
  TextInput,
  ValidationSummary,
} from '../../components'
import { ApiClientError } from '../../lib/api/client'
import {
  loginAdmin,
  loginResident,
  type AccountType,
  type AuthSessionDto,
} from '../../lib/api/identity'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { getIdentityCopy } from './identityCopy'
import { applyAuthSession } from './session'

type LoginFormValues = {
  email: string
  organizationId: string
  password: string
}

export type IdentityLoginPageProps = {
  accountType?: AccountType
  defaultRedirectPath?: string
  onAuthenticated?: (session: AuthSessionDto) => void
}

const loginSchema = z.object({
  email: z.string().trim().min(1, 'required').email('email'),
  organizationId: z.string().trim().optional(),
  password: z.string().min(1, 'passwordRequired'),
})

function buildValidationSummary(
  errors: Record<string, { message?: string }>,
  copy: ReturnType<typeof getIdentityCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    const messageKey = error.message
    const message =
      messageKey === 'email'
        ? copy.login.emailInvalid
        : messageKey === 'passwordRequired'
          ? copy.login.passwordRequired
          : copy.login.required

    summary[field] = [message]

    return summary
  }, {})
}

export function IdentityLoginPage({
  accountType = 'admin',
  defaultRedirectPath,
  onAuthenticated,
}: IdentityLoginPageProps) {
  const apiClient = useApiClient()
  const navigate = useNavigate()
  const locale = useAppPreferencesStore((state) => state.locale)
  const organizations = useActiveOrganizationStore((state) => state.organizations)
  const intendedPath = useAuthSessionStore((state) => state.intendedPath)
  const isAuthenticated = useAuthSessionStore((state) => state.isAuthenticated)
  const clearIntendedPath = useAuthSessionStore((state) => state.clearIntendedPath)
  const [formError, setFormError] = useState<string | null>(null)
  const copy = getIdentityCopy(locale)
  const redirectPath =
    defaultRedirectPath ?? (accountType === 'resident' ? '/portal' : '/dashboard')
  const form = useForm<LoginFormValues>({
    defaultValues: {
      email: '',
      organizationId: '',
      password: '',
    },
  })
  const validationSummary = buildValidationSummary(form.formState.errors, copy)
  const loginMutation = useMutation({
    mutationFn: (values: LoginFormValues) => {
      const request = {
        email: values.email.trim(),
        organizationId: values.organizationId || undefined,
        password: values.password,
      }

      return accountType === 'resident'
        ? loginResident(apiClient, request)
        : loginAdmin(apiClient, request)
    },
    onError: (error) => {
      setFormError(
        error instanceof ApiClientError
          ? (error.problem?.title ?? copy.login.genericError)
          : copy.login.genericError,
      )
    },
    onSuccess: (session) => {
      applyAuthSession(session)
      onAuthenticated?.(session)
      const destination = intendedPath ?? redirectPath
      clearIntendedPath()
      navigate(destination, { replace: true })
    },
  })

  if (isAuthenticated) {
    return <Navigate replace to={intendedPath ?? redirectPath} />
  }

  return (
    <main
      aria-labelledby="identity-login-title"
      className="identity-login-page"
      style={{
        alignItems: 'center',
        background: 'var(--als-color-background, #f4f7fb)',
        color: 'var(--als-color-text, #1f2937)',
        display: 'grid',
        minHeight: '100vh',
        padding: 24,
      }}
    >
      <section
        style={{
          background: 'var(--als-color-surface, #ffffff)',
          border: '1px solid var(--als-color-border, #d7deea)',
          borderRadius: 'var(--als-radius-md, 8px)',
          boxShadow: '0 24px 70px rgba(15, 23, 42, 0.14)',
          display: 'grid',
          gap: 22,
          justifySelf: 'center',
          maxWidth: 440,
          padding: 28,
          width: '100%',
        }}
      >
        <div style={{ alignItems: 'center', display: 'flex', gap: 12 }}>
          <span
            aria-hidden="true"
            style={{
              alignItems: 'center',
              background: 'var(--als-color-primary-soft, #e8f2ff)',
              borderRadius: 'var(--als-radius-md, 8px)',
              color: 'var(--als-color-primary, #1877f2)',
              display: 'inline-flex',
              height: 48,
              justifyContent: 'center',
              width: 48,
            }}
          >
            <Building2 size={24} />
          </span>
          <div style={{ display: 'grid', gap: 2 }}>
            <strong>Alsappan</strong>
            <span style={{ color: 'var(--als-color-text-muted, #64748b)', fontSize: '0.9rem' }}>
              {copy.login.accountType[accountType]}
            </span>
          </div>
        </div>

        <div style={{ display: 'grid', gap: 6 }}>
          <h1 id="identity-login-title" style={{ fontSize: '2rem', lineHeight: 1.1, margin: 0 }}>
            {copy.login.title}
          </h1>
          <p style={{ color: 'var(--als-color-text-muted, #64748b)', lineHeight: 1.55, margin: 0 }}>
            {copy.login.subtitle}
          </p>
        </div>

        {formError ? (
          <div
            role="alert"
            style={{
              background: 'var(--als-color-danger-soft, #fef3f2)',
              border: '1px solid var(--als-color-danger-border, #fecdca)',
              borderRadius: 'var(--als-radius-md, 8px)',
              color: 'var(--als-color-danger, #b42318)',
              padding: 12,
            }}
          >
            {formError}
          </div>
        ) : null}

        <ValidationSummary
          errors={validationSummary}
          fieldLabels={{
            email: copy.login.email,
            password: copy.login.password,
          }}
        />

        <form
          noValidate
          onSubmit={form.handleSubmit((values) => {
            setFormError(null)
            const result = loginSchema.safeParse(values)

            if (!result.success) {
              for (const issue of result.error.issues) {
                const field = issue.path[0]

                if (field === 'email' || field === 'password' || field === 'organizationId') {
                  form.setError(field, { message: issue.message })
                }
              }

              return
            }

            loginMutation.mutate(result.data as LoginFormValues)
          })}
          style={{ display: 'grid', gap: 16 }}
        >
          <FormField error={validationSummary.email} label={copy.login.email} required>
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

          <FormField label={copy.login.organization}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                {...form.register('organizationId')}
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                options={organizations.map((organization) => ({
                  label: organization.displayName,
                  value: organization.id,
                }))}
                placeholder={copy.login.organizationPlaceholder}
              />
            )}
          </FormField>

          <FormField error={validationSummary.password} label={copy.login.password} required>
            {({ describedBy, id, isInvalid, isRequired }) => (
              <TextInput
                {...form.register('password')}
                id={id}
                aria-describedby={describedBy}
                autoComplete="current-password"
                isInvalid={isInvalid}
                required={isRequired}
                type="password"
              />
            )}
          </FormField>

          <ActionButton
            icon={<KeyRound aria-hidden="true" size={18} />}
            isLoading={loginMutation.isPending}
            loadingLabel={copy.login.submit}
            tone="primary"
            type="submit"
          >
            {copy.login.submit}
          </ActionButton>
        </form>
      </section>
    </main>
  )
}
