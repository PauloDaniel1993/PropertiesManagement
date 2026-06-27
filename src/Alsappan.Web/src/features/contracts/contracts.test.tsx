import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import type { AuthSessionDto } from '../../lib/api/identity'
import type { ContractDetail } from '../../lib/api/leaseContracts'
import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
  useFiltersStore,
} from '../../stores'
import { applyAuthSession } from '../identity/session'
import { ContractForm } from './ContractForm'
import { ContractsListPage } from './ContractsListPage'

const propertyId = '33333333-3333-3333-3333-333333333333'
const residentId = '55555555-5555-5555-5555-555555555555'
const contractId = '44444444-4444-4444-4444-444444444444'

const apiProperty = {
  address: {
    city: 'Sao Paulo',
    countryCode: 'BR',
    neighborhood: 'Vila Fazzione',
    number: '82',
    stateCode: 'SP',
    streetLine: 'Rua Calabria',
  },
  garageSpaceCount: 1,
  id: propertyId,
  isArchived: false,
  location: 'Rua Calabria, 82 - Vila Fazzione, Sao Paulo/SP',
  name: 'Casa Calabria',
  status: { code: 'available', label: 'Disponivel', tone: 'success' },
  suggestedRent: { amount: 2500, currency: 'BRL' },
  type: 'house',
  typeLabel: 'Casa',
} as const

const apiResident = {
  emergencyContact: {},
  fullName: 'Joao da Silva',
  id: residentId,
  isArchived: false,
  isSensitiveMasked: false,
  portalStatus: { code: 'not-invited', label: 'Nao convidado' },
  privacyFlags: [],
  status: { code: 'active', label: 'Ativo', tone: 'success' },
} as const

const apiContract = {
  adjustmentIndex: 'ipca',
  adjustmentIndexLabel: 'IPCA',
  adjustmentIntervalMonths: 12,
  createdAt: '2026-06-27T10:00:00.000Z',
  concurrencyToken: 'contract-token',
  depositAmount: { amount: 2500, currency: 'BRL' },
  documents: [
    {
      category: 'contract',
      count: 0,
      label: 'Contrato assinado',
      route: `/documentos?contractId=${contractId}&category=contract`,
    },
  ],
  dueDay: 10,
  endDate: '2027-06-30',
  generatePaymentsAutomatically: true,
  id: contractId,
  isArchived: false,
  monthlyRent: { amount: 2500, currency: 'BRL' },
  notes: 'Contrato residencial',
  primaryResident: { id: residentId, isPrimary: true, name: 'Joao da Silva' },
  property: {
    id: propertyId,
    location: 'Rua Calabria, 82 - Vila Fazzione, Sao Paulo/SP',
    name: 'Casa Calabria',
  },
  relationships: [
    {
      count: 0,
      label: 'Pagamentos',
      module: 'payments',
      route: `/pagamentos?contractId=${contractId}`,
    },
    {
      count: 0,
      label: 'Timeline',
      module: 'timeline',
      route: `/timeline?entityType=contract&entityId=${contractId}`,
    },
    {
      count: 0,
      label: 'Auditoria',
      module: 'audit',
      route: `/auditoria?entityType=contract&entityId=${contractId}`,
    },
  ],
  residents: [{ id: residentId, isPrimary: true, name: 'Joao da Silva' }],
  startDate: '2026-07-01',
  status: { code: 'draft', label: 'Rascunho', tone: 'neutral' },
  updatedAt: '2026-06-27T10:30:00.000Z',
} as const

const contractDetail = apiContract as unknown as ContractDetail

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

function buildContractSession(): AuthSessionDto {
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
            'contracts.read',
            'contracts.write',
            'contracts.manage',
            'contracts.archive',
            'properties.read',
            'residents.read',
          ],
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions: [
        'contracts.read',
        'contracts.write',
        'contracts.manage',
        'contracts.archive',
        'properties.read',
        'residents.read',
      ],
    },
  }
}

function createContractsFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/properties') {
      return jsonResponse(paged([apiProperty]))
    }

    if (url.pathname === '/v1/residents') {
      return jsonResponse(paged([apiResident]))
    }

    if (url.pathname === `/v1/contracts/${contractId}/activate`) {
      return jsonResponse({
        ...apiContract,
        status: { code: 'active', label: 'Ativo', tone: 'success' },
      })
    }

    if (url.pathname === `/v1/contracts/${contractId}` && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === `/v1/contracts/${contractId}`) {
      return jsonResponse(apiContract)
    }

    if (url.pathname === '/v1/contracts' && init?.method === 'POST') {
      return jsonResponse({
        ...apiContract,
        status: { code: 'active', label: 'Ativo', tone: 'success' },
      })
    }

    if (url.pathname === '/v1/contracts') {
      return jsonResponse(paged([apiContract]))
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

describe('contracts management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('validates and submits contract form fields with localized messages', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const fetchImpl = createContractsFetch()

    renderWithApi(<ContractForm mode="create" onSubmit={onSubmit} />, fetchImpl)

    await screen.findByRole('option', { name: 'Casa Calabria - Sao Paulo' })
    await user.click(screen.getByRole('button', { name: 'Criar contrato' }))

    expect(screen.getByText('Selecione um imovel.')).toBeInTheDocument()
    expect(screen.getByText('Informe a data de inicio.')).toBeInTheDocument()
    expect(screen.getByText('Informe um aluguel mensal valido.')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText(/Imovel/), propertyId)
    await user.selectOptions(screen.getByLabelText(/Responsavel principal/), residentId)
    await user.click(screen.getByLabelText(/Joao da Silva/))
    await user.type(screen.getByLabelText(/Inicio/), '2026-07-01')
    await user.type(screen.getByLabelText(/Fim/), '2027-06-30')
    await user.type(screen.getByLabelText(/Aluguel mensal/), '2500')
    await user.clear(screen.getByLabelText(/Dia de vencimento/))
    await user.type(screen.getByLabelText(/Dia de vencimento/), '10')
    await user.type(screen.getByLabelText(/Caucao/), '2500')
    await user.type(screen.getByLabelText(/^Observacoes$/), 'Contrato residencial')
    await user.click(screen.getByLabelText(/Criar como contrato ativo/))
    await user.click(screen.getByRole('button', { name: 'Criar contrato' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          lifecycleAction: 'activate',
          monthlyRent: 2500,
          primaryResidentId: residentId,
          propertyId,
          residentIds: [residentId],
        }),
      ),
    )
  })

  it('submits concurrency token and disables property changes when editing', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const fetchImpl = createContractsFetch()

    renderWithApi(
      <ContractForm initialValue={contractDetail} mode="edit" onSubmit={onSubmit} />,
      fetchImpl,
    )

    await screen.findByRole('option', { name: 'Casa Calabria - Sao Paulo' })
    expect(screen.getByLabelText(/Imovel/)).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Salvar alteracoes' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          concurrencyToken: 'contract-token',
          propertyId,
        }),
      ),
    )
  })

  it('loads contracts with filters and executes activation action', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildContractSession())
    const fetchImpl = createContractsFetch()

    renderWithApi(<ContractsListPage />, fetchImpl)

    expect((await screen.findAllByText('Casa Calabria')).length).toBeGreaterThan(0)
    expect(screen.getByRole('columnheader', { name: 'Imovel' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Moradores' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Status' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar contratos' }), 'calabria')
    await user.selectOptions(screen.getByLabelText('Status'), 'draft')

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/contracts?') &&
              String(input).includes('search=calabria') &&
              String(input).includes('status=draft'),
          ),
      ).toBe(true),
    )

    await user.click(screen.getByLabelText('Ativar Casa Calabria'))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/contracts/${contractId}/activate`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )
  })

  it('includes archived rows when filtering by archived status', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildContractSession())
    const fetchImpl = createContractsFetch()

    renderWithApi(<ContractsListPage />, fetchImpl)

    expect((await screen.findAllByText('Casa Calabria')).length).toBeGreaterThan(0)

    await user.selectOptions(screen.getByLabelText('Status'), 'archived')

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/contracts?') &&
              String(input).includes('status=archived') &&
              String(input).includes('includeArchived=true'),
          ),
      ).toBe(true),
    )
  })

  it('opens contract details with relationship and document panels', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildContractSession())
    const fetchImpl = createContractsFetch()

    renderWithApi(<ContractsListPage />, fetchImpl)

    expect((await screen.findAllByText('Casa Calabria')).length).toBeGreaterThan(0)

    await user.click(screen.getByLabelText('Ver detalhes Casa Calabria'))

    expect(await screen.findByText('Resumo do contrato')).toBeInTheDocument()
    expect(screen.getByText('Vinculos de documentos')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Contrato assinado' })).toHaveAttribute(
      'href',
      `/documentos?contractId=${contractId}&category=contract`,
    )
    expect(screen.getByRole('link', { name: 'Timeline' })).toHaveAttribute(
      'href',
      `/timeline?entityType=contract&entityId=${contractId}`,
    )
    expect(screen.getByRole('link', { name: 'Auditoria' })).toHaveAttribute(
      'href',
      `/auditoria?entityType=contract&entityId=${contractId}`,
    )
  })

  it('applies dashboard and search route state to filters and details', async () => {
    applyAuthSession(buildContractSession())
    const fetchImpl = createContractsFetch()

    renderWithApi(<ContractsListPage />, fetchImpl, [
      `/contratos?endingSoon=true&contractId=${contractId}`,
    ])

    expect(await screen.findByText('Resumo do contrato')).toBeInTheDocument()

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/contracts?') &&
              String(input).includes('endingSoonOnly=true'),
          ),
      ).toBe(true),
    )

    expect(fetchImpl).toHaveBeenCalledWith(
      `https://api.alsappan.test/v1/contracts/${contractId}`,
      expect.objectContaining({ method: 'GET' }),
    )
  })
})
