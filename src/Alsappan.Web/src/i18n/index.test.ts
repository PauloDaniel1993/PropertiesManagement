import {
  appLocaleStorageKey,
  changeAppLocale,
  defaultLocale,
  getPersistedLocale,
  i18next,
  persistLocalePreference,
  resolveInitialLocale,
} from '.'

function createMemoryStorage(initialValue?: string) {
  let value = initialValue

  return {
    getItem: vi.fn((_key: string) => value ?? null),
    setItem: vi.fn((_: string, nextValue: string) => {
      value = nextValue
    }),
  }
}

describe('i18n foundation', () => {
  afterEach(async () => {
    await changeAppLocale(defaultLocale, createMemoryStorage())
  })

  it('defaults to pt-BR and exposes shell/login translations', () => {
    expect(resolveInitialLocale(createMemoryStorage())).toBe('pt-BR')
    expect(i18next.t('shell.auth.loginTitle', { lng: 'pt-BR' })).toBe('Entrar')
    expect(i18next.t('shell.auth.loginTitle', { lng: 'en-US' })).toBe('Sign in')
    expect(i18next.t('shell.topbar.organizationLabel', { lng: 'pt-BR' })).toBe('Organização')
    expect(i18next.t('shell.areaLabel', { lng: 'pt-BR' })).toBe('Área administrativa')
    expect(i18next.t('shell.navigation.admin', { lng: 'en-US' })).toBe('Administration')
    expect(i18next.t('shell.locale.toggle', { lng: 'pt-BR' })).toBe('Alternar idioma')
    expect(i18next.t('shell.topbar.unreadNotifications', { count: 3, lng: 'en-US' })).toBe(
      'Unread notifications: 3',
    )
  })

  it('persists locale preference in the Zustand preferences payload', () => {
    const storage = createMemoryStorage()

    persistLocalePreference('en-US', storage)

    expect(getPersistedLocale(storage)).toBe('en-US')
    expect(JSON.parse(storage.getItem(appLocaleStorageKey) ?? '{}')).toMatchObject({
      state: {
        locale: 'en-US',
      },
    })
  })

  it('changes the active i18next locale and persists the preference', async () => {
    const storage = createMemoryStorage()

    await changeAppLocale('en-US', storage)

    expect(i18next.language).toBe('en-US')
    expect(getPersistedLocale(storage)).toBe('en-US')
  })
})
