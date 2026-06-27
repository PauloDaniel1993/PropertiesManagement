import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import type { AuthSessionDto, CurrentUserDto } from '../../lib/api/identity'
import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
} from '../../stores'
import { RequireAdminRoute, RequireAuthenticated, RequireResidentRoute } from '../../routes/guards'
import { AuthSessionBootstrap } from './AuthSessionBootstrap'
import { IdentityLoginPage } from './LoginPage'
import { OrganizationSwitcher } from './components/OrganizationSwitcher'
import { applyAuthSession } from './session'

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      headers: { 'Content-Type': 'application/json' },
      status,
    }),
  )
}

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      mutations: { retry: false },
      queries: { retry: false },
    },
  })
}

function renderWithApi(ui: React.ReactNode, fetchImpl: typeof fetch = vi.fn()) {
  const apiClient = new ApiClient({
    baseUrl: 'https://api.alsappan.test',
    fetchImpl,
  })

  return render(
    <QueryClientProvider client={createQueryClient()}>
      <ApiClientContext.Provider value={apiClient}>
        <MemoryRouter>{ui}</MemoryRouter>
      </ApiClientContext.Provider>
    </QueryClientProvider>,
  )
}

function buildUser(overrides: Partial<CurrentUserDto> = {}): CurrentUserDto {
  return {
    accountType: 'admin',
    activeOrganizationId: 'org-a',
    displayName: 'Ana Admin',
    email: 'ana@example.com',
    id: 'user-a',
    organizations: [
      {
        currencyCode: 'BRL',
        displayName: 'Organização A',
        id: 'org-a',
        locale: 'pt-BR',
        name: 'Organização A',
        permissionCodes: ['administrators.read', 'administrators.write'],
        roleCodes: ['Administrador'],
        slug: 'org-a',
      },
      {
        currencyCode: 'BRL',
        displayName: 'Organização B',
        id: 'org-b',
        locale: 'pt-BR',
        name: 'Organização B',
        permissionCodes: ['administrators.read'],
        roleCodes: ['Leitura'],
        slug: 'org-b',
      },
    ],
    permissions: ['administrators.read', 'administrators.write'],
    ...overrides,
  }
}

function buildSession(user: CurrentUserDto = buildUser()): AuthSessionDto {
  return {
    accessToken: `access-${user.activeOrganizationId}`,
    expiresAt: '2026-06-27T12:00:00.000Z',
    refreshToken: `refresh-${user.activeOrganizationId}`,
    tokenType: 'Bearer',
    user,
  }
}

function resetStores() {
  localStorage.clear()
  useActiveOrganizationStore.getState().setOrganizations(defaultOrganizations)
  useAppPreferencesStore.getState().resetPreferences()
  useAuthSessionStore.getState().signOut()
}

describe('identity frontend contract', () => {
  beforeEach(() => {
    resetStores()
  })

  it('bootstraps a session through the refresh endpoint and syncs active organization state', async () => {
    const session = buildSession()
    const fetchImpl = vi.fn(() => jsonResponse(session)) as unknown as typeof fetch
    useAuthSessionStore.setState({ refreshToken: 'refresh-org-a' })

    renderWithApi(
      <AuthSessionBootstrap>
        <div>Aplicação autenticada</div>
      </AuthSessionBootstrap>,
      fetchImpl,
    )

    expect(await screen.findByText('Aplicação autenticada')).toBeInTheDocument()

    expect(fetchImpl).toHaveBeenCalledWith(
      'https://api.alsappan.test/v1/auth/refresh',
      expect.objectContaining({
        body: '{"refreshToken":"refresh-org-a"}',
        method: 'POST',
      }),
    )
    expect(useAuthSessionStore.getState().user).toMatchObject({
      accountType: 'admin',
      email: 'ana@example.com',
    })
    expect(useActiveOrganizationStore.getState().activeOrganization).toMatchObject({
      id: 'org-a',
      roleCodes: ['Administrador'],
    })
  })

  it('skips refresh when no refresh token exists locally', async () => {
    const fetchImpl = vi.fn() as unknown as typeof fetch

    renderWithApi(
      <AuthSessionBootstrap>
        <div>Rota pública</div>
      </AuthSessionBootstrap>,
      fetchImpl,
    )

    expect(await screen.findByText('Rota pública')).toBeInTheDocument()
    expect(fetchImpl).not.toHaveBeenCalled()
    expect(useAuthSessionStore.getState().sessionStatus).toBe('unauthenticated')
  })

  it('submits the localized admin login flow to the auth helper contract', async () => {
    const user = userEvent.setup()
    const session = buildSession()
    const fetchImpl = vi.fn(() => jsonResponse(session)) as unknown as typeof fetch

    renderWithApi(<IdentityLoginPage defaultRedirectPath="/dashboard" />, fetchImpl)

    await user.type(screen.getByLabelText(/E-mail/), 'ana@example.com')
    await user.type(screen.getByLabelText(/Senha/), 'secret123')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    await waitFor(() => expect(useAuthSessionStore.getState().isAuthenticated).toBe(true))

    expect(fetchImpl).toHaveBeenCalledWith(
      'https://api.alsappan.test/v1/auth/admin/login',
      expect.objectContaining({
        body: JSON.stringify({
          email: 'ana@example.com',
          password: 'secret123',
        }),
        method: 'POST',
      }),
    )
  })

  it('switches active organization and applies the returned session', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildSession())
    const switchedUser = buildUser({
      activeOrganizationId: 'org-b',
      permissions: ['administrators.read'],
    })
    const fetchImpl = vi.fn(() =>
      jsonResponse(buildSession(switchedUser)),
    ) as unknown as typeof fetch

    renderWithApi(<OrganizationSwitcher />, fetchImpl)

    await user.selectOptions(screen.getByLabelText('Organização ativa'), 'org-b')

    await waitFor(() =>
      expect(useActiveOrganizationStore.getState().activeOrganization).toMatchObject({
        id: 'org-b',
        roleCodes: ['Leitura'],
      }),
    )
    expect(fetchImpl).toHaveBeenCalledWith(
      'https://api.alsappan.test/v1/auth/switch-organization',
      expect.objectContaining({
        body: JSON.stringify({ organizationId: 'org-b' }),
        method: 'POST',
      }),
    )
  })

  it('stores intended route for unauthenticated users', async () => {
    render(
      <MemoryRouter initialEntries={['/administradores?status=active']}>
        <Routes>
          <Route
            path="/administradores"
            element={
              <RequireAuthenticated>
                <div>Administradores</div>
              </RequireAuthenticated>
            }
          />
          <Route path="/login" element={<div>Login</div>} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByText('Login')).toBeInTheDocument()
    expect(useAuthSessionStore.getState().intendedPath).toBe('/administradores?status=active')
  })

  it('separates resident and admin route surfaces', () => {
    applyAuthSession(buildSession(buildUser({ accountType: 'resident' })))

    const { rerender } = render(
      <MemoryRouter>
        <RequireAdminRoute>
          <div>Área administrativa</div>
        </RequireAdminRoute>
      </MemoryRouter>,
    )

    expect(screen.getByText('Acesso administrativo obrigatório')).toBeInTheDocument()

    applyAuthSession(buildSession(buildUser({ accountType: 'admin' })))
    rerender(
      <MemoryRouter>
        <RequireResidentRoute>
          <div>Portal do morador</div>
        </RequireResidentRoute>
      </MemoryRouter>,
    )

    expect(screen.getByText('Acesso de morador obrigatório')).toBeInTheDocument()
  })
})
