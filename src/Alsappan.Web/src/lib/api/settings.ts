import type { ApiClient } from './client'

export type LocaleOption = {
  code: string
  isDefault: boolean
  isEnabled: boolean
  isFallback: boolean
  label: string
}

export type OrganizationProfileSettings = {
  concurrencyToken?: string
  contactEmail?: string
  contactPhone?: string
  contactWebsite?: string
  createdAt: string
  currencyCode: string
  displayName: string
  name: string
  organizationId: string
  slug: string
  timeZone: string
  updatedAt?: string
}

export type OrganizationProfileUpdateRequest = {
  concurrencyToken?: string
  contactEmail?: string
  contactPhone?: string
  contactWebsite?: string
  currencyCode: string
  displayName: string
  name: string
  slug: string
  timeZone: string
}

export type TenantBehaviorSettings = {
  allowOrganizationSwitching: boolean
  concurrencyToken?: string
  requireActiveOrganization: boolean
  strictTenantIsolation: boolean
}

export type TenantBehaviorUpdateRequest = TenantBehaviorSettings

export type ResidentPortalSettings = {
  allowDocumentUpload: boolean
  allowOccurrenceCreation: boolean
  allowProfileUpdateRequests: boolean
  concurrencyToken?: string
  isEnabled: boolean
}

export type ResidentPortalUpdateRequest = ResidentPortalSettings

export type LocalizationSettings = {
  concurrencyToken?: string
  defaultLocale: string
  enabledLocales: string[]
  fallbackLocale: string
  supportedLocales: LocaleOption[]
}

export type LocalizationUpdateRequest = Omit<LocalizationSettings, 'supportedLocales'>

export type UserLocalePreference = {
  effectiveLocale: string
  isPersisted: boolean
  locale: string
  notice: string
  supportedLocales: LocaleOption[]
}

export type PasswordRuleSettings = {
  minimumLength: number
  requireDigit: boolean
  requireLowercase: boolean
  requireSymbol: boolean
  requireUppercase: boolean
}

export type SecuritySettings = {
  concurrencyToken?: string
  mfaPolicy: string
  passwordRules: PasswordRuleSettings
  sessionTimeoutMinutes: number
  supportedMfaPolicies: string[]
}

export type SecuritySettingsUpdateRequest = Omit<SecuritySettings, 'supportedMfaPolicies'>

export type NotificationSettingOption = {
  code: string
  isEnabled: boolean
  label: string
}

export type NotificationSettings = {
  availableCategories: NotificationSettingOption[]
  availableChannels: NotificationSettingOption[]
  concurrencyToken?: string
  emailEnabled: boolean
  enabledCategories: string[]
  enabledChannels: string[]
  inAppEnabled: boolean
  whatsAppEnabled: boolean
}

export type NotificationSettingsUpdateRequest = Omit<
  NotificationSettings,
  'availableCategories' | 'availableChannels'
>

export type BrandLogoMetadata = {
  contentType: string
  fileName: string
  height?: number
  sizeBytes: number
  storageKey: string
  width?: number
}

export type OrganizationBrandingSettings = {
  accentColor?: string
  accentForegroundColor?: string
  concurrencyToken?: string
  displayName?: string
  logo?: BrandLogoMetadata
  logoAlt?: string
  logoUrl?: string
  primaryColor?: string
  primaryForegroundColor?: string
  supportEmail?: string
  supportPhone?: string
  supportUrl?: string
}

export type OrganizationBrandingUpdateRequest = Omit<
  OrganizationBrandingSettings,
  'concurrencyToken' | 'logo'
> & {
  concurrencyToken?: string
}

export type BrandLogoUploadRequest = {
  concurrencyToken?: string
  contentBase64: string
  contentType: string
  fileName: string
  height: number
  logoAlt: string
  sizeBytes: number
  width: number
}

export type DomainCatalogItem = {
  catalogType: string
  code: string
  concurrencyToken?: string
  id: string
  isEnabled: boolean
  isSystem: boolean
  labels: Record<string, string>
  sortOrder: number
}

export type DomainCatalogSettings = {
  catalogType: string
  items: DomainCatalogItem[]
  label: string
}

