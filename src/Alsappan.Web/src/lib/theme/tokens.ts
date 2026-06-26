export type AppTheme = 'light' | 'dark'

export type ThemeColorTokens = {
  accent: string
  accentForeground: string
  background: string
  border: string
  card: string
  danger: string
  dangerForeground: string
  foreground: string
  muted: string
  mutedForeground: string
  primary: string
  primaryForeground: string
  ring: string
  success: string
  successForeground: string
  warning: string
  warningForeground: string
}

export type ThemeRadiusTokens = {
  control: string
  panel: string
}

export type ThemeSpacingTokens = {
  contentMaxWidth: string
  shellGutter: string
}

export type ThemeTokens = {
  color: ThemeColorTokens
  radius: ThemeRadiusTokens
  spacing: ThemeSpacingTokens
}

export type ThemeCssVariables = Record<string, string>

export const appThemes = ['light', 'dark'] as const
export const defaultAppTheme: AppTheme = 'light'

export const defaultAlsappanThemeTokens: ThemeTokens = {
  color: {
    accent: '#d97706',
    accentForeground: '#ffffff',
    background: '#f8fafc',
    border: '#d7dee8',
    card: '#ffffff',
    danger: '#b42318',
    dangerForeground: '#ffffff',
    foreground: '#182230',
    muted: '#eef2f6',
    mutedForeground: '#526071',
    primary: '#16635f',
    primaryForeground: '#ffffff',
    ring: '#2f9c95',
    success: '#067647',
    successForeground: '#ffffff',
    warning: '#b54708',
    warningForeground: '#ffffff',
  },
  radius: {
    control: '6px',
    panel: '8px',
  },
  spacing: {
    contentMaxWidth: '1440px',
    shellGutter: '24px',
  },
}

export const darkAlsappanThemeTokens: ThemeTokens = {
  color: {
    accent: '#f4b740',
    accentForeground: '#1f1600',
    background: '#101828',
    border: '#344054',
    card: '#182230',
    danger: '#f97066',
    dangerForeground: '#310b08',
    foreground: '#f8fafc',
    muted: '#202b3d',
    mutedForeground: '#c4cdd9',
    primary: '#47bdb6',
    primaryForeground: '#052321',
    ring: '#7bd3ce',
    success: '#47cd89',
    successForeground: '#052e1c',
    warning: '#fdb022',
    warningForeground: '#2e1b00',
  },
  radius: defaultAlsappanThemeTokens.radius,
  spacing: defaultAlsappanThemeTokens.spacing,
}

export function isAppTheme(value: unknown): value is AppTheme {
  return typeof value === 'string' && appThemes.includes(value as AppTheme)
}

export function getThemeTokens(theme: AppTheme) {
  return theme === 'dark' ? darkAlsappanThemeTokens : defaultAlsappanThemeTokens
}

export function createThemeCssVariables(tokens: ThemeTokens): ThemeCssVariables {
  return {
    '--alsappan-accent': tokens.color.accent,
    '--alsappan-accent-foreground': tokens.color.accentForeground,
    '--alsappan-background': tokens.color.background,
    '--alsappan-border': tokens.color.border,
    '--alsappan-card': tokens.color.card,
    '--alsappan-content-max-width': tokens.spacing.contentMaxWidth,
    '--alsappan-control-radius': tokens.radius.control,
    '--alsappan-danger': tokens.color.danger,
    '--alsappan-danger-foreground': tokens.color.dangerForeground,
    '--alsappan-foreground': tokens.color.foreground,
    '--alsappan-muted': tokens.color.muted,
    '--alsappan-muted-foreground': tokens.color.mutedForeground,
    '--alsappan-panel-radius': tokens.radius.panel,
    '--alsappan-primary': tokens.color.primary,
    '--alsappan-primary-foreground': tokens.color.primaryForeground,
    '--alsappan-ring': tokens.color.ring,
    '--alsappan-shell-gutter': tokens.spacing.shellGutter,
    '--alsappan-success': tokens.color.success,
    '--alsappan-success-foreground': tokens.color.successForeground,
    '--alsappan-warning': tokens.color.warning,
    '--alsappan-warning-foreground': tokens.color.warningForeground,
  }
}

export function mergeThemeTokens(
  tokens: ThemeTokens,
  overrides: Partial<ThemeTokens>,
): ThemeTokens {
  return {
    color: {
      ...tokens.color,
      ...overrides.color,
    },
    radius: {
      ...tokens.radius,
      ...overrides.radius,
    },
    spacing: {
      ...tokens.spacing,
      ...overrides.spacing,
    },
  }
}

export function normalizeHexColor(value: string) {
  const normalizedValue = value.trim().toLowerCase()

  if (/^#[0-9a-f]{6}$/.test(normalizedValue)) {
    return normalizedValue
  }

  if (/^#[0-9a-f]{3}$/.test(normalizedValue)) {
    const [, red, green, blue] = normalizedValue

    return `#${red}${red}${green}${green}${blue}${blue}`
  }

  return undefined
}

function hexToRgb(value: string) {
  const normalizedValue = normalizeHexColor(value)

  if (!normalizedValue) {
    return undefined
  }

  return {
    blue: Number.parseInt(normalizedValue.slice(5, 7), 16),
    green: Number.parseInt(normalizedValue.slice(3, 5), 16),
    red: Number.parseInt(normalizedValue.slice(1, 3), 16),
  }
}

function getRelativeLuminance(value: string) {
  const rgb = hexToRgb(value)

  if (!rgb) {
    return undefined
  }

  const channels = [rgb.red, rgb.green, rgb.blue].map((channel) => {
    const normalizedChannel = channel / 255

    return normalizedChannel <= 0.03928
      ? normalizedChannel / 12.92
      : ((normalizedChannel + 0.055) / 1.055) ** 2.4
  })

  return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722
}

export function getContrastRatio(foreground: string, background: string) {
  const foregroundLuminance = getRelativeLuminance(foreground)
  const backgroundLuminance = getRelativeLuminance(background)

  if (foregroundLuminance === undefined || backgroundLuminance === undefined) {
    return 0
  }

  const lighter = Math.max(foregroundLuminance, backgroundLuminance)
  const darker = Math.min(foregroundLuminance, backgroundLuminance)

  return (lighter + 0.05) / (darker + 0.05)
}

export function meetsContrastRatio(foreground: string, background: string, minimumRatio = 4.5) {
  return getContrastRatio(foreground, background) >= minimumRatio
}

export function getReadableForeground(background: string) {
  return meetsContrastRatio('#ffffff', background) ? '#ffffff' : '#182230'
}
