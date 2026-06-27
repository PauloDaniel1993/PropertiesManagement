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
import { PetsListPage } from './PetsListPage'

const petId = '11111111-1111-1111-1111-111111111111'
const vaccinationDocumentId = '22222222-2222-2222-2222-222222222222'
const authorizationDocumentId = '33333333-3333-3333-3333-333333333333'
const propertyId = '44444444-4444-4444-4444-444444444444'
const contractId = '55555555-5555-5555-5555-555555555555'
const residentId = '66666666-6666-6666-6666-666666666666'

const apiPet = {
  archivedAt: undefined,
  auditRoute: `/auditoria?entityType=pet&entityId=${petId}`,
  authorizationFormDocuments: [
    {
      documentId: authorizationDocumentId,
      kind: 'authorization-form',
      kindLabel: 'Formulario de autorizacao',
      label: 'Formulario Luna',
      route: `/documentos?entityType=pet&entityId=${petId}`,
    },
  ],
  authorizationHistory: [
    {
      notes: 'Aguardando formulario',
      occurredAt: '2026-06-27T10:00:00.000Z',
      status: 'pending',
      statusLabel: 'Pendente',
    },
  ],
  authorizationNotes: 'Aguardando formulario',
  authorizationStatus: { code: 'pending', label: 'Pendente', tone: 'warning' },
  breed: 'SRD',
  concurrencyToken: 'pet-token',
  contract: {
    description: 'Casa Calabria - Joao da Silva',
    id: contractId,
    name: 'Contrato Casa Calabria',
    route: `/contratos?id=${contractId}`,
  },
  createdAt: '2026-06-27T10:00:00.000Z',
  id: petId,
  isArchived: false,
  name: 'Luna',
  notes: 'Docil',
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
  species: { code: 'cat', label: 'Gato', tone: 'info' },
  timelineRoute: `/timeline?entityType=pet&entityId=${petId}`,
  updatedAt: '2026-06-27T10:30:00.000Z',
  vaccinationRecordDocuments: [
    {
      documentId: vaccinationDocumentId,
      kind: 'vaccination-record',
      kindLabel: 'Carteira de vacinacao',
      label: 'Carteira Luna',
      route: `/documentos?entityType=pet&entityId=${petId}`,
    },
  ],
} as const

const apiPetListItem = (() => {
  const item = { ...apiPet } as Record<string, unknown>
  delete item.archivedAt
  delete item.auditRoute
  delete item.authorizationHistory
  delete item.notes
  delete item.timelineRoute

  return item
})()

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

function buildPetSession(): AuthSessionDto {
  const permissions = [
    'pets.read',
    'pets.write',
    'pets.manage',
    'pets.archive',
    'documents.read',
    'timeline.read',
    'audit.read',
  ]

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
          permissionCodes: permissions,
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions,
    },
  }
}

function createPetsFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/pets/options') {
      return jsonResponse({
        authorizationStatuses: [
          { label: 'Pendente', value: 'pending' },
          { label: 'Autorizado', value: 'authorized' },
          { label: 'Negado', value: 'denied' },
          { label: 'Arquivado', value: 'archived' },
        ],
        documentKinds: [
          { label: 'Carteira de vacinacao', value: 'vaccination-record' },
          { label: 'Formulario de autorizacao', value: 'authorization-form' },
        ],
        species: [
          { label: 'Gato', value: 'cat' },
          { label: 'Cachorro', value: 'dog' },
        ],
      })
    }

    if (url.pathname === `/v1/pets/${petId}/authorize`) {
      return jsonResponse({
        ...apiPet,
        authorizationStatus: { code: 'authorized', label: 'Autorizado', tone: 'success' },
      })
    }

    if (url.pathname === `/v1/pets/${petId}/deny`) {
      return jsonResponse({
        ...apiPet,
        authorizationStatus: { code: 'denied', label: 'Negado', tone: 'danger' },
      })
    }

    if (url.pathname === `/v1/pets/${petId}` && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === `/v1/pets/${petId}` && init?.method === 'PUT') {
      return jsonResponse(apiPet)
    }

    if (url.pathname === `/v1/pets/${petId}`) {
      return jsonResponse(apiPet)
    }

    if (url.pathname === '/v1/pets') {
      return jsonResponse(paged([apiPetListItem]))
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

describe('pets management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads pets with filters', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildPetSession())
    const fetchImpl = createPetsFetch()

    renderWithApi(<PetsListPage />, fetchImpl)

    expect(await screen.findByText('Luna')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Pet' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Responsavel' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar pets' }), 'luna')
    await user.selectOptions(screen.getByLabelText('Especie'), 'cat')
    await user.selectOptions(screen.getByLabelText('Autorizacao'), 'pending')
    await user.type(screen.getByLabelText('ID do morador'), residentId)
    await user.type(screen.getByLabelText('ID do imovel'), propertyId)
    await user.type(screen.getByLabelText('ID do contrato'), contractId)
    await user.click(screen.getByLabelText('Somente contrato ativo'))

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/pets?') &&
              String(input).includes('search=luna') &&
              String(input).includes('species=cat') &&
              String(input).includes('authorizationStatus=pending') &&
              String(input).includes(`residentId=${residentId}`) &&
              String(input).includes(`propertyId=${propertyId}`) &&
              String(input).includes(`contractId=${contractId}`) &&
              String(input).includes('activeContractOnly=true'),
          ),
      ).toBe(true),
    )
  })

  it('authorizes a pet', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildPetSession())
    const fetchImpl = createPetsFetch()

    renderWithApi(<PetsListPage />, fetchImpl)

    expect(await screen.findByText('Luna')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Autorizar Luna - Joao da Silva'))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/pets/${petId}/authorize?locale=pt-BR`,
        expect.objectContaining({
          body: '{}',
          method: 'POST',
        }),
      ),
    )
  })

  it('opens details and preserves document fields when editing from the list', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildPetSession())
    const fetchImpl = createPetsFetch()

    renderWithApi(<PetsListPage />, fetchImpl)

    expect(await screen.findByText('Luna')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Ver detalhes Luna - Joao da Silva'))

    expect(await screen.findByText('Resumo do pet')).toBeInTheDocument()
    expect(screen.getByText('Carteira Luna')).toBeInTheDocument()
    expect(screen.getByText('Formulario Luna')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Voltar para pets' }))
    await user.click(screen.getByLabelText('Editar Luna - Joao da Silva'))

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input, init]) =>
              String(input) === `https://api.alsappan.test/v1/pets/${petId}?locale=pt-BR` &&
              init?.method === 'GET',
          ),
      ).toBe(true),
    )

    await user.click(await screen.findByRole('button', { name: 'Salvar alteracoes' }))

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input, init]) =>
              String(input) === `https://api.alsappan.test/v1/pets/${petId}?locale=pt-BR` &&
              init?.method === 'PUT' &&
              String(init.body).includes(
                `"vaccinationRecordDocumentId":"${vaccinationDocumentId}"`,
              ) &&
              String(init.body).includes(
                `"authorizationFormDocumentId":"${authorizationDocumentId}"`,
              ) &&
              String(init.body).includes('"concurrencyToken":"pet-token"') &&
              String(init.body).includes('"notes":"Docil"'),
          ),
      ).toBe(true),
    )
  })
})
