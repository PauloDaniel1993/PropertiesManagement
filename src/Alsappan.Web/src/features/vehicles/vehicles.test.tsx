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
import { VehiclesListPage } from './VehiclesListPage'

const vehicleId = '11111111-1111-1111-1111-111111111111'
const residentId = '22222222-2222-2222-2222-222222222222'
const propertyId = '33333333-3333-3333-3333-333333333333'
const contractId = '44444444-4444-4444-4444-444444444444'

const apiVehicle = {
  archivedAt: undefined,
  auditRoute: `/auditoria?entityType=vehicle&entityId=${vehicleId}`,
  authorizationStatus: { code: 'pending', label: 'Pendente', tone: 'warning' },
  brand: 'Toyota',
  color: 'Prata',
  concurrencyToken: 'vehicle-token',
  contract: {
    description: 'Casa Calabria - Joao da Silva',
    id: contractId,
    name: 'Contrato Casa Calabria',
    route: `/contratos?id=${contractId}`,
  },
  createdAt: '2026-06-27T10:00:00.000Z',
  hasParkingAllocation: true,
  id: vehicleId,
  isArchived: false,
  model: 'Corolla',
  normalizedParkingSpaceIdentifier: 'A12',
  normalizedPlate: 'ABC1234',
  notes: 'Autorizacao aguardando vistoria.',
  parkingAllocationNotes: 'Vaga coberta',
  parkingSpaceIdentifier: 'A-12',
  plate: 'ABC-1234',
  property: {
    description: 'Rua Calabria, 82',
    id: propertyId,
    name: 'Casa Calabria',
    route: `/imoveis?id=${propertyId}`,
  },
  resident: {
    id: residentId,
    name: 'Joao da Silva',
    route: `/moradores?id=${residentId}`,
  },
  timelineRoute: `/timeline?entityType=vehicle&entityId=${vehicleId}`,
  type: { code: 'car', label: 'Carro', tone: 'info' },
  updatedAt: '2026-06-27T10:30:00.000Z',
  year: 2021,
} as const

const apiVehicleListItem = (() => {
  const item = { ...apiVehicle } as Record<string, unknown>
  delete item.archivedAt
  delete item.auditRoute
  delete item.notes
  delete item.timelineRoute

  return item
})()

const apiResidentListItem = {
  emergencyContact: {},
  fullName: 'Joao da Silva',
  id: residentId,
  isArchived: false,
  isSensitiveMasked: false,
  portalStatus: { code: 'not-invited', label: 'Nao convidado' },
  privacyFlags: [],
  status: { code: 'active', label: 'Ativo' },
}

const apiPropertyListItem = {
  address: {
    city: 'Sao Paulo',
    countryCode: 'BR',
    neighborhood: 'Centro',
    number: '82',
    stateCode: 'SP',
    streetLine: 'Rua Calabria',
  },
  garageSpaceCount: 2,
  id: propertyId,
  name: 'Casa Calabria',
  status: { code: 'rented', label: 'Alugado' },
  suggestedRent: { amount: 2500, currency: 'BRL' },
}

const apiContractListItem = {
  adjustmentIndex: 'ipca',
  adjustmentIndexLabel: 'IPCA',
  dueDay: 10,
  id: contractId,
  isArchived: false,
  monthlyRent: { amount: 2500, currency: 'BRL' },
  primaryResident: { id: residentId, isPrimary: true, name: 'Joao da Silva' },
  property: { id: propertyId, name: 'Casa Calabria' },
  residents: [{ id: residentId, isPrimary: true, name: 'Joao da Silva' }],
  startDate: '2026-01-01',
  status: { code: 'active', label: 'Ativo' },
}

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

function paged(items: unknown[]) {
  return {
    hasNextPage: false,
    hasPreviousPage: false,
    items,
    page: 1,
    pageSize: 10,
    totalItems: items.length,
    totalPages: items.length > 0 ? 1 : 0,
  }
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

function buildVehicleSession(): AuthSessionDto {
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
          permissionCodes: [
            'vehicles.read',
            'vehicles.write',
            'vehicles.manage',
            'vehicles.archive',
          ],
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions: ['vehicles.read', 'vehicles.write', 'vehicles.manage', 'vehicles.archive'],
    },
  }
}

function createVehiclesFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/vehicles/type-options') {
      return jsonResponse([
        { code: 'car', label: 'Carro', tone: 'info' },
        { code: 'motorcycle', label: 'Moto', tone: 'warning' },
      ])
    }

    if (url.pathname === '/v1/vehicles/authorization-status-options') {
      return jsonResponse([
        { code: 'pending', label: 'Pendente', tone: 'warning' },
        { code: 'authorized', label: 'Autorizado', tone: 'success' },
        { code: 'denied', label: 'Negado', tone: 'danger' },
        { code: 'archived', label: 'Arquivado', tone: 'archived' },
      ])
    }

    if (url.pathname === `/v1/vehicles/${vehicleId}/authorize`) {
      return jsonResponse({
        ...apiVehicle,
        authorizationStatus: { code: 'authorized', label: 'Autorizado', tone: 'success' },
      })
    }

    if (url.pathname === `/v1/vehicles/${vehicleId}/deny`) {
      return jsonResponse({
        ...apiVehicle,
        authorizationStatus: { code: 'denied', label: 'Negado', tone: 'danger' },
      })
    }

    if (url.pathname === `/v1/vehicles/${vehicleId}/restore`) {
      return jsonResponse(apiVehicle)
    }

    if (url.pathname === `/v1/vehicles/${vehicleId}` && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === `/v1/vehicles/${vehicleId}` && init?.method === 'PUT') {
      return jsonResponse(apiVehicle)
    }

    if (url.pathname === `/v1/vehicles/${vehicleId}`) {
      return jsonResponse(apiVehicle)
    }

    if (url.pathname === '/v1/residents') {
      return jsonResponse(paged([apiResidentListItem]))
    }

    if (url.pathname === '/v1/properties') {
      return jsonResponse(paged([apiPropertyListItem]))
    }

    if (url.pathname === '/v1/contracts') {
      return jsonResponse(paged([apiContractListItem]))
    }

    if (url.pathname === '/v1/vehicles') {
      return jsonResponse(paged([apiVehicleListItem]))
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

describe('vehicles management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads vehicles with filters', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildVehicleSession())
    const fetchImpl = createVehiclesFetch()

    renderWithApi(<VehiclesListPage />, fetchImpl)

    expect(await screen.findByText('ABC-1234')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Veiculo' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Responsavel' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar veiculos' }), 'abc')
    await user.selectOptions(screen.getByLabelText('Tipo'), 'car')
    await user.selectOptions(screen.getByLabelText('Autorizacao'), 'pending')
    await user.selectOptions(screen.getByLabelText('Vaga'), 'true')
    await user.type(screen.getByLabelText('Placa'), 'abc-1234')
    await user.type(screen.getByLabelText('ID do morador'), residentId)
    await user.type(screen.getByLabelText('ID do imovel'), propertyId)
    await user.type(screen.getByLabelText('ID do contrato'), contractId)
    await user.type(screen.getByLabelText('Identificador da vaga'), 'A-12')
    await user.click(screen.getByLabelText('Incluir arquivados'))

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/vehicles?') &&
              String(input).includes('search=abc') &&
              String(input).includes('type=car') &&
              String(input).includes('authorizationStatus=pending') &&
              String(input).includes('hasParkingAllocation=true') &&
              String(input).includes('plate=abc-1234') &&
              String(input).includes(`residentId=${residentId}`) &&
              String(input).includes(`propertyId=${propertyId}`) &&
              String(input).includes(`contractId=${contractId}`) &&
              String(input).includes('parkingSpaceIdentifier=A-12') &&
              String(input).includes('includeArchived=true'),
          ),
      ).toBe(true),
    )
  })

  it('runs vehicle lifecycle actions from the list', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildVehicleSession())
    const fetchImpl = createVehiclesFetch()

    renderWithApi(<VehiclesListPage />, fetchImpl)

    expect(await screen.findByText('ABC-1234')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Autorizar ABC-1234 - Casa Calabria'))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/vehicles/${vehicleId}/authorize?locale=pt-BR`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )

    await user.click(screen.getByLabelText('Negar ABC-1234 - Casa Calabria'))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/vehicles/${vehicleId}/deny?locale=pt-BR`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )

    await user.click(screen.getByLabelText('Arquivar ABC-1234 - Casa Calabria'))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/vehicles/${vehicleId}`,
        expect.objectContaining({ method: 'DELETE' }),
      ),
    )
  })

  it('opens details and preserves detail-only fields when editing', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildVehicleSession())
    const fetchImpl = createVehiclesFetch()

    renderWithApi(<VehiclesListPage />, fetchImpl)

    expect(await screen.findByText('ABC-1234')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Ver detalhes ABC-1234 - Casa Calabria'))

    expect(await screen.findByText('Resumo do veiculo')).toBeInTheDocument()
    expect(screen.getAllByText('Timeline').length).toBeGreaterThan(0)
    expect(screen.getByText('Vaga coberta')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Editar veiculo' }))
    await user.click(await screen.findByRole('button', { name: 'Salvar alteracoes' }))

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input, init]) =>
              String(input) === `https://api.alsappan.test/v1/vehicles/${vehicleId}?locale=pt-BR` &&
              init?.method === 'PUT' &&
              String(init.body).includes('"plate":"ABC-1234"') &&
              String(init.body).includes(`"residentId":"${residentId}"`) &&
              String(init.body).includes(`"propertyId":"${propertyId}"`) &&
              String(init.body).includes(`"contractId":"${contractId}"`) &&
              String(init.body).includes('"parkingSpaceIdentifier":"A-12"') &&
              String(init.body).includes('"concurrencyToken":"vehicle-token"'),
          ),
      ).toBe(true),
    )
  })
})
