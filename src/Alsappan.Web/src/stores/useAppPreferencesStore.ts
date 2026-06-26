import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { defaultLocale, normalizeAppLocale, type AppLocale } from '../i18n'
import { defaultAppTheme, isAppTheme, type AppTheme } from '../lib/theme/tokens'

export type { AppLocale, AppTheme }

export type AppDensity = 'comfortable' | 'compact'
export type DateFormatPreference = 'short' | 'medium'

export type AppPreferencesState = {
  locale: AppLocale
  setLocale: (locale: AppLocale) => void
  theme: AppTheme
  setTheme: (theme: AppTheme) => void
  toggleTheme: () => void
  density: AppDensity
  setDensity: (density: AppDensity) => void
  dateFormat: DateFormatPreference
  setDateFormat: (dateFormat: DateFormatPreference) => void
  resetPreferences: () => void
}

type AppPreferenceSnapshot = Pick<
  AppPreferencesState,
  'dateFormat' | 'density' | 'locale' | 'theme'
>

const defaultPreferences: AppPreferenceSnapshot = {
  dateFormat: 'short',
  density: 'comfortable',
  locale: defaultLocale,
  theme: defaultAppTheme,
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isAppDensity(value: unknown): value is AppDensity {
  return value === 'comfortable' || value === 'compact'
}

function isDateFormatPreference(value: unknown): value is DateFormatPreference {
  return value === 'short' || value === 'medium'
}

function coercePersistedPreferences(value: unknown): Partial<AppPreferenceSnapshot> {
  const persistedState = isRecord(value) && isRecord(value.state) ? value.state : value

  if (!isRecord(persistedState)) {
    return {}
  }

  return {
    dateFormat: isDateFormatPreference(persistedState.dateFormat)
      ? persistedState.dateFormat
      : defaultPreferences.dateFormat,
    density: isAppDensity(persistedState.density)
      ? persistedState.density
      : defaultPreferences.density,
    locale: normalizeAppLocale(persistedState.locale),
    theme: isAppTheme(persistedState.theme) ? persistedState.theme : defaultPreferences.theme,
  }
}

export const useAppPreferencesStore = create<AppPreferencesState>()(
  persist(
    (set) => ({
      ...defaultPreferences,
      setLocale: (locale) => set({ locale: normalizeAppLocale(locale) }),
      setTheme: (theme) =>
        set({
          theme: isAppTheme(theme) ? theme : defaultPreferences.theme,
        }),
      toggleTheme: () =>
        set((state) => ({
          theme: state.theme === 'dark' ? 'light' : 'dark',
        })),
      setDensity: (density) =>
        set({
          density: isAppDensity(density) ? density : defaultPreferences.density,
        }),
      setDateFormat: (dateFormat) =>
        set({
          dateFormat: isDateFormatPreference(dateFormat)
            ? dateFormat
            : defaultPreferences.dateFormat,
        }),
      resetPreferences: () => set(defaultPreferences),
    }),
    {
      merge: (persistedState, currentState) => ({
        ...currentState,
        ...coercePersistedPreferences(persistedState),
      }),
      migrate: (persistedState) => coercePersistedPreferences(persistedState),
      name: 'alsappan.preferences',
      partialize: (state) => ({
        dateFormat: state.dateFormat,
        density: state.density,
        locale: state.locale,
        theme: state.theme,
      }),
      version: 1,
    },
  ),
)
