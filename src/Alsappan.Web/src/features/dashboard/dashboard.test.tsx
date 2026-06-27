import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
  useFiltersStore,
} from '../../stores'
import { DashboardPage } from './DashboardPage'

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

function renderWithApi(ui: ReactNode, fetchImpl: typeof fetch) {
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

function dashboardOverview() {
  return {
    emptyState: null,
    generatedAt: '2026-06-27T12:00:00.000Z',
    metrics: [
      {
        description: 'Imoveis alugados sobre o total ativo.',
        displayValue: '75%',
        hiddenReason: null,
        isVisible: true,
        key: 'occupancy',
        label: 'Ocupacao',
        metadata: {
          rentedProperties: '3',
          totalProperties: '4',
        },
        route: '/imoveis?status=rented',
        tone: 'success',
        unit: '%',
        value: 75,
      },
      {
        description: 'Cobrancas pendentes com vencimento ultrapassado.',
        displayValue: '2',
        hiddenReason: null,
        isVisible: true,
        key: 'overduePayments',
        label: 'Pagamentos vencidos',
        metadata: {
          currency: 'BRL',
          totalBalance: '1500',
        },
        route: '/pagamentos?overdueOnly=true',
        tone: 'danger',
        value: 2,
      },
      {
        description: 'Indicador oculto por permissao.',
        hiddenReason: 'Sem permissao para visualizar este indicador.',
        isVisible: false,
        key: 'contractExpirations',
        label: 'Contratos a vencer',
        metadata: {},
        route: '/contratos?endingSoon=true',
        tone: 'neutral',
      },
    ],
    recentActivity: {
      description: 'Ultimos eventos operacionais.',
      hiddenReason: null,
      isVisible: true,
      items: [
        {
          entityType: 'property',
          entityTypeLabel: 'Imovel',
          eventType: 'property.created',
          eventTypeLabel: 'Imovel criado',
          id: 'activity-a',
          occurredAt: '2026-06-27T11:45:00.000Z',
          route: '/imoveis?propertyId=property-a',
          subjectDisplayName: 'Casa A',
          summary: 'Imovel criado: Casa A',
        },
      ],
      label: 'Atividade recente',
      route: '/timeline',
    },
  }
}

function emptyDashboardOverview() {
  return {
    ...dashboardOverview(),
    emptyState: {
      actionLabel: 'Novo imovel',
      description: 'Cadastre o primeiro imovel para iniciar os indicadores operacionais.',
      route: '/imoveis',
      title: 'Nenhum imovel cadastrado',
    },
    metrics: [],
    recentActivity: {
      description: 'Ultimos eventos operacionais.',
      hiddenReason: null,
      isVisible: true,
      items: [],
      label: 'Atividade recente',
      route: '/timeline',
    },
  }
}

function createDashboardFetch(response: unknown) {
  return vi.fn((input: RequestInfo | URL) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/dashboard') {
      return jsonResponse(response)
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

describe('dashboard page', () => {
  beforeEach(() => {
    resetStores()
  })

  it('renders metrics with permission masking and deep links', async () => {
    const fetchImpl = createDashboardFetch(dashboardOverview())

    renderWithApi(<DashboardPage />, fetchImpl)

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument()
    expect(await screen.findByText('75%')).toBeInTheDocument()
    expect(screen.getByText('2 - R$ 1.500,00')).toBeInTheDocument()
    expect(screen.getByText('Sem permissao para visualizar este indicador.')).toBeInTheDocument()
    expect(screen.getByText('Ocupacao').closest('a')).toHaveAttribute(
      'href',
      '/imoveis?status=rented',
    )
    expect(screen.getByText('Imovel criado: Casa A').closest('a')).toHaveAttribute(
      'href',
      '/imoveis?propertyId=property-a',
    )

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(([input]) => String(input).includes('/v1/dashboard?locale=pt-BR')),
      ).toBe(true),
    )
  })

  it('renders the API-provided empty state', async () => {
    renderWithApi(<DashboardPage />, createDashboardFetch(emptyDashboardOverview()))

    expect(await screen.findByText('Nenhum imovel cadastrado')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Novo imovel' })).toBeInTheDocument()
    expect(screen.getByText('Nenhuma atividade recente')).toBeInTheDocument()
  })
})
