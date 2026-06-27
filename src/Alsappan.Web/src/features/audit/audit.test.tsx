import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import { useAppPreferencesStore, useFiltersStore } from '../../stores'
import { AuditListPage } from './AuditListPage'

const auditEntry = {
  action: 'property.created',
  actionLabel: 'Imovel criado',
  actorDisplayName: 'Paulo',
  actorKind: 'user',
  actorUserId: 'user-a',
  category: {
    code: 'Mutation',
    label: 'Dados',
    tone: 'info',
  },
  changedFields: {
    name: 'Calabria casa1',
  },
  context: {
    module: 'properties',
  },
  correlationId: 'trace-a',
  id: 'audit-a',
  occurredAt: '2026-06-27T10:00:00.000Z',
  targetDisplayName: 'Calabria casa1',
  targetEntityId: 'property-a',
  targetEntityType: 'property',
} as const

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

function renderWithApi(ui: ReactNode, fetchImpl: typeof fetch, initialEntries = ['/']) {
  const apiClient = new ApiClient({
    baseUrl: 'https://api.alsappan.test',
    fetchImpl,
  })

  return render(
    <QueryClientProvider client={createQueryClient()}>
      <ApiClientContext.Provider value={apiClient}>
        <MemoryRouter initialEntries={initialEntries}>{ui}</MemoryRouter>
      </ApiClientContext.Provider>
    </QueryClientProvider>,
  )
}

function createAuditFetch() {
  return vi.fn((input: RequestInfo | URL) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/audit/category-options') {
      return jsonResponse([
        { code: 'Mutation', label: 'Dados', tone: 'info' },
        { code: 'Security', label: 'Seguranca', tone: 'danger' },
        { code: 'System', label: 'Sistema', tone: 'neutral' },
      ])
    }

    if (url.pathname === '/v1/audit') {
      return jsonResponse({
        hasNextPage: false,
        hasPreviousPage: false,
        items: [auditEntry],
        page: 1,
        pageSize: 10,
        totalItems: 1,
        totalPages: 1,
      })
    }

    return jsonResponse({ title: 'Not found' }, 404)
  }) as unknown as typeof fetch
}

describe('audit UI', () => {
  beforeEach(() => {
    localStorage.clear()
    useAppPreferencesStore.getState().resetPreferences()
    useFiltersStore.getState().resetFilters()
  })

  it('loads audit entries and sends filters to the API', async () => {
    const user = userEvent.setup()
    const fetchImpl = createAuditFetch()

    renderWithApi(<AuditListPage />, fetchImpl)

    expect(await screen.findByText('Imovel criado')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Acao' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Categoria' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar auditoria' }), 'property')
    await user.selectOptions(screen.getByLabelText('Categoria'), 'Security')
    await user.type(screen.getByLabelText('Ator'), 'Paulo')

    await waitFor(() =>
      expect(
        vi.mocked(fetchImpl).mock.calls.some(([input]) => {
          const url = String(input)

          return (
            url.includes('/v1/audit?') &&
            url.includes('search=property') &&
            url.includes('category=Security') &&
            url.includes('actor=Paulo')
          )
        }),
      ).toBe(true),
    )

    await user.click(screen.getByLabelText('Ver Imovel criado'))

    expect(screen.getByText('Campos alterados')).toBeInTheDocument()
    expect(screen.getAllByText('Calabria casa1')).not.toHaveLength(0)
    expect(screen.getByText('trace-a')).toBeInTheDocument()
  })

  it('renders English copy', async () => {
    useAppPreferencesStore.getState().setLocale('en-US')
    const fetchImpl = createAuditFetch()

    renderWithApi(<AuditListPage />, fetchImpl)

    expect(await screen.findByText('Audit')).toBeInTheDocument()
    expect(screen.getByRole('searchbox', { name: 'Search audit' })).toBeInTheDocument()
  })

  it('applies entity filters from detail page links', async () => {
    const fetchImpl = createAuditFetch()

    renderWithApi(<AuditListPage />, fetchImpl, ['/auditoria?propertyId=property-a'])

    expect(await screen.findByText('Imovel criado')).toBeInTheDocument()

    await waitFor(() =>
      expect(
        vi.mocked(fetchImpl).mock.calls.some(([input]) => {
          const url = String(input)

          return (
            url.includes('/v1/audit?') &&
            url.includes('entityType=property') &&
            url.includes('entityId=property-a')
          )
        }),
      ).toBe(true),
    )
  })
})
