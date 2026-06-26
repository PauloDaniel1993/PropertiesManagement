import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { i18next } from './i18n'
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
    expect(screen.getByLabelText('E-mail')).toBeInTheDocument()
    expect(screen.getByLabelText('Senha')).toBeInTheDocument()
  })

  it('enters the authenticated admin shell after demo sign in', async () => {
    const user = userEvent.setup()

    render(
      <AppProviders>
        <App />
      </AppProviders>,
    )

    await user.click(await screen.findByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('navigation', { name: 'Administração' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Dashboard' })).toBeInTheDocument()

    await user.click(screen.getByLabelText('Perfil'))

    expect(screen.getByRole('menuitem', { name: 'Sair' })).toBeInTheDocument()
  })
})