export type DomainCatalogUpdateRequest = {
  items: Array<{
    code: string
    concurrencyToken?: string
    isEnabled: boolean
    labels: Record<string, string>
    sortOrder: number
  }>
}

export type SettingsDashboard = {
  branding: OrganizationBrandingSettings
  catalogs: DomainCatalogSettings[]
  localization: LocalizationSettings
  notifications: NotificationSettings
  organization: OrganizationProfileSettings
  profilePreferences: UserLocalePreference
  residentPortal: ResidentPortalSettings
  security: SecuritySettings
  tenantBehavior: TenantBehaviorSettings
}

export function getSettingsDashboard(client: ApiClient, locale?: string) {
  return client.get<SettingsDashboard>('/v1/settings', { query: { locale } })
}

export function updateOrganizationProfile(
  client: ApiClient,
  request: OrganizationProfileUpdateRequest,
  locale?: string,
) {
  return client.put<OrganizationProfileSettings, OrganizationProfileUpdateRequest>(
    '/v1/settings/organization',
    request,
    { query: { locale } },
  )
}

export function updateTenantBehavior(
  client: ApiClient,
  request: TenantBehaviorUpdateRequest,
  locale?: string,
) {
  return client.put<TenantBehaviorSettings, TenantBehaviorUpdateRequest>(
    '/v1/settings/tenant-behavior',
    request,
    { query: { locale } },
  )
}

export function updateResidentPortal(
  client: ApiClient,
  request: ResidentPortalUpdateRequest,
  locale?: string,
) {
  return client.put<ResidentPortalSettings, ResidentPortalUpdateRequest>(
    '/v1/settings/resident-portal',
    request,
    { query: { locale } },
  )
}

export function updateLocalization(
  client: ApiClient,
  request: LocalizationUpdateRequest,
  locale?: string,
) {
  return client.put<LocalizationSettings, LocalizationUpdateRequest>(
    '/v1/settings/localization',
    request,
    { query: { locale } },
  )
}

export function updateUserLocalePreference(
  client: ApiClient,
  locale: string,
  displayLocale?: string,
) {
  return client.put<UserLocalePreference, { locale: string }>(
    '/v1/settings/profile/locale',
    { locale },
    { query: { locale: displayLocale } },
  )
}

export function updateSecurity(
  client: ApiClient,
  request: SecuritySettingsUpdateRequest,
  locale?: string,
) {
  return client.put<SecuritySettings, SecuritySettingsUpdateRequest>(
    '/v1/settings/security',
    request,
    {
      query: { locale },
    },
  )
}

export function updateNotifications(
  client: ApiClient,
  request: NotificationSettingsUpdateRequest,
  locale?: string,
) {
  return client.put<NotificationSettings, NotificationSettingsUpdateRequest>(
    '/v1/settings/notifications',
    request,
    { query: { locale } },
  )
}

export function updateBranding(
  client: ApiClient,
  request: OrganizationBrandingUpdateRequest,
  locale?: string,
) {
  return client.put<OrganizationBrandingSettings, OrganizationBrandingUpdateRequest>(
    '/v1/settings/branding',
    request,
    { query: { locale } },
  )
}

export function uploadBrandLogo(
  client: ApiClient,
  request: BrandLogoUploadRequest,
  locale?: string,
) {
  return client.post<OrganizationBrandingSettings, BrandLogoUploadRequest>(
    '/v1/settings/branding/logo',
    request,
    { query: { locale } },
  )
}

export function removeBrandLogo(client: ApiClient, locale?: string) {
  return client.delete<OrganizationBrandingSettings>('/v1/settings/branding/logo', {
    query: { locale },
  })
}

export function resetBranding(client: ApiClient, locale?: string) {
  return client.post<OrganizationBrandingSettings, Record<string, never>>(
    '/v1/settings/branding/reset',
    {},
    { query: { locale } },
  )
}

export function updateDomainCatalog(
  client: ApiClient,
  catalogType: string,
  request: DomainCatalogUpdateRequest,
  locale?: string,
) {
  return client.put<DomainCatalogSettings, DomainCatalogUpdateRequest>(
    `/v1/settings/catalogs/${catalogType}`,
    request,
    { query: { locale } },
  )
}
