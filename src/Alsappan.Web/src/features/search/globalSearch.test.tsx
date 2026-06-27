import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter, useLocation } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
  useFiltersStore,
} from '../../stores'
import { GlobalSearch } from './GlobalSearch'

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

function LocationProbe() {
  const location = useLocation()

  return <output aria-label="current-route">{`${location.pathname}${location.search}`}</output>
}

function renderWithApi(ui: ReactNode, fetchImpl: typeof fetch) {
  const apiClient = new ApiClient({
    baseUrl: 'https://api.alsappan.test',
    fetchImpl,
  })

  return render(
    <QueryClientProvider client={createQueryClient()}>
      <ApiClientContext.Provider value={apiClient}>
        <MemoryRouter initialEntries={['/dashboard']}>
          {ui}
          <LocationProbe />
        </MemoryRouter>
      </ApiClientContext.Provider>
    </QueryClientProvider>,
  )
}

function createSearchFetch() {
  return vi.fn((input: RequestInfo | URL) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/search' && url.searchParams.get('query') === 'zz') {
      return jsonResponse({
        groups: [],
        query: 'zz',
        totalItems: 0,
      })
    }

    if (url.pathname === '/v1/search') {
      return jsonResponse({
        groups: [
          {
            entityType: 'property',
            entityTypeLabel: 'Imovel',
            results: [
              {
                entityType: 'property',
                entityTypeLabel: 'Imovel',
                id: 'property-a',
                label: 'Casa Calabria',
                matchedField: 'Nome',
                route: '/imoveis?propertyId=property-a',
                summary: 'Rua Calabria, 82',
              },
            ],
            route: '/imoveis?search=casa',
          },
          {
            entityType: 'payment',
            entityTypeLabel: 'Pagamento',
            results: [
              {
                entityType: 'payment',
                entityTypeLabel: 'Pagamento',
                id: 'payment-a',
                label: 'Aluguel Calabria',
                matchedField: 'Nome',
                route: '/pagamentos?paymentId=payment-a',
                summary: 'Vencimento 2026-06-20',
              },
            ],
            route: '/pagamentos?search=casa',
          },
        ],
        query: 'casa',
        totalItems: 2,
      })
    }

    return jsonResponse({ title: 'Not found' }, 404)
  }) as unknown as typeof fetch
}

function resetStores() {
  localStorage.clear()
  useActiveOrganizationStore.getState().setOrganizations(defaultOrganizations)
  useAppPreferencesStore.getState().resetPreferences()
  useAuthSessionStore.getState().signOut()
  useFiltersStore.getState().resetFilters()
}

describe('global search', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads grouped results and opens the active result with the keyboard', async () => {
    const user = userEvent.setup()
    const fetchImpl = createSearchFetch()

    renderWithApi(<GlobalSearch />, fetchImpl)

    await user.type(screen.getByRole('combobox', { name: 'Buscar' }), 'casa')

    expect(await screen.findByText('Casa Calabria')).toBeInTheDocument()
    expect(screen.getByText('Pagamento')).toBeInTheDocument()
    expect(screen.getAllByText('Encontrado em Nome')).toHaveLength(2)

    await waitFor(() =>
      expect(
        vi.mocked(fetchImpl).mock.calls.some(([input]) => {
          const request = String(input)
          return (
            request.includes('/v1/search?') &&
            request.includes('query=casa') &&
            request.includes('limit=5') &&
            request.includes('locale=pt-BR')
          )
        }),
      ).toBe(true),
    )

    await user.keyboard('{Enter}')

    expect(screen.getByLabelText('current-route')).toHaveTextContent(
      '/imoveis?propertyId=property-a',
    )
  })

  it('renders min-length and empty permission-safe states', async () => {
    const user = userEvent.setup()
    const fetchImpl = createSearchFetch()

    renderWithApi(<GlobalSearch />, fetchImpl)

    await user.type(screen.getByRole('combobox', { name: 'Buscar' }), 'z')

    expect(screen.getByText('Digite ao menos 2 caracteres')).toBeInTheDocument()
    expect(vi.mocked(fetchImpl)).not.toHaveBeenCalled()

    await user.type(screen.getByRole('combobox', { name: 'Buscar' }), 'z')

    expect(await screen.findByText('Nenhum resultado permitido encontrado')).toBeInTheDocument()
  })
})
