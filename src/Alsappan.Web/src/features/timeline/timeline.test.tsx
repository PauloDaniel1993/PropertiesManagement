import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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
import { EntityTimelinePanel } from './EntityTimelinePanel'
import { TimelinePage } from './TimelinePage'

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

function renderWithApi(ui: ReactNode, fetchImpl: typeof fetch, route = '/timeline') {
  const apiClient = new ApiClient({
    baseUrl: 'https://api.alsappan.test',
    fetchImpl,
  })

  return render(
    <QueryClientProvider client={createQueryClient()}>
      <ApiClientContext.Provider value={apiClient}>
        <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
      </ApiClientContext.Provider>
    </QueryClientProvider>,
  )
}

function timelineEntry(eventType = 'property.created') {
  return {
    actor: {
      displayName: 'Ana Admin',
      kind: 'user',
      kindLabel: 'Usuario',
      userId: 'user-a',
    },
    data: {},
    display: {
      eventLabel: eventType === 'property.updated' ? 'Imovel atualizado' : 'Imovel criado',
      summary:
        eventType === 'property.updated' ? 'Imovel atualizado: Casa A' : 'Imovel criado: Casa A',
    },
    eventId: 'event-a',
    eventType,
    eventTypeLabel: eventType === 'property.updated' ? 'Imovel atualizado' : 'Imovel criado',
    id: 'timeline-a',
    moduleName: 'properties',
    occurredAt: '2026-06-27T10:00:00.000Z',
    relatedEntities: [
      {
        displayName: 'Joao',
        entityId: 'resident-a',
        entityType: 'resident',
        entityTypeLabel: 'Morador',
        route: '/moradores?residentId=resident-a',
      },
    ],
    route: '/imoveis?propertyId=property-a',
    subject: {
      displayName: 'Casa A',
      entityId: 'property-a',
      entityType: 'property',
      entityTypeLabel: 'Imovel',
      route: '/imoveis?propertyId=property-a',
    },
  }
}

function timelinePage(eventType = 'property.created') {
  return {
    hasNextPage: false,
    hasPreviousPage: false,
    items: [timelineEntry(eventType)],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  }
}

function createTimelineFetch() {
  return vi.fn((input: RequestInfo | URL) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/timeline/entities/property/property-a') {
      return jsonResponse(timelinePage(url.searchParams.get('eventType') ?? 'property.updated'))
    }

    if (url.pathname === '/v1/timeline') {
      return jsonResponse(timelinePage(url.searchParams.get('eventType') ?? 'property.created'))
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

describe('timeline UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads grouped global timeline entries and applies filters', async () => {
    const user = userEvent.setup()
    const fetchImpl = createTimelineFetch()

    renderWithApi(<TimelinePage />, fetchImpl)

    expect(await screen.findByText('Imovel criado: Casa A')).toBeInTheDocument()
    expect(screen.getByText('Relacionados: Joao')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Abrir registro de origem' })).toHaveAttribute(
      'href',
      '/imoveis?propertyId=property-a',
    )

    await user.selectOptions(screen.getByLabelText('Entidade'), 'resident')
    await user.type(screen.getByLabelText('Tipo de evento'), 'resident.created')
    await user.type(screen.getByLabelText('De'), '2026-06-01')

    await waitFor(() =>
      expect(
        vi.mocked(fetchImpl).mock.calls.some(([input]) => {
          const request = String(input)
          return (
            request.includes('/v1/timeline?') &&
            request.includes('entityType=resident') &&
            request.includes('eventType=resident.created') &&
            request.includes('from=2026-06-01') &&
            request.includes('locale=pt-BR')
          )
        }),
      ).toBe(true),
    )
  })

  it('uses entity timeline endpoint when opened from detail links', async () => {
    const fetchImpl = createTimelineFetch()

    renderWithApi(<TimelinePage />, fetchImpl, '/timeline?propertyId=property-a')

    expect(await screen.findByRole('heading', { name: 'Timeline de Imovel' })).toBeInTheDocument()

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(([input]) =>
            String(input).includes('/v1/timeline/entities/property/property-a'),
          ),
      ).toBe(true),
    )
  })

  it('renders reusable entity timeline panel', async () => {
    const fetchImpl = createTimelineFetch()

    renderWithApi(<EntityTimelinePanel entityId="property-a" entityType="property" />, fetchImpl)

    expect(await screen.findByText('Imovel atualizado: Casa A')).toBeInTheDocument()
    expect(
      vi
        .mocked(fetchImpl)
        .mock.calls.some(
          ([input]) =>
            String(input).includes('/v1/timeline/entities/property/property-a') &&
            String(input).includes('pageSize=5') &&
            String(input).includes('locale=pt-BR'),
        ),
    ).toBe(true)
  })
})
