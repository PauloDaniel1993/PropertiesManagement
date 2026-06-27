import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import type { AuthSessionDto } from '../../lib/api/identity'
import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
  useFiltersStore,
} from '../../stores'
import { applyAuthSession } from '../identity/session'
import { PropertiesListPage } from './PropertiesListPage'
import { PropertyForm } from './PropertyForm'

const propertyListItem = {
  address: {
    city: 'Sao Paulo',
    country: 'BR',
    neighborhood: 'Jardim Paulista',
    number: '101',
    postalCode: '01310-100',
    state: 'SP',
    street: 'Rua das Flores',
  },
  currencyCode: 'BRL',
  garageSpaces: 1,
  hasGarage: true,
  id: 'property-a',
  name: 'Apartamento Jardim',
  status: 'available',
  suggestedRent: 2500,
  type: 'apartment',
  updatedAt: '2026-06-27T10:00:00.000Z',
} as const

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      headers: { 'Content-Type': 'application/json' },
      status,
    }),
  )
}

function emptyResponse(status = 204) {
  return Promise.resolve(new Response(null, { status }))
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

function buildPropertySession(): AuthSessionDto {
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
          displayName: 'Organizacao A',
          id: 'org-a',
          locale: 'pt-BR',
          name: 'Organizacao A',
          permissionCodes: ['properties.read', 'properties.write'],
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions: ['properties.read', 'properties.write'],
    },
  }
}

function propertyListResponse() {
  return {
    hasNextPage: false,
    hasPreviousPage: false,
    items: [propertyListItem],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  }
}

function createPropertiesFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/properties/property-a' && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === '/v1/properties/property-a') {
      return jsonResponse({
        ...propertyListItem,
        createdAt: '2026-06-01T10:00:00.000Z',
        notes: 'Sol da manha',
      })
    }

    if (url.pathname === '/v1/properties' && init?.method !== 'POST') {
      return jsonResponse(propertyListResponse())
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

describe('properties management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('validates and submits property form fields with localized messages', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()

    render(<PropertyForm mode="create" onSubmit={onSubmit} />)

    await user.click(screen.getByRole('button', { name: 'Criar imovel' }))

    expect(screen.getByText('Informe o imovel.')).toBeInTheDocument()
    expect(screen.getByText('Informe o logradouro.')).toBeInTheDocument()
    expect(screen.getByText('Informe um aluguel sugerido valido.')).toBeInTheDocument()

    await user.type(screen.getByLabelText(/Imovel/), 'Apartamento Jardim')
    await user.type(screen.getByLabelText(/Logradouro/), 'Rua das Flores')
    await user.type(screen.getByLabelText(/Numero/), '101')
    await user.type(screen.getByLabelText(/Bairro/), 'Jardim Paulista')
    await user.type(screen.getByLabelText(/Cidade/), 'Sao Paulo')
    await user.type(screen.getByLabelText(/UF/), 'SP')
    await user.type(screen.getByLabelText(/Aluguel sugerido/), '2500')
    await user.click(screen.getByLabelText('Possui garagem'))
    await user.type(screen.getByLabelText(/Vagas de garagem/), '1')
    await user.type(screen.getByLabelText(/Observacoes/), 'Sol da manha')
    await user.click(screen.getByRole('button', { name: 'Criar imovel' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        address: {
          city: 'Sao Paulo',
          complement: undefined,
          country: 'BR',
          neighborhood: 'Jardim Paulista',
          number: '101',
          postalCode: undefined,
          state: 'SP',
          street: 'Rua das Flores',
        },
        garageSpaces: 1,
        hasGarage: true,
        name: 'Apartamento Jardim',
        notes: 'Sol da manha',
        status: 'available',
        suggestedRent: 2500,
        type: 'apartment',
      }),
    )
  })

  it('loads properties with filters and executes row archive action', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildPropertySession())
    const fetchImpl = createPropertiesFetch()

    renderWithApi(<PropertiesListPage />, fetchImpl)

    expect(await screen.findByText('Apartamento Jardim')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Imovel' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Localizacao' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Status' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Aluguel sugerido' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Garagem' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Acoes' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar imoveis' }), 'jardim')
    await user.selectOptions(screen.getByLabelText('Status'), 'rented')

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/properties?') &&
              String(input).includes('search=jardim') &&
              String(input).includes('status=rented'),
          ),
      ).toBe(true),
    )

    await user.click(screen.getByLabelText('Arquivar Apartamento Jardim'))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        'https://api.alsappan.test/v1/properties/property-a',
        expect.objectContaining({ method: 'DELETE' }),
      ),
    )
  })

  it('opens property details with relationship placeholders and audit links', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildPropertySession())
    const fetchImpl = createPropertiesFetch()

    renderWithApi(<PropertiesListPage />, fetchImpl)

    expect(await screen.findByText('Apartamento Jardim')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Ver detalhes Apartamento Jardim'))

    expect(await screen.findByText('Resumo do imovel')).toBeInTheDocument()
    expect(screen.getByText('Contratos')).toBeInTheDocument()
    expect(screen.getByText('Moradores')).toBeInTheDocument()
    expect(screen.getByText('Pagamentos')).toBeInTheDocument()
    expect(screen.getByText('Contas de consumo')).toBeInTheDocument()
    expect(screen.getByText('Documentos')).toBeInTheDocument()
    expect(screen.getByText('Pets')).toBeInTheDocument()
    expect(screen.getByText('Veiculos')).toBeInTheDocument()
    expect(screen.getByText('Ocorrencias')).toBeInTheDocument()
    expect(screen.getByText('Vistorias')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Abrir timeline do imovel' })).toHaveAttribute(
      'href',
      '/timeline?propertyId=property-a',
    )
    expect(screen.getByRole('link', { name: 'Abrir auditoria do imovel' })).toHaveAttribute(
      'href',
      '/auditoria?propertyId=property-a',
    )
  })

  it('applies dashboard and search route state to filters and details', async () => {
    applyAuthSession(buildPropertySession())
    const fetchImpl = createPropertiesFetch()

    renderWithApi(<PropertiesListPage />, fetchImpl, [
      '/imoveis?status=rented&propertyId=property-a',
    ])

    expect(await screen.findByText('Resumo do imovel')).toBeInTheDocument()

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/properties?') && String(input).includes('status=rented'),
          ),
      ).toBe(true),
    )

    expect(fetchImpl).toHaveBeenCalledWith(
      'https://api.alsappan.test/v1/properties/property-a',
      expect.objectContaining({ method: 'GET' }),
    )
  })
})
