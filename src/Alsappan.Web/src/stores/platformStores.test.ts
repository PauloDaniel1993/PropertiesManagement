import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
  useFiltersStore,
  useResidentPortalStore,
  useShellStore,
} from '.'

describe('platform stores', () => {
  beforeEach(() => {
    localStorage.clear()
    useActiveOrganizationStore.getState().setOrganizations(defaultOrganizations)
    useAppPreferencesStore.getState().resetPreferences()
    useAuthSessionStore.getState().signOut()
    useFiltersStore.getState().resetFilters()
    useResidentPortalStore.getState().resetResidentPortalState()
    useShellStore.getState().resetShellState()
  })

  it('keeps the app preferences compatibility contract', () => {
    expect(useAppPreferencesStore.getState().locale).toBe('pt-BR')
    expect(useAppPreferencesStore.getState().theme).toBe('light')

    useAppPreferencesStore.getState().setLocale('en-US')
    useAppPreferencesStore.getState().setTheme('dark')

    expect(useAppPreferencesStore.getState().locale).toBe('en-US')
    expect(useAppPreferencesStore.getState().theme).toBe('dark')

    useAppPreferencesStore.getState().toggleTheme()

    expect(useAppPreferencesStore.getState().theme).toBe('light')
  })

  it('tracks demo authentication state and clears it on sign out', () => {
    useAuthSessionStore.getState().setIntendedPath('/properties')
    useAuthSessionStore.getState().signInDemo()

    expect(useAuthSessionStore.getState()).toMatchObject({
      accessToken: 'demo-access-token',
      intendedPath: '/properties',
      isAuthenticated: true,
      user: {
        email: 'admin@alsappan.local',
      },
    })

    useAuthSessionStore.getState().signOut()

    expect(useAuthSessionStore.getState()).toMatchObject({
      accessToken: undefined,
      intendedPath: null,
      isAuthenticated: false,
      user: null,
    })
  })

  it('keeps the active organization aligned with available organizations', () => {
    useActiveOrganizationStore.getState().setOrganizations([
      {
        currencyCode: 'BRL',
        displayName: 'Organization A',
        id: 'org-a',
        locale: 'pt-BR',
        name: 'Organization A',
        slug: 'org-a',
      },
      {
        currencyCode: 'USD',
        displayName: 'Organization B',
        id: 'org-b',
        locale: 'en-US',
        name: 'Organization B',
        slug: 'org-b',
      },
    ])

    useActiveOrganizationStore.getState().setActiveOrganizationId('org-b')

    expect(useActiveOrganizationStore.getState().activeOrganization).toMatchObject({
      currencyCode: 'USD',
      id: 'org-b',
      locale: 'en-US',
    })
  })

  it('tracks shell navigation affordances', () => {
    useShellStore.getState().toggleSidebarCollapsed()
    useShellStore.getState().openMobileNavigation()

    expect(useShellStore.getState()).toMatchObject({
      isMobileNavigationOpen: true,
      isSidebarCollapsed: true,
    })

    useShellStore.getState().closeMobileNavigation()

    expect(useShellStore.getState().isMobileNavigationOpen).toBe(false)
  })

  it('stores scoped filters and resident portal UI state', () => {
    useFiltersStore.getState().setFilter('properties', 'status', 'active')
    useResidentPortalStore.getState().setActiveSection('payments')
    useResidentPortalStore.getState().openOccurrenceComposer()
    useResidentPortalStore.getState().setUnreadNotificationCount(3.8)

    expect(useFiltersStore.getState().filtersByScope.properties).toEqual({
      status: 'active',
    })
    expect(useResidentPortalStore.getState()).toMatchObject({
      activeSection: 'payments',
      isOccurrenceComposerOpen: true,
      unreadNotificationCount: 3,
    })
  })
})
