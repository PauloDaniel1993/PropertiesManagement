import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type AppLocale = 'pt-BR' | 'en-US'
export type AppTheme = 'light' | 'dark'

type AppPreferencesState = {
  locale: AppLocale
  setLocale: (locale: AppLocale) => void
  theme: AppTheme
  toggleTheme: () => void
}

export const useAppPreferencesStore = create<AppPreferencesState>()(
  persist(
    (set) => ({
      locale: 'pt-BR',
      setLocale: (locale) => set({ locale }),
      theme: 'light',
      toggleTheme: () =>
        set((state) => ({
          theme: state.theme === 'dark' ? 'light' : 'dark',
        })),
    }),
    {
      name: 'alsappan.preferences',
    },
  ),
)
