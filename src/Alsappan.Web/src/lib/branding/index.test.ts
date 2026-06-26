import {
  applyOrganizationBranding,
  brandingValidationMessageKeys,
  createOrganizationBrandCssVariables,
  organizationBrandingSchema,
  toBrandingFieldErrorMap,
} from '.'

describe('organization branding foundation', () => {
  it('creates safe CSS variables from organization colors', () => {
    const tokens = createOrganizationBrandCssVariables({
      accentColor: '#8a4b00',
      accentForegroundColor: '#ffffff',
      primaryColor: '#005f73',
      primaryForegroundColor: '#ffffff',
    })

    expect(tokens['--alsappan-primary']).toBe('#005f73')
    expect(tokens['--alsappan-primary-foreground']).toBe('#ffffff')
    expect(tokens['--alsappan-accent']).toBe('#8a4b00')
  })

  it('rejects unsafe brand color contrast with localized message keys', () => {
    const result = organizationBrandingSchema.safeParse({
      primaryColor: '#ffffff',
      primaryForegroundColor: '#fffffe',
    })

    expect(result.success).toBe(false)

    if (result.success) {
      throw new Error('Expected unsafe contrast to fail validation')
    }

    expect(toBrandingFieldErrorMap(result.error)).toEqual({
      primaryForegroundColor: [brandingValidationMessageKeys.colorContrast],
    })
  })

  it('validates logo preview metadata and alt text', () => {
    const result = organizationBrandingSchema.safeParse({
      logoPreview: {
        fileName: 'logo.gif',
        mimeType: 'image/gif',
        sizeBytes: 2,
      },
    })

    expect(result.success).toBe(false)

    if (result.success) {
      throw new Error('Expected unsupported logo preview to fail validation')
    }

    expect(toBrandingFieldErrorMap(result.error)).toMatchObject({
      logoAlt: [brandingValidationMessageKeys.logoAltRequired],
      'logoPreview.mimeType': [brandingValidationMessageKeys.logoMimeType],
    })
  })

  it('applies resolved white-label tokens to a target element', () => {
    const target = document.createElement('div')
    const branding = applyOrganizationBranding(
      {
        displayName: 'Client Portal',
        primaryColor: '#003f5c',
        primaryForegroundColor: '#ffffff',
      },
      target,
    )

    expect(branding.displayName).toBe('Client Portal')
    expect(target.style.getPropertyValue('--alsappan-primary')).toBe('#003f5c')
  })
})
