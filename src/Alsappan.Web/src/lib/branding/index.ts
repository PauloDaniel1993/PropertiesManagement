import {
  createThemeCssVariables,
  defaultAlsappanThemeTokens,
  getReadableForeground,
  normalizeHexColor,
  type ThemeCssVariables,
} from '../theme'
import {
  organizationBrandingSchema,
  type LogoPreviewMetadata,
  type OrganizationBranding,
} from './validation'

export type {
  BrandingFieldErrorMap,
  LogoPreviewMetadata,
  LogoMimeType,
  OrganizationBranding,
} from './validation'
export {
  brandingValidationMessageKeys,
  brandColorSchema,
  logoPreviewMetadataSchema,
  organizationBrandingSchema,
  supportedLogoMimeTypes,
  toBrandingFieldErrorMap,
  validateOrganizationBranding,
} from './validation'

export type ResolvedOrganizationBranding = {
  displayName: string
  logoAlt: string
  logoPreview?: LogoPreviewMetadata
  logoUrl?: string
  tokens: ThemeCssVariables
}

export const defaultBrandName = 'Alsappan'

export const defaultOrganizationBranding: ResolvedOrganizationBranding = {
  displayName: defaultBrandName,
  logoAlt: defaultBrandName,
  tokens: createThemeCssVariables(defaultAlsappanThemeTokens),
}

function normalizeOptionalColor(value: string | undefined) {
  return value ? normalizeHexColor(value) : undefined
}

function parseBranding(value: OrganizationBranding | null | undefined) {
  const result = organizationBrandingSchema.safeParse(value ?? {})

  return result.success ? result.data : {}
}

export function resolveOrganizationBranding(
  value: OrganizationBranding | null | undefined,
): ResolvedOrganizationBranding {
  const branding = parseBranding(value)
  const primaryColor =
    normalizeOptionalColor(branding.primaryColor) ?? defaultAlsappanThemeTokens.color.primary
  const accentColor =
    normalizeOptionalColor(branding.accentColor) ?? defaultAlsappanThemeTokens.color.accent
  const primaryForegroundColor =
    normalizeOptionalColor(branding.primaryForegroundColor) ?? getReadableForeground(primaryColor)
  const accentForegroundColor =
    normalizeOptionalColor(branding.accentForegroundColor) ?? getReadableForeground(accentColor)

  return {
    displayName: branding.displayName ?? defaultOrganizationBranding.displayName,
    logoAlt: branding.logoAlt ?? branding.displayName ?? defaultOrganizationBranding.logoAlt,
    logoPreview: branding.logoPreview,
    logoUrl: branding.logoUrl,
    tokens: {
      ...defaultOrganizationBranding.tokens,
      '--alsappan-accent': accentColor,
      '--alsappan-accent-foreground': accentForegroundColor,
      '--alsappan-primary': primaryColor,
      '--alsappan-primary-foreground': primaryForegroundColor,
      '--alsappan-ring': primaryColor,
    },
  }
}

export function createOrganizationBrandCssVariables(
  value: OrganizationBranding | null | undefined,
) {
  return resolveOrganizationBranding(value).tokens
}

export function applyOrganizationBranding(
  value: OrganizationBranding | null | undefined,
  target: HTMLElement = document.documentElement,
) {
  const branding = resolveOrganizationBranding(value)

  for (const [name, tokenValue] of Object.entries(branding.tokens)) {
    target.style.setProperty(name, tokenValue)
  }

  return branding
}
