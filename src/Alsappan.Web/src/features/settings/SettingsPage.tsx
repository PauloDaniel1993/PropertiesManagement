import { useEffect, useMemo, useState, type ChangeEvent, type CSSProperties } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Bell,
  Building2,
  KeyRound,
  Languages,
  RefreshCw,
  Tags,
  Upload,
  UserCog,
  Users,
} from 'lucide-react'
import {
  ActionButton,
  CheckboxInput,
  DataTable,
  DetailSection,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  SelectInput,
  StatusBadge,
  Tabs,
  TextInput,
  ValidationSummary,
} from '../../components'
import { ApiClientError } from '../../lib/api/client'
import { useApiClient } from '../../lib/api/ApiClientContext'
import {
  getSettingsDashboard,
  removeBrandLogo,
  resetBranding,
  updateBranding,
  updateDomainCatalog,
  updateLocalization,
  updateNotifications,
  updateOrganizationProfile,
  updateResidentPortal,
  updateSecurity,
  updateTenantBehavior,
  updateUserLocalePreference,
  uploadBrandLogo,
  type DomainCatalogItem,
  type DomainCatalogSettings,
  type LocalizationSettings,
  type NotificationSettings,
  type OrganizationBrandingSettings,
  type OrganizationBrandingUpdateRequest,
  type OrganizationProfileSettings,
  type PasswordRuleSettings,
  type ResidentPortalSettings,
  type SecuritySettings,
  type TenantBehaviorSettings,
} from '../../lib/api/settings'
import { hasEveryPermission } from '../identity/session'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { getSettingsCopy } from './settingsCopy'

const sectionGridStyle: CSSProperties = {
  display: 'grid',
  gap: 16,
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
}

const actionRowStyle: CSSProperties = {
  alignItems: 'center',
  display: 'flex',
  flexWrap: 'wrap',
  gap: 10,
  justifyContent: 'flex-end',
}

const compactInputStyle: CSSProperties = {
  minWidth: 0,
}

function toFieldErrors(error: unknown) {
  if (error instanceof ApiClientError) {
    return error.validationErrors
  }

  return {}
}

function optionalString(value: string | undefined) {
  return value?.trim() ? value.trim() : undefined
}

function toOrganizationDraft(settings: OrganizationProfileSettings) {
  return {
    concurrencyToken: settings.concurrencyToken,
    contactEmail: settings.contactEmail ?? '',
    contactPhone: settings.contactPhone ?? '',
    contactWebsite: settings.contactWebsite ?? '',
    currencyCode: settings.currencyCode,
    displayName: settings.displayName,
    name: settings.name,
    slug: settings.slug,
    timeZone: settings.timeZone,
  }
}

function toBrandingDraft(settings: OrganizationBrandingSettings) {
  return {
    accentColor: settings.accentColor ?? '',
    accentForegroundColor: settings.accentForegroundColor ?? '',
    concurrencyToken: settings.concurrencyToken,
    displayName: settings.displayName ?? '',
    logoAlt: settings.logoAlt ?? '',
    logoUrl: settings.logoUrl ?? '',
    primaryColor: settings.primaryColor ?? '',
    primaryForegroundColor: settings.primaryForegroundColor ?? '',
    supportEmail: settings.supportEmail ?? '',
    supportPhone: settings.supportPhone ?? '',
    supportUrl: settings.supportUrl ?? '',
  }
}

function toLocalizationDraft(settings: LocalizationSettings) {
  return {
    concurrencyToken: settings.concurrencyToken,
    defaultLocale: settings.defaultLocale,
    enabledLocales: [...settings.enabledLocales],
    fallbackLocale: settings.fallbackLocale,
    supportedLocales: settings.supportedLocales,
  }
}

function toCatalogDraft(catalog?: DomainCatalogSettings) {
  return (
    catalog?.items.map((item) => ({
      ...item,
      labels: { ...item.labels },
    })) ?? []
  )
}

function updateCatalogItem(
  items: DomainCatalogItem[],
  code: string,
  mutate: (item: DomainCatalogItem) => DomainCatalogItem,
) {
  return items.map((item) => (item.code === code ? mutate(item) : item))
}

function getOptionLabel(code: string) {
  return code
    .split('-')
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')
}

function getImageDimensions(file: File) {
  return new Promise<{ height: number; width: number }>((resolve, reject) => {
    const image = new Image()
    const url = URL.createObjectURL(file)

    image.onload = () => {
      URL.revokeObjectURL(url)
      resolve({
        height: image.naturalHeight || 1,
        width: image.naturalWidth || 1,
      })
    }
    image.onerror = () => {
      URL.revokeObjectURL(url)
      reject(new Error('Could not read image dimensions.'))
    }
    image.src = url
  })
}

function readFileBase64(file: File) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader()

    reader.onload = () => {
      const result = String(reader.result ?? '')
      resolve(result.includes(',') ? result.split(',').at(-1)! : result)
    }
    reader.onerror = () => reject(reader.error ?? new Error('Could not read file.'))
    reader.readAsDataURL(file)
  })
}

