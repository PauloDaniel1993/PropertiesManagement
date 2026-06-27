import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Building2 } from 'lucide-react'
import { SelectInput } from '../../../components'
import { ApiClientError } from '../../../lib/api/client'
import { switchActiveOrganization } from '../../../lib/api/identity'
import { useApiClient } from '../../../lib/api/ApiClientContext'
import { useActiveOrganizationStore } from '../../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../../stores/useAppPreferencesStore'
import { getIdentityCopy } from '../identityCopy'
import { applyAuthSession } from '../session'

export type OrganizationSwitcherProps = {
  className?: string
}

export function OrganizationSwitcher({ className }: OrganizationSwitcherProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const activeOrganization = useActiveOrganizationStore((state) => state.activeOrganization)
  const organizations = useActiveOrganizationStore((state) => state.organizations)
  const [error, setError] = useState<string | null>(null)
  const copy = getIdentityCopy(locale)
  const switchMutation = useMutation({
    mutationFn: (organizationId: string) => switchActiveOrganization(apiClient, { organizationId }),
    onError: (mutationError) => {
      setError(
        mutationError instanceof ApiClientError
          ? (mutationError.problem?.title ?? copy.organizationSwitcher.error)
          : copy.organizationSwitcher.error,
      )
    },
    onSuccess: (session) => {
      setError(null)
      applyAuthSession(session)
    },
  })

  return (
    <div className={className} style={{ display: 'grid', gap: 6, minWidth: 220 }}>
      <label
        htmlFor="identity-organization-switcher"
        style={{ alignItems: 'center', display: 'inline-flex', fontWeight: 800, gap: 8 }}
      >
        <Building2 aria-hidden="true" size={16} />
        {copy.organizationSwitcher.label}
      </label>
      <SelectInput
        id="identity-organization-switcher"
        aria-describedby={error ? 'identity-organization-switcher-error' : undefined}
        disabled={organizations.length <= 1 || switchMutation.isPending}
        onChange={(event) => switchMutation.mutate(event.currentTarget.value)}
        options={organizations.map((organization) => ({
          label: organization.displayName,
          value: organization.id,
        }))}
        title={organizations.length <= 1 ? copy.organizationSwitcher.singleOrganization : undefined}
        value={activeOrganization?.id ?? ''}
      />
      {switchMutation.isPending ? (
        <span
          role="status"
          style={{ color: 'var(--als-color-text-muted, #64748b)', fontSize: '0.86rem' }}
        >
          {copy.organizationSwitcher.switching}
        </span>
      ) : null}
      {error ? (
        <span
          id="identity-organization-switcher-error"
          role="alert"
          style={{ color: 'var(--als-color-danger, #b42318)', fontSize: '0.86rem' }}
        >
          {error}
        </span>
      ) : null}
    </div>
  )
}
