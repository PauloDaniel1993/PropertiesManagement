import { z } from 'zod'
import { meetsContrastRatio, normalizeHexColor } from '../theme'

export const maxLogoPreviewBytes = 2 * 1024 * 1024
export const maxLogoPreviewDimension = 4096

export const supportedLogoMimeTypes = [
  'image/jpeg',
  'image/png',
  'image/svg+xml',
  'image/webp',
] as const

export type LogoMimeType = (typeof supportedLogoMimeTypes)[number]

export const brandingValidationMessageKeys = {
  colorContrast: 'validation.branding.colorContrast',
  colorHex: 'validation.branding.colorHex',
  logoAltRequired: 'validation.branding.logoAltRequired',
  logoDimension: 'validation.branding.logoDimension',
  logoMimeType: 'validation.branding.logoMimeType',
  logoSize: 'validation.branding.logoSize',
  logoUrl: 'validation.branding.logoUrl',
  previewUrl: 'validation.branding.previewUrl',
} as const

function blankStringToUndefined(value: unknown) {
  if (typeof value === 'string' && value.trim().length === 0) {
    return undefined
  }

  return value
}

function isSupportedLogoMimeType(value: string): value is LogoMimeType {
  return supportedLogoMimeTypes.includes(value as LogoMimeType)
}

function isPreviewUrl(value: string) {
  return (
    value.startsWith('blob:') ||
    value.startsWith('data:image/') ||
    value.startsWith('https://') ||
    value.startsWith('http://')
  )
}

const optionalTrimmedStringSchema = (maxLength: number) =>
  z.preprocess(
    blankStringToUndefined,
    z.string().trim().max(maxLength, { message: 'validation.maxLength' }).optional(),
  )

export const brandColorSchema = z
  .string()
  .trim()
  .refine((value) => normalizeHexColor(value) !== undefined, {
    message: brandingValidationMessageKeys.colorHex,
  })
  .transform((value) => normalizeHexColor(value) ?? value)

export const optionalBrandColorSchema = z.preprocess(
  blankStringToUndefined,
  brandColorSchema.optional(),
)

export const logoPreviewMetadataSchema = z.object({
  fileName: z.string().trim().min(1, { message: 'validation.required' }),
  height: z.coerce
    .number()
    .int()
    .min(1, { message: brandingValidationMessageKeys.logoDimension })
    .max(maxLogoPreviewDimension, { message: brandingValidationMessageKeys.logoDimension })
    .optional(),
  mimeType: z
    .string()
    .trim()
    .refine(isSupportedLogoMimeType, { message: brandingValidationMessageKeys.logoMimeType }),
  previewUrl: z
    .string()
    .trim()
    .refine(isPreviewUrl, { message: brandingValidationMessageKeys.previewUrl })
    .optional(),
  sizeBytes: z.coerce
    .number()
    .int()
    .min(1, { message: brandingValidationMessageKeys.logoSize })
    .max(maxLogoPreviewBytes, { message: brandingValidationMessageKeys.logoSize }),
  width: z.coerce
    .number()
    .int()
    .min(1, { message: brandingValidationMessageKeys.logoDimension })
    .max(maxLogoPreviewDimension, { message: brandingValidationMessageKeys.logoDimension })
    .optional(),
})

export const organizationBrandingSchema = z
  .object({
    accentColor: optionalBrandColorSchema,
    accentForegroundColor: optionalBrandColorSchema,
    displayName: optionalTrimmedStringSchema(120),
    logoAlt: optionalTrimmedStringSchema(140),
    logoPreview: logoPreviewMetadataSchema.optional(),
    logoUrl: z.preprocess(
      blankStringToUndefined,
      z.string().trim().url({ message: brandingValidationMessageKeys.logoUrl }).optional(),
    ),
    primaryColor: optionalBrandColorSchema,
    primaryForegroundColor: optionalBrandColorSchema,
  })
  .superRefine((branding, context) => {
    if (
      branding.primaryColor &&
      branding.primaryForegroundColor &&
      !meetsContrastRatio(branding.primaryForegroundColor, branding.primaryColor)
    ) {
      context.addIssue({
        code: 'custom',
        message: brandingValidationMessageKeys.colorContrast,
        path: ['primaryForegroundColor'],
      })
    }

    if (
      branding.accentColor &&
      branding.accentForegroundColor &&
      !meetsContrastRatio(branding.accentForegroundColor, branding.accentColor)
    ) {
      context.addIssue({
        code: 'custom',
        message: brandingValidationMessageKeys.colorContrast,
        path: ['accentForegroundColor'],
      })
    }

    if ((branding.logoUrl || branding.logoPreview) && !branding.logoAlt) {
      context.addIssue({
        code: 'custom',
        message: brandingValidationMessageKeys.logoAltRequired,
        path: ['logoAlt'],
      })
    }
  })

export type LogoPreviewMetadata = z.infer<typeof logoPreviewMetadataSchema>
export type OrganizationBranding = z.infer<typeof organizationBrandingSchema>

export type BrandingFieldErrorMap = Record<string, string[]>

export function toBrandingFieldErrorMap(error: z.ZodError): BrandingFieldErrorMap {
  return error.issues.reduce<BrandingFieldErrorMap>((errors, issue) => {
    const field = issue.path.length > 0 ? issue.path.join('.') : '$'
    errors[field] = [...(errors[field] ?? []), issue.message]

    return errors
  }, {})
}

export function validateOrganizationBranding(value: unknown) {
  return organizationBrandingSchema.safeParse(value)
}
