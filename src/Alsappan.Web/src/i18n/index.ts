import i18next from 'i18next'
import { initReactI18next } from 'react-i18next'
import enUS from './locales/en-US.json'
import ptBR from './locales/pt-BR.json'

export const appLocales = ['pt-BR', 'en-US'] as const
export type AppLocale = (typeof appLocales)[number]

export const defaultLocale: AppLocale = 'pt-BR'
export const appLocaleStorageKey = 'alsappan.preferences'

export const appLocaleLabels: Record<AppLocale, string> = {
  'en-US': 'English (US)',
  'pt-BR': 'Portugues (Brasil)',
}

export const i18nResources = {
  'en-US': {
    translation: enUS,
  },
  'pt-BR': {
    translation: ptBR,
  },
} as const

type LocaleStorage = Pick<Storage, 'getItem' | 'setItem'>

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function getBrowserStorage(): LocaleStorage | undefined {
  if (typeof window === 'undefined') {
    return undefined
  }

  try {
    return window.localStorage
  } catch {
    return undefined
  }
}

function readPreferenceState(value: unknown) {
  if (!isRecord(value)) {
    return undefined
  }

  return isRecord(value.state) ? value.state : value
}

export function isAppLocale(value: unknown): value is AppLocale {
  return typeof value === 'string' && appLocales.includes(value as AppLocale)
}

export function normalizeAppLocale(value: unknown, fallback: AppLocale = defaultLocale) {
  return isAppLocale(value) ? value : fallback
}

export function getPersistedLocale(storage: LocaleStorage | undefined = getBrowserStorage()) {
  const rawValue = storage?.getItem(appLocaleStorageKey)

  if (!rawValue) {
    return undefined
  }

  try {
    const preferenceState = readPreferenceState(JSON.parse(rawValue))

    return isAppLocale(preferenceState?.locale) ? preferenceState.locale : undefined
  } catch {
    return undefined
  }
}

export function persistLocalePreference(
  locale: AppLocale,
  storage: LocaleStorage | undefined = getBrowserStorage(),
) {
  if (!storage) {
    return locale
  }

  const normalizedLocale = normalizeAppLocale(locale)

  try {
    const parsedValue = JSON.parse(storage.getItem(appLocaleStorageKey) ?? '{}')
    const persistedRecord = isRecord(parsedValue) ? parsedValue : {}
    const preferenceState = readPreferenceState(persistedRecord) ?? {}

    storage.setItem(
      appLocaleStorageKey,
      JSON.stringify({
        ...persistedRecord,
        state: {
          ...preferenceState,
          locale: normalizedLocale,
        },
        version: typeof persistedRecord.version === 'number' ? persistedRecord.version : 1,
      }),
    )
  } catch {
    storage.setItem(
      appLocaleStorageKey,
      JSON.stringify({
        state: {
          locale: normalizedLocale,
        },
        version: 1,
      }),
    )
  }

  return normalizedLocale
}

export function resolveInitialLocale(storage: LocaleStorage | undefined = getBrowserStorage()) {
  return getPersistedLocale(storage) ?? defaultLocale
}

export async function changeAppLocale(
  locale: AppLocale,
  storage: LocaleStorage | undefined = getBrowserStorage(),
) {
  const normalizedLocale = persistLocalePreference(locale, storage)
  await i18next.changeLanguage(normalizedLocale)

  return normalizedLocale
}

void i18next.use(initReactI18next).init({
  fallbackLng: defaultLocale,
  interpolation: {
    escapeValue: false,
  },
  lng: resolveInitialLocale(),
  resources: i18nResources,
  supportedLngs: appLocales,
})

export { i18next }
