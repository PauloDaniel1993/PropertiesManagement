import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import type { AuthSessionDto } from '../../lib/api/identity'
import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
} from '../../stores'
import { applyAuthSession } from '../identity/session'
import { AdministratorForm } from './AdministratorForm'
import { AdministratorsListPage } from './AdministratorsListPage'

const roleOptions = [
  { label: 'Administrador', value: 'Administrador' },
  { label: 'Leitura', value: 'Leitura' },
]

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

function renderWithApi(ui: React.ReactNode, fetchImpl: typeof fetch) {
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

function buildAdminSession(): AuthSessionDto {
  return {
    accessToken: 'access-org-a',
    expiresAt: '2026-06-27T12:00:00.000Z',
    refreshToken: 'refresh-org-a',
    tokenType: 'Bearer',
    user: {
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
      ],
      permissions: ['administrators.read', 'administrators.write'],
    },
  }
}

function resetStores() {
  localStorage.clear()
  useActiveOrganizationStore.getState().setOrganizations(defaultOrganizations)
  useAppPreferencesStore.getState().resetPreferences()
  useAuthSessionStore.getState().signOut()
}

describe('administrator management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('validates and submits administrator invite fields with localized messages', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()

    render(<AdministratorForm mode="create" onSubmit={onSubmit} roleOptions={roleOptions} />)

    await user.click(screen.getByRole('button', { name: 'Enviar convite' }))

    expect(screen.getByText('Informe o nome do administrador.')).toBeInTheDocument()
    expect(screen.getByText('Informe um e-mail válido.')).toBeInTheDocument()
    expect(screen.getByText('Selecione ao menos um perfil.')).toBeInTheDocument()

    await user.type(screen.getByLabelText(/Nome de exibição/), 'Bruno Gestor')
    await user.type(screen.getByLabelText(/E-mail/), 'bruno@example.com')
    await user.click(screen.getByLabelText('Administrador'))
    await user.click(screen.getByRole('button', { name: 'Enviar convite' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        displayName: 'Bruno Gestor',
        email: 'bruno@example.com',
        roleCodes: ['Administrador'],
        temporaryPassword: undefined,
      }),
    )
  })

  it('loads administrators with filters and executes row lifecycle actions', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildAdminSession())
    const fetchImpl = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(String(input))

      if (url.pathname === '/v1/administrators/role-options') {
        return jsonResponse(roleOptions)
      }

      if (url.pathname === '/v1/administrators/admin-a/deactivate') {
        return jsonResponse({
          displayName: 'Ana Admin',
          email: 'ana@example.com',
          id: 'admin-a',
          roleCodes: ['Administrador'],
          status: 'inactive',
        })
      }

      if (url.pathname === '/v1/administrators' && init?.method !== 'POST') {
        return jsonResponse({
          hasNextPage: false,
          hasPreviousPage: false,
          items: [
            {
              displayName: 'Ana Admin',
              email: 'ana@example.com',
              id: 'admin-a',
              lastLoginAt: '2026-06-27T10:00:00.000Z',
              roleCodes: ['Administrador'],
              status: 'active',
            },
          ],
          page: Number(url.searchParams.get('page') ?? 1),
          pageSize: Number(url.searchParams.get('pageSize') ?? 10),
          totalItems: 1,
          totalPages: 1,
        })
      }

      return jsonResponse({ title: 'Not found' }, 404)
    }) as unknown as typeof fetch

    renderWithApi(<AdministratorsListPage />, fetchImpl)

    expect(await screen.findByText('Ana Admin')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Status'), 'active')

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        expect.stringContaining('/v1/administrators?'),
        expect.any(Object),
      ),
    )
    expect(
      vi.mocked(fetchImpl).mock.calls.some(([input]) => String(input).includes('status=active')),
    ).toBe(true)

    await user.click(screen.getByLabelText('Ações do administrador'))
    await user.click(screen.getByRole('menuitem', { name: 'Desativar' }))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        'https://api.alsappan.test/v1/administrators/admin-a/deactivate',
        expect.objectContaining({ method: 'POST' }),
      ),
    )
  })
})