function ColorField({
  label,
  onChange,
  value,
}: {
  label: string
  onChange: (value: string) => void
  value: string
}) {
  const normalizedValue = value || '#1877f2'

  return (
    <FormField label={label}>
      {({ id }) => (
        <div
          style={{ alignItems: 'center', display: 'grid', gap: 8, gridTemplateColumns: '48px 1fr' }}
        >
          <input
            aria-label={label}
            id={id}
            onChange={(event) => onChange(event.currentTarget.value)}
            style={{
              border: '1px solid var(--als-color-border, #d7deea)',
              borderRadius: 'var(--als-radius-sm, 6px)',
              height: 44,
              padding: 2,
              width: 48,
            }}
            type="color"
            value={normalizedValue}
          />
          <TextInput
            aria-label={`${label} hex`}
            onChange={(event) => onChange(event.currentTarget.value)}
            value={value}
          />
        </div>
      )}
    </FormField>
  )
}

export function SettingsPage() {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const activeOrganizationId = useActiveOrganizationStore((state) => state.activeOrganizationId)
  const authUser = useAuthSessionStore((state) => state.user)
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getSettingsCopy(locale)
  const canWriteSettings = hasEveryPermission(['settings.write'], authUser)
  const canManageSettings = hasEveryPermission(['settings.manage'], authUser)
  const [selectedTab, setSelectedTab] = useState('organization')
  const [selectedCatalogType, setSelectedCatalogType] = useState('')
  const settingsQuery = useQuery({
    queryFn: () => getSettingsDashboard(apiClient, locale),
    queryKey: ['settings', activeOrganizationId, locale],
  })
  const dashboard = settingsQuery.data
  const [organizationDraft, setOrganizationDraft] = useState<
    ReturnType<typeof toOrganizationDraft> | undefined
  >()
  const [brandingDraft, setBrandingDraft] = useState<
    ReturnType<typeof toBrandingDraft> | undefined
  >()
  const [tenantDraft, setTenantDraft] = useState<TenantBehaviorSettings | undefined>()
  const [residentPortalDraft, setResidentPortalDraft] = useState<
    ResidentPortalSettings | undefined
  >()
  const [localizationDraft, setLocalizationDraft] = useState<
    ReturnType<typeof toLocalizationDraft> | undefined
  >()
  const [profileLocaleDraft, setProfileLocaleDraft] = useState('')
  const [securityDraft, setSecurityDraft] = useState<SecuritySettings | undefined>()
  const [notificationDraft, setNotificationDraft] = useState<NotificationSettings | undefined>()
  const [catalogDraft, setCatalogDraft] = useState<DomainCatalogItem[]>([])

  useEffect(() => {
    if (!dashboard) {
      return
    }

    setOrganizationDraft(toOrganizationDraft(dashboard.organization))
    setBrandingDraft(toBrandingDraft(dashboard.branding))
    setTenantDraft({ ...dashboard.tenantBehavior })
    setResidentPortalDraft({ ...dashboard.residentPortal })
    setLocalizationDraft(toLocalizationDraft(dashboard.localization))
    setProfileLocaleDraft(
      dashboard.profilePreferences.locale || dashboard.profilePreferences.effectiveLocale,
    )
    setSecurityDraft({
      ...dashboard.security,
      passwordRules: { ...dashboard.security.passwordRules },
    })
    setNotificationDraft({
      ...dashboard.notifications,
      availableCategories: [...dashboard.notifications.availableCategories],
      availableChannels: [...dashboard.notifications.availableChannels],
      enabledCategories: [...dashboard.notifications.enabledCategories],
      enabledChannels: [...dashboard.notifications.enabledChannels],
    })
    setSelectedCatalogType((current) => current || dashboard.catalogs[0]?.catalogType || '')
  }, [dashboard])

  const selectedCatalog = useMemo(
    () => dashboard?.catalogs.find((catalog) => catalog.catalogType === selectedCatalogType),
    [dashboard?.catalogs, selectedCatalogType],
  )

  useEffect(() => {
    setCatalogDraft(toCatalogDraft(selectedCatalog))
  }, [selectedCatalog])

  async function invalidateSettings() {
    await queryClient.invalidateQueries({ queryKey: ['settings'] })
  }

  const organizationMutation = useMutation({
    mutationFn: () => {
      if (!organizationDraft) {
        throw new Error('Missing organization draft.')
      }

      return updateOrganizationProfile(
        apiClient,
        {
          ...organizationDraft,
          contactEmail: optionalString(organizationDraft.contactEmail),
          contactPhone: optionalString(organizationDraft.contactPhone),
          contactWebsite: optionalString(organizationDraft.contactWebsite),
        },
        locale,
      )
    },
    onSuccess: invalidateSettings,
  })
  const brandingMutation = useMutation({
    mutationFn: () => {
      if (!brandingDraft) {
        throw new Error('Missing branding draft.')
      }

      const request: OrganizationBrandingUpdateRequest = {
        accentColor: optionalString(brandingDraft.accentColor),
        accentForegroundColor: optionalString(brandingDraft.accentForegroundColor),
        concurrencyToken: brandingDraft.concurrencyToken,
        displayName: optionalString(brandingDraft.displayName),
        logoAlt: optionalString(brandingDraft.logoAlt),
        logoUrl: optionalString(brandingDraft.logoUrl),
        primaryColor: optionalString(brandingDraft.primaryColor),
        primaryForegroundColor: optionalString(brandingDraft.primaryForegroundColor),
        supportEmail: optionalString(brandingDraft.supportEmail),
        supportPhone: optionalString(brandingDraft.supportPhone),
        supportUrl: optionalString(brandingDraft.supportUrl),
      }

      return updateBranding(apiClient, request, locale)
    },
    onSuccess: invalidateSettings,
  })
  const logoUploadMutation = useMutation({
    mutationFn: async (file: File) => {
      if (!brandingDraft) {
        throw new Error('Missing branding draft.')
      }

      const [contentBase64, dimensions] = await Promise.all([
        readFileBase64(file),
        getImageDimensions(file),
      ])

      return uploadBrandLogo(
        apiClient,
        {
          concurrencyToken: brandingDraft.concurrencyToken,
          contentBase64,
          contentType: file.type,
          fileName: file.name,
          height: dimensions.height,
          logoAlt: brandingDraft.logoAlt || file.name,
          sizeBytes: file.size,
          width: dimensions.width,
        },
        locale,
      )
    },
    onSuccess: invalidateSettings,
  })
  const logoRemoveMutation = useMutation({
    mutationFn: () => removeBrandLogo(apiClient, locale),
    onSuccess: invalidateSettings,
  })
  const resetBrandingMutation = useMutation({
    mutationFn: () => resetBranding(apiClient, locale),
    onSuccess: invalidateSettings,
  })
  const tenantMutation = useMutation({
    mutationFn: () => updateTenantBehavior(apiClient, tenantDraft!, locale),
    onSuccess: invalidateSettings,
  })
  const residentPortalMutation = useMutation({
    mutationFn: () => updateResidentPortal(apiClient, residentPortalDraft!, locale),
    onSuccess: invalidateSettings,
  })
  const localizationMutation = useMutation({
    mutationFn: () => {
      if (!localizationDraft) {
        throw new Error('Missing localization draft.')
      }

      return updateLocalization(
        apiClient,
        {
          concurrencyToken: localizationDraft.concurrencyToken,
          defaultLocale: localizationDraft.defaultLocale,
          enabledLocales: localizationDraft.enabledLocales,
          fallbackLocale: localizationDraft.fallbackLocale,
        },
        locale,
      )
    },
    onSuccess: invalidateSettings,
  })
  const profileMutation = useMutation({
    mutationFn: () => updateUserLocalePreference(apiClient, profileLocaleDraft, locale),
    onSuccess: invalidateSettings,
  })
  const securityMutation = useMutation({
    mutationFn: () => {
      if (!securityDraft) {
        throw new Error('Missing security draft.')
      }

      return updateSecurity(
        apiClient,
        {
          concurrencyToken: securityDraft.concurrencyToken,
          mfaPolicy: securityDraft.mfaPolicy,
          passwordRules: securityDraft.passwordRules,
          sessionTimeoutMinutes: securityDraft.sessionTimeoutMinutes,
        },
        locale,
      )
    },
    onSuccess: invalidateSettings,
  })
  const notificationsMutation = useMutation({
    mutationFn: () => {
      if (!notificationDraft) {
        throw new Error('Missing notification draft.')
      }

      return updateNotifications(
        apiClient,
        {
          concurrencyToken: notificationDraft.concurrencyToken,
          emailEnabled: notificationDraft.emailEnabled,
          enabledCategories: notificationDraft.enabledCategories,
          enabledChannels: notificationDraft.enabledChannels,
          inAppEnabled: notificationDraft.inAppEnabled,
          whatsAppEnabled: notificationDraft.whatsAppEnabled,
        },
        locale,
      )
    },
    onSuccess: invalidateSettings,
  })
  const catalogMutation = useMutation({
    mutationFn: () =>
      updateDomainCatalog(
        apiClient,
        selectedCatalogType,
        {
          items: catalogDraft.map((item) => ({
            code: item.code,
            concurrencyToken: item.concurrencyToken,
            isEnabled: item.isEnabled,
            labels: item.labels,
            sortOrder: item.sortOrder,
          })),
        },
        locale,
      ),
    onSuccess: invalidateSettings,
  })

  function toggleLocale(code: string, checked: boolean) {
    setLocalizationDraft((current) =>
      current
        ? {
            ...current,
            enabledLocales: checked
              ? [...new Set([...current.enabledLocales, code])]
              : current.enabledLocales.filter((item) => item !== code),
          }
        : current,
    )
  }

  function toggleNotificationList(
    field: 'enabledCategories' | 'enabledChannels',
    code: string,
    checked: boolean,
  ) {
    setNotificationDraft((current) =>
      current
        ? {
            ...current,
            [field]: checked
              ? [...new Set([...current[field], code])]
              : current[field].filter((item) => item !== code),
          }
        : current,
    )
  }

  async function handleLogoFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0]
    event.currentTarget.value = ''

    if (file) {
      await logoUploadMutation.mutateAsync(file)
    }
  }

  if (settingsQuery.isLoading) {
    return <LoadingState title={copy.common.loading} />
  }

  if (settingsQuery.isError || !dashboard) {
    return <ErrorState title={copy.common.pageTitle} onRetry={() => void settingsQuery.refetch()} />
  }

  const tabs = [
    {
      content: (
        <div style={{ display: 'grid', gap: 16 }}>
          <DetailSection title={copy.organization.title}>
            {organizationDraft ? (
              <fieldset
                disabled={!canWriteSettings}
                style={{ border: 0, display: 'grid', gap: 16, margin: 0, padding: 0 }}
              >
                <ValidationSummary
                  errors={toFieldErrors(organizationMutation.error)}
                  title={copy.common.validation}
                />
                <div style={sectionGridStyle}>
                  <FormField label={copy.organization.slug} required>
                    {({ id, isInvalid }) => (
                      <TextInput
                        id={id}
                        isInvalid={isInvalid}
                        onChange={(event) =>
                          setOrganizationDraft({
                            ...organizationDraft,
                            slug: event.currentTarget.value,
                          })
                        }
                        value={organizationDraft.slug}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.organization.name} required>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setOrganizationDraft({
                            ...organizationDraft,
                            name: event.currentTarget.value,
                          })
                        }
                        value={organizationDraft.name}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.organization.displayName} required>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setOrganizationDraft({
                            ...organizationDraft,
                            displayName: event.currentTarget.value,
                          })
                        }
                        value={organizationDraft.displayName}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.organization.currencyCode} required>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        maxLength={3}
                        onChange={(event) =>
                          setOrganizationDraft({
                            ...organizationDraft,
                            currencyCode: event.currentTarget.value.toUpperCase(),
                          })
                        }
                        value={organizationDraft.currencyCode}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.organization.timeZone} required>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setOrganizationDraft({
                            ...organizationDraft,
                            timeZone: event.currentTarget.value,
                          })
                        }
                        value={organizationDraft.timeZone}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.organization.contactEmail}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setOrganizationDraft({
                            ...organizationDraft,
                            contactEmail: event.currentTarget.value,
                          })
                        }
                        type="email"
                        value={organizationDraft.contactEmail}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.organization.contactPhone}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setOrganizationDraft({
                            ...organizationDraft,
                            contactPhone: event.currentTarget.value,
                          })
                        }
                        value={organizationDraft.contactPhone}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.organization.contactWebsite}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setOrganizationDraft({
                            ...organizationDraft,
                            contactWebsite: event.currentTarget.value,
                          })
                        }
                        type="url"
                        value={organizationDraft.contactWebsite}
                      />
                    )}
                  </FormField>
                </div>
                {canWriteSettings ? (
                  <div style={actionRowStyle}>
                    <ActionButton
                      isLoading={organizationMutation.isPending}
                      onClick={() => organizationMutation.mutate()}
                      tone="primary"
                    >
                      {copy.actions.save}
                    </ActionButton>
                  </div>
                ) : null}
              </fieldset>
            ) : null}
          </DetailSection>
          <DetailSection title={copy.branding.title}>
            {brandingDraft ? (
              <fieldset
                disabled={!canWriteSettings && !canManageSettings}
                style={{ border: 0, display: 'grid', gap: 16, margin: 0, padding: 0 }}
              >
                <ValidationSummary
                  errors={toFieldErrors(brandingMutation.error)}
                  title={copy.common.validation}
                />
                <div style={sectionGridStyle}>
                  <FormField label={copy.branding.displayName}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setBrandingDraft({
                            ...brandingDraft,
                            displayName: event.currentTarget.value,
                          })
                        }
                        value={brandingDraft.displayName}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.branding.logoUrl}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setBrandingDraft({ ...brandingDraft, logoUrl: event.currentTarget.value })
                        }
                        type="url"
                        value={brandingDraft.logoUrl}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.branding.logoAlt}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setBrandingDraft({ ...brandingDraft, logoAlt: event.currentTarget.value })
                        }
                        value={brandingDraft.logoAlt}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.branding.supportEmail}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setBrandingDraft({
                            ...brandingDraft,
                            supportEmail: event.currentTarget.value,
                          })
                        }
                        type="email"
                        value={brandingDraft.supportEmail}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.branding.supportPhone}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setBrandingDraft({
                            ...brandingDraft,
                            supportPhone: event.currentTarget.value,
                          })
                        }
                        value={brandingDraft.supportPhone}
                      />
                    )}
                  </FormField>
                  <FormField label={copy.branding.supportUrl}>
                    {({ id }) => (
                      <TextInput
                        id={id}
                        onChange={(event) =>
                          setBrandingDraft({
                            ...brandingDraft,
                            supportUrl: event.currentTarget.value,
                          })
                        }
                        type="url"
                        value={brandingDraft.supportUrl}
                      />
                    )}
                  </FormField>
                  <ColorField
                    label={copy.branding.primaryColor}
                    onChange={(value) =>
                      setBrandingDraft({ ...brandingDraft, primaryColor: value })
                    }
                    value={brandingDraft.primaryColor}
                  />
                  <ColorField
                    label={copy.branding.primaryForegroundColor}
                    onChange={(value) =>
                      setBrandingDraft({ ...brandingDraft, primaryForegroundColor: value })
                    }
                    value={brandingDraft.primaryForegroundColor}
                  />
                  <ColorField
                    label={copy.branding.accentColor}
                    onChange={(value) => setBrandingDraft({ ...brandingDraft, accentColor: value })}
                    value={brandingDraft.accentColor}
                  />
                  <ColorField
                    label={copy.branding.accentForegroundColor}
                    onChange={(value) =>
                      setBrandingDraft({ ...brandingDraft, accentForegroundColor: value })
                    }
                    value={brandingDraft.accentForegroundColor}
                  />
                </div>
                {dashboard.branding.logo ? (
                  <StatusBadge label={dashboard.branding.logo.fileName} tone="info" />
                ) : null}
                {canWriteSettings || canManageSettings ? (
                  <div style={actionRowStyle}>
                    {canManageSettings ? (
                      <>
                        <label
                          style={{
                            alignItems: 'center',
                            background: 'var(--als-color-surface, #ffffff)',
                            border: '1px solid var(--als-color-border, #d7deea)',
                            borderRadius: 'var(--als-radius-md, 8px)',
                            color: 'var(--als-color-text, #1f2937)',
                            cursor: logoUploadMutation.isPending ? 'not-allowed' : 'pointer',
                            display: 'inline-flex',
                            font: 'inherit',
                            fontWeight: 700,
                            gap: 8,
                            justifyContent: 'center',
                            minHeight: 44,
                            minWidth: 44,
                            opacity: logoUploadMutation.isPending ? 0.62 : 1,
                            paddingInline: 16,
                          }}
                        >
                          <input
                            accept="image/png,image/jpeg,image/webp"
                            aria-label={copy.branding.logoFile}
                            disabled={logoUploadMutation.isPending}
                            onChange={(event) => void handleLogoFileChange(event)}
                            style={{ display: 'none' }}
                            type="file"
                          />
                          <Upload aria-hidden="true" size={16} />
                          {copy.actions.uploadLogo}
                        </label>
                        <ActionButton
                          disabled={!dashboard.branding.logo}
                          isLoading={logoRemoveMutation.isPending}
                          onClick={() => logoRemoveMutation.mutate()}
                          tone="ghost"
                        >
                          {copy.actions.removeLogo}
                        </ActionButton>
                        <ActionButton
                          isLoading={resetBrandingMutation.isPending}
                          onClick={() => resetBrandingMutation.mutate()}
                          tone="ghost"
                        >
                          {copy.actions.resetBranding}
                        </ActionButton>
                      </>
                    ) : null}
                    {canWriteSettings ? (
                      <ActionButton
                        isLoading={brandingMutation.isPending}
                        onClick={() => brandingMutation.mutate()}
                        tone="primary"
                      >
                        {copy.actions.save}
                      </ActionButton>
                    ) : null}
                  </div>
                ) : null}
              </fieldset>
            ) : null}
          </DetailSection>
        </div>
      ),
      icon: <Building2 aria-hidden="true" size={16} />,
      id: 'organization',
      label: copy.tabs.organization,
    },
    {
      content: (
        <DetailSection title={copy.tenant.title}>
          {tenantDraft ? (
            <div style={{ display: 'grid', gap: 16 }}>
              <CheckboxInput
                checked={tenantDraft.allowOrganizationSwitching}
                label={copy.tenant.allowOrganizationSwitching}
                onChange={(event) =>
                  setTenantDraft({
                    ...tenantDraft,
                    allowOrganizationSwitching: event.currentTarget.checked,
                  })
                }
              />
              <CheckboxInput
                checked={tenantDraft.requireActiveOrganization}
                label={copy.tenant.requireActiveOrganization}
                onChange={(event) =>
                  setTenantDraft({
                    ...tenantDraft,
                    requireActiveOrganization: event.currentTarget.checked,
                  })
                }
              />
              <CheckboxInput
                checked={tenantDraft.strictTenantIsolation}
                label={copy.tenant.strictTenantIsolation}
                onChange={(event) =>
                  setTenantDraft({
                    ...tenantDraft,
                    strictTenantIsolation: event.currentTarget.checked,
                  })
                }
              />
              <div style={actionRowStyle}>
                <ActionButton
                  isLoading={tenantMutation.isPending}
                  onClick={() => tenantMutation.mutate()}
                  tone="primary"
                >
                  {copy.actions.save}
                </ActionButton>
              </div>
            </div>
          ) : null}
        </DetailSection>
      ),
      icon: <Users aria-hidden="true" size={16} />,
      id: 'tenant',
      label: copy.tabs.tenant,
      manageOnly: true,
    },
    {
      content: (
        <DetailSection title={copy.residentPortal.title}>
          {residentPortalDraft ? (
            <fieldset
              disabled={!canWriteSettings}
              style={{ border: 0, display: 'grid', gap: 16, margin: 0, padding: 0 }}
            >
              <CheckboxInput
                checked={residentPortalDraft.isEnabled}
                label={copy.residentPortal.isEnabled}
                onChange={(event) =>
                  setResidentPortalDraft({
                    ...residentPortalDraft,
                    isEnabled: event.currentTarget.checked,
                  })
                }
              />
              <CheckboxInput
                checked={residentPortalDraft.allowOccurrenceCreation}
                label={copy.residentPortal.allowOccurrenceCreation}
                onChange={(event) =>
                  setResidentPortalDraft({
                    ...residentPortalDraft,
                    allowOccurrenceCreation: event.currentTarget.checked,
                  })
                }
              />
              <CheckboxInput
                checked={residentPortalDraft.allowDocumentUpload}
                label={copy.residentPortal.allowDocumentUpload}
                onChange={(event) =>
                  setResidentPortalDraft({
                    ...residentPortalDraft,
                    allowDocumentUpload: event.currentTarget.checked,
                  })
                }
              />
              <CheckboxInput
                checked={residentPortalDraft.allowProfileUpdateRequests}
                label={copy.residentPortal.allowProfileUpdateRequests}
                onChange={(event) =>
                  setResidentPortalDraft({
                    ...residentPortalDraft,
                    allowProfileUpdateRequests: event.currentTarget.checked,
                  })
                }
              />
              {canWriteSettings ? (
                <div style={actionRowStyle}>
                  <ActionButton
                    isLoading={residentPortalMutation.isPending}
                    onClick={() => residentPortalMutation.mutate()}
                    tone="primary"
                  >
                    {copy.actions.save}
                  </ActionButton>
                </div>
              ) : null}
            </fieldset>
          ) : null}
        </DetailSection>
      ),
      icon: <UserCog aria-hidden="true" size={16} />,
      id: 'residentPortal',
      label: copy.tabs.residentPortal,
    },
    {
      content: (
        <DetailSection title={copy.localization.title}>
          {localizationDraft ? (
            <fieldset
              disabled={!canWriteSettings}
              style={{ border: 0, display: 'grid', gap: 16, margin: 0, padding: 0 }}
            >
              <div style={sectionGridStyle}>
                <FormField label={copy.localization.defaultLocale}>
                  {({ id }) => (
                    <SelectInput
                      id={id}
                      onChange={(event) =>
                        setLocalizationDraft({
                          ...localizationDraft,
                          defaultLocale: event.currentTarget.value,
                        })
                      }
                      options={localizationDraft.supportedLocales.map((option) => ({
                        label: option.label,
                        value: option.code,
                      }))}
                      value={localizationDraft.defaultLocale}
                    />
                  )}
                </FormField>
                <FormField label={copy.localization.fallbackLocale}>
                  {({ id }) => (
                    <SelectInput
                      id={id}
                      onChange={(event) =>
                        setLocalizationDraft({
                          ...localizationDraft,
                          fallbackLocale: event.currentTarget.value,
                        })
                      }
                      options={localizationDraft.supportedLocales.map((option) => ({
                        label: option.label,
                        value: option.code,
                      }))}
                      value={localizationDraft.fallbackLocale}
                    />
                  )}
                </FormField>
              </div>
              <div style={{ display: 'grid', gap: 10 }}>
                <strong>{copy.localization.enabledLocales}</strong>
                {localizationDraft.supportedLocales.map((option) => (
                  <CheckboxInput
                    key={option.code}
                    checked={localizationDraft.enabledLocales.includes(option.code)}
                    label={option.label}
                    onChange={(event) => toggleLocale(option.code, event.currentTarget.checked)}
                  />
                ))}
              </div>
              {canWriteSettings ? (
                <div style={actionRowStyle}>
                  <ActionButton
                    isLoading={localizationMutation.isPending}
                    onClick={() => localizationMutation.mutate()}
                    tone="primary"
                  >
                    {copy.actions.save}
                  </ActionButton>
                </div>
              ) : null}
            </fieldset>
          ) : null}
        </DetailSection>
      ),
      icon: <Languages aria-hidden="true" size={16} />,
      id: 'localization',
      label: copy.tabs.localization,
    },
    {
      content: (
        <DetailSection title={copy.catalogs.title}>
          <div style={{ display: 'grid', gap: 16 }}>
            <FormField label={copy.tabs.catalogs}>
              {({ id }) => (
                <SelectInput
                  id={id}
                  onChange={(event) => setSelectedCatalogType(event.currentTarget.value)}
                  options={dashboard.catalogs.map((catalog) => ({
                    label: catalog.label,
                    value: catalog.catalogType,
                  }))}
                  value={selectedCatalogType}
                />
              )}
            </FormField>
            <DataTable
              columns={[
                {
                  cell: (item) => item.code,
                  header: copy.catalogs.code,
                  id: 'code',
                  width: 160,
                },
                {
                  cell: (item) => (
                    <TextInput
                      aria-label={`${item.code} ${copy.catalogs.pt}`}
                      onChange={(event) =>
                        setCatalogDraft((current) =>
                          updateCatalogItem(current, item.code, (currentItem) => ({
                            ...currentItem,
                            labels: { ...currentItem.labels, 'pt-BR': event.currentTarget.value },
                          })),
                        )
                      }
                      style={compactInputStyle}
                      value={item.labels['pt-BR'] ?? ''}
                    />
                  ),
                  header: copy.catalogs.pt,
                  id: 'pt',
                },
                {
                  cell: (item) => (
                    <TextInput
                      aria-label={`${item.code} ${copy.catalogs.en}`}
                      onChange={(event) =>
                        setCatalogDraft((current) =>
                          updateCatalogItem(current, item.code, (currentItem) => ({
                            ...currentItem,
                            labels: { ...currentItem.labels, 'en-US': event.currentTarget.value },
                          })),
                        )
                      }
                      style={compactInputStyle}
                      value={item.labels['en-US'] ?? ''}
                    />
                  ),
                  header: copy.catalogs.en,
                  id: 'en',
                },
                {
                  cell: (item) => (
                    <TextInput
                      aria-label={`${item.code} ${copy.catalogs.sortOrder}`}
                      onChange={(event) =>
                        setCatalogDraft((current) =>
                          updateCatalogItem(current, item.code, (currentItem) => ({
                            ...currentItem,
                            sortOrder: Number(event.currentTarget.value),
                          })),
                        )
                      }
                      style={compactInputStyle}
                      type="number"
                      value={item.sortOrder}
                    />
                  ),
                  header: copy.catalogs.sortOrder,
                  id: 'sortOrder',
                  width: 112,
                },
                {
                  cell: (item) => (
                    <input
                      aria-label={`${item.code} ${copy.catalogs.enabled}`}
                      checked={item.isEnabled}
                      onChange={(event) =>
                        setCatalogDraft((current) =>
                          updateCatalogItem(current, item.code, (currentItem) => ({
                            ...currentItem,
                            isEnabled: event.currentTarget.checked,
                          })),
                        )
                      }
                      type="checkbox"
                    />
                  ),
                  header: copy.catalogs.enabled,
                  id: 'enabled',
                  width: 90,
                },
              ]}
              getRowKey={(row) => row.code}
              rows={catalogDraft}
            />
            <div style={actionRowStyle}>
              <ActionButton
                isLoading={catalogMutation.isPending}
                onClick={() => catalogMutation.mutate()}
                tone="primary"
              >
                {copy.actions.save}
              </ActionButton>
            </div>
          </div>
        </DetailSection>
      ),
      icon: <Tags aria-hidden="true" size={16} />,
      id: 'catalogs',
      label: copy.tabs.catalogs,
      manageOnly: true,
    },
    {
      content: (
        <DetailSection title={copy.notifications.title}>
          {notificationDraft ? (
            <div style={{ display: 'grid', gap: 16 }}>
              <div style={sectionGridStyle}>
                <CheckboxInput
                  checked={notificationDraft.inAppEnabled}
                  label={copy.notifications.inApp}
                  onChange={(event) =>
                    setNotificationDraft({
                      ...notificationDraft,
                      inAppEnabled: event.currentTarget.checked,
                    })
                  }
                />
                <CheckboxInput
                  checked={notificationDraft.emailEnabled}
                  label={copy.notifications.email}
                  onChange={(event) =>
                    setNotificationDraft({
                      ...notificationDraft,
                      emailEnabled: event.currentTarget.checked,
                    })
                  }
                />
                <CheckboxInput
                  checked={notificationDraft.whatsAppEnabled}
                  label={copy.notifications.whatsapp}
                  onChange={(event) =>
                    setNotificationDraft({
                      ...notificationDraft,
                      whatsAppEnabled: event.currentTarget.checked,
                    })
                  }
                />
              </div>
              <div style={sectionGridStyle}>
                <div style={{ display: 'grid', gap: 10 }}>
                  <strong>{copy.notifications.categories}</strong>
                  {notificationDraft.availableCategories.map((option) => (
                    <CheckboxInput
                      key={option.code}
                      checked={notificationDraft.enabledCategories.includes(option.code)}
                      label={option.label}
                      onChange={(event) =>
                        toggleNotificationList(
                          'enabledCategories',
                          option.code,
                          event.currentTarget.checked,
                        )
                      }
                    />
                  ))}
                </div>
                <div style={{ display: 'grid', gap: 10 }}>
                  <strong>{copy.notifications.channels}</strong>
                  {notificationDraft.availableChannels.map((option) => (
                    <CheckboxInput
                      key={option.code}
                      checked={notificationDraft.enabledChannels.includes(option.code)}
                      label={option.label}
                      onChange={(event) =>
                        toggleNotificationList(
                          'enabledChannels',
                          option.code,
                          event.currentTarget.checked,
                        )
                      }
                    />
                  ))}
                </div>
              </div>
              <div style={actionRowStyle}>
                <ActionButton
                  isLoading={notificationsMutation.isPending}
                  onClick={() => notificationsMutation.mutate()}
                  tone="primary"
                >
                  {copy.actions.save}
                </ActionButton>
              </div>
            </div>
          ) : null}
        </DetailSection>
      ),
      icon: <Bell aria-hidden="true" size={16} />,
      id: 'notifications',
      label: copy.tabs.notifications,
      manageOnly: true,
    },
    {
      content: (
        <DetailSection title={copy.security.title}>
          {securityDraft ? (
            <div style={{ display: 'grid', gap: 16 }}>
              <div style={sectionGridStyle}>
                <FormField label={copy.security.sessionTimeoutMinutes}>
                  {({ id }) => (
                    <TextInput
                      id={id}
                      min={5}
                      onChange={(event) =>
                        setSecurityDraft({
                          ...securityDraft,
                          sessionTimeoutMinutes: Number(event.currentTarget.value),
                        })
                      }
                      type="number"
                      value={securityDraft.sessionTimeoutMinutes}
                    />
                  )}
                </FormField>
                <FormField label={copy.security.minimumLength}>
                  {({ id }) => (
                    <TextInput
                      id={id}
                      min={8}
                      onChange={(event) =>
                        setSecurityDraft({
                          ...securityDraft,
                          passwordRules: {
                            ...securityDraft.passwordRules,
                            minimumLength: Number(event.currentTarget.value),
                          },
                        })
                      }
                      type="number"
                      value={securityDraft.passwordRules.minimumLength}
                    />
                  )}
                </FormField>
                <FormField label={copy.security.mfaPolicy}>
                  {({ id }) => (
                    <SelectInput
                      id={id}
                      onChange={(event) =>
                        setSecurityDraft({ ...securityDraft, mfaPolicy: event.currentTarget.value })
                      }
                      options={securityDraft.supportedMfaPolicies.map((policy) => ({
                        label: getOptionLabel(policy),
                        value: policy,
                      }))}
                      value={securityDraft.mfaPolicy}
                    />
                  )}
                </FormField>
              </div>
              <div style={sectionGridStyle}>
                {(
                  [
                    ['requireUppercase', copy.security.requireUppercase],
                    ['requireLowercase', copy.security.requireLowercase],
                    ['requireDigit', copy.security.requireDigit],
                    ['requireSymbol', copy.security.requireSymbol],
                  ] as const
                ).map(([field, label]) => (
                  <CheckboxInput
                    key={field}
                    checked={securityDraft.passwordRules[field]}
                    label={label}
                    onChange={(event) =>
                      setSecurityDraft({
                        ...securityDraft,
                        passwordRules: {
                          ...securityDraft.passwordRules,
                          [field]: event.currentTarget.checked,
                        } as PasswordRuleSettings,
                      })
                    }
                  />
                ))}
              </div>
              <div style={actionRowStyle}>
                <ActionButton
                  isLoading={securityMutation.isPending}
                  onClick={() => securityMutation.mutate()}
                  tone="primary"
                >
                  {copy.actions.save}
                </ActionButton>
              </div>
            </div>
          ) : null}
        </DetailSection>
      ),
      icon: <KeyRound aria-hidden="true" size={16} />,
      id: 'security',
      label: copy.tabs.security,
      manageOnly: true,
    },
    {
      content: (
        <DetailSection title={copy.localization.preference}>
          <div style={{ display: 'grid', gap: 16 }}>
            <p style={{ color: 'var(--als-color-text-muted, #64748b)', margin: 0 }}>
              {dashboard.profilePreferences.notice}
            </p>
            <FormField label={copy.localization.preference}>
              {({ id }) => (
                <SelectInput
                  id={id}
                  onChange={(event) => setProfileLocaleDraft(event.currentTarget.value)}
                  options={dashboard.profilePreferences.supportedLocales.map((option) => ({
                    label: option.label,
                    value: option.code,
                  }))}
                  value={profileLocaleDraft}
                />
              )}
            </FormField>
            <div style={actionRowStyle}>
              <ActionButton
                isLoading={profileMutation.isPending}
                onClick={() => profileMutation.mutate()}
                tone="primary"
              >
                {copy.actions.save}
              </ActionButton>
            </div>
          </div>
        </DetailSection>
      ),
      icon: <UserCog aria-hidden="true" size={16} />,
      id: 'profile',
      label: copy.tabs.profile,
    },
  ]

  const visibleTabs = tabs.filter((tab) => !tab.manageOnly || canManageSettings)
  const safeSelectedTab = visibleTabs.some((tab) => tab.id === selectedTab)
    ? selectedTab
    : visibleTabs[0]?.id

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        actions={
          <ActionButton
            icon={<RefreshCw aria-hidden="true" size={16} />}
            isLoading={settingsQuery.isFetching}
            onClick={() => void settingsQuery.refetch()}
          >
            {copy.actions.refresh}
          </ActionButton>
        }
        description={copy.common.pageDescription}
        title={copy.common.pageTitle}
      />
      <Tabs
        ariaLabel={copy.common.pageTitle}
        onSelectedIdChange={setSelectedTab}
        selectedId={safeSelectedTab}
        tabs={visibleTabs}
      />
    </section>
  )
}
