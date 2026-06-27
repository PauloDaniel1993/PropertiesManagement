import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { i18next } from './i18n'
import type { AuthSessionDto } from './lib/api/identity'
import { App } from './App'
import { AppProviders } from './app/AppProviders'
import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
  useShellStore,
} from './stores'

function resetAppState() {
  localStorage.clear()
  useActiveOrganizationStore.getState().setOrganizations(defaultOrganizations)
  useAppPreferencesStore.getState().resetPreferences()
  useAuthSessionStore.getState().signOut()
  useShellStore.getState().resetShellState()
  void i18next.changeLanguage('pt-BR')
}

function setAuthenticatedSession(permissions: string[]) {
  const session: AuthSessionDto = {
    accessToken: 'access-demo',
    expiresAt: '2026-06-27T12:00:00.000Z',
    refreshToken: 'refresh-demo',
    tokenType: 'Bearer',
    user: {
      accountType: 'admin',
      activeOrganizationId: 'demo-alsappan',
      displayName: 'Demo Administrator',
      email: 'admin@alsappan.local',
      id: 'demo-admin',
      organizations: [
        {
          currencyCode: 'BRL',
          displayName: 'Alsappan',
          id: 'demo-alsappan',
          locale: 'pt-BR',
          name: 'Alsappan',
          permissionCodes: permissions,
          roleCodes: ['Administrador'],
          slug: 'alsappan',
        },
      ],
      permissions,
    },
  }

  useAuthSessionStore.getState().setSession(session)
}

describe('App shell routing', () => {
  beforeEach(() => {
    resetAppState()
    window.history.replaceState({}, '', '/')
  })

  it('redirects unauthenticated users to the Portuguese login route', async () => {
    render(
      <AppProviders>
        <App />
      </AppProviders>,
    )

    expect(await screen.findByRole('heading', { name: 'Entrar' })).toBeInTheDocument()
    expect(screen.getByLabelText(/E-mail/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Senha/)).toBeInTheDocument()
  })

  it('renders the authenticated admin shell with an existing session', async () => {
    const user = userEvent.setup()
    useAuthSessionStore.getState().signInDemo()
    window.history.replaceState({}, '', '/dashboard')

    render(
      <AppProviders>
        <App />
      </AppProviders>,
    )

    expect(await screen.findByRole('navigation', { name: 'Administração' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Dashboard' })).toBeInTheDocument()

    await user.click(screen.getByLabelText('Perfil'))

    expect(screen.getByRole('menuitem', { name: 'Sair' })).toBeInTheDocument()
  })

  it('hides sidebar items without active organization permissions', async () => {
    setAuthenticatedSession(['dashboard.read'])
    window.history.replaceState({}, '', '/dashboard')

    render(
      <AppProviders>
        <App />
      </AppProviders>,
    )

    expect(await screen.findByRole('link', { name: 'Dashboard' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Contas de Consumo' })).not.toBeInTheDocument()
  })

  it('blocks direct module navigation without the required permission', async () => {
    setAuthenticatedSession(['dashboard.read'])
    window.history.replaceState({}, '', '/contas-de-consumo')

    render(
      <AppProviders>
        <App />
      </AppProviders>,
    )

    expect(await screen.findByText('Acesso não autorizado')).toBeInTheDocument()
  })
})
