import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { adminMenuItems } from '../../navigation/menuContract'
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
import { modulePageRoute } from './modulePageRoute'
import { OccurrencesListPage } from './OccurrencesListPage'

const occurrenceId = '11111111-1111-1111-1111-111111111111'
const propertyId = '22222222-2222-2222-2222-222222222222'
const residentId = '33333333-3333-3333-3333-333333333333'
const contractId = '44444444-4444-4444-4444-444444444444'
const assigneeId = '55555555-5555-5555-5555-555555555555'
const documentId = '66666666-6666-6666-6666-666666666666'

const apiOccurrence = {
  archivedAt: undefined,
  assignedUser: {
    displayName: 'Bruno Operador',
    email: 'bruno@example.com',
    id: assigneeId,
  },
  assignmentHistory: [
    {
      actorDisplayName: 'Ana Admin',
      actorUserId: assigneeId,
      createdAt: '2026-06-27T10:02:00.000Z',
      id: 'assignment-history-1',
      newAssignedUser: {
        displayName: 'Bruno Operador',
        email: 'bruno@example.com',
        id: assigneeId,
      },
      notes: 'Direcionar manutencao',
      previousAssignedUser: undefined,
    },
  ],
  attachments: [
    {
      createdAt: '2026-06-27T10:04:00.000Z',
      documentId,
      label: 'Foto do vazamento',
      route: `/documentos?entityType=occurrence&entityId=${occurrenceId}`,
    },
  ],
  auditRoute: `/auditoria?entityType=occurrence&entityId=${occurrenceId}`,
  cancelledAt: undefined,
  cancellationNotes: undefined,
  comments: [
    {
      authorDisplayName: 'Ana Admin',
      authorUserId: assigneeId,
      body: 'Morador confirmou acesso.',
      createdAt: '2026-06-27T10:03:00.000Z',
      id: 'comment-1',
      isInternal: true,
    },
  ],
  concurrencyToken: 'occurrence-token',
  contract: {
    description: 'Casa Calabria - Joao da Silva',
    id: contractId,
    name: 'Contrato Casa Calabria',
    route: `/contratos?id=${contractId}`,
  },
  createdAt: '2026-06-27T10:00:00.000Z',
  description: 'Morador relatou vazamento recorrente.',
  dueDate: '2026-07-02',
  id: occurrenceId,
  isArchived: false,
  isUnresolved: true,
  priority: { code: 'high', label: 'Alta', tone: 'warning' },
  priorityHistory: [
    {
      actorDisplayName: 'Ana Admin',
      actorUserId: assigneeId,
      createdAt: '2026-06-27T10:00:00.000Z',
      id: 'priority-history-1',
      newPriority: { code: 'high', label: 'Alta', tone: 'warning' },
      notes: 'Prioridade inicial',
      previousPriority: undefined,
    },
  ],
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
  resolutionNotes: undefined,
  resolvedAt: undefined,
  status: { code: 'assigned', label: 'Atribuida', tone: 'info' },
  statusHistory: [
    {
      actorDisplayName: 'Ana Admin',
      actorUserId: assigneeId,
      createdAt: '2026-06-27T10:00:00.000Z',
      id: 'status-history-1',
      newStatus: { code: 'assigned', label: 'Atribuida', tone: 'info' },
      notes: 'Status inicial',
      previousStatus: undefined,
    },
  ],
  timelineRoute: `/timeline?entityType=occurrence&entityId=${occurrenceId}`,
  title: 'Vazamento na cozinha',
  type: { code: 'maintenance', label: 'Manutencao', tone: 'info' },
  updatedAt: '2026-06-27T10:30:00.000Z',
} as const

const apiOccurrenceListItem = (() => {
  const item = { ...apiOccurrence } as Record<string, unknown>
  delete item.archivedAt
  delete item.assignmentHistory
  delete item.attachments
  delete item.auditRoute
  delete item.cancelledAt
  delete item.cancellationNotes
  delete item.comments
  delete item.priorityHistory
  delete item.resolutionNotes
  delete item.resolvedAt
  delete item.statusHistory
  delete item.timelineRoute

  return item
})()

const apiAdministrator = {
  displayName: 'Bruno Operador',
  email: 'bruno@example.com',
  id: assigneeId,
  roleCodes: ['Administrador'],
  status: 'active',
  statusLabel: 'Ativo',
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

const apiDocument = {
  category: 'occurrence',
  categoryLabel: 'Ocorrencia',
  contentType: 'image/jpeg',
  currentVersionNumber: 1,
  fileName: 'foto-vazamento.jpg',
  id: documentId,
  isArchived: false,
  links: [],
  sizeBytes: 1024,
  status: { code: 'active', label: 'Ativo', tone: 'success' },
  title: 'Foto do vazamento',
  uploadedAt: '2026-06-27T10:00:00.000Z',
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

function buildOccurrenceSession(): AuthSessionDto {
  const permissions = [
    'occurrences.read',
    'occurrences.write',
    'occurrences.manage',
    'occurrences.archive',
    'properties.read',
    'residents.read',
    'contracts.read',
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

function createOccurrencesFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/occurrences/type-options') {
      return jsonResponse([{ label: 'Manutencao', value: 'maintenance' }])
    }

    if (url.pathname === '/v1/occurrences/priority-options') {
      return jsonResponse([
        { label: 'Alta', value: 'high' },
        { label: 'Urgente', value: 'urgent' },
      ])
    }

    if (url.pathname === '/v1/occurrences/status-options') {
      return jsonResponse([
        { label: 'Aberta', value: 'open' },
        { label: 'Atribuida', value: 'assigned' },
        { label: 'Em andamento', value: 'in-progress' },
      ])
    }

    if (url.pathname === '/v1/administrators') {
      return jsonResponse(paged([apiAdministrator]))
    }

    if (url.pathname === '/v1/properties') {
      return jsonResponse(paged([apiPropertyListItem]))
    }

    if (url.pathname === '/v1/residents') {
      return jsonResponse(paged([apiResidentListItem]))
    }

    if (url.pathname === '/v1/contracts') {
      return jsonResponse(paged([apiContractListItem]))
    }

    if (url.pathname === '/v1/documents') {
      return jsonResponse(paged([apiDocument]))
    }

    if (url.pathname === `/v1/occurrences/${occurrenceId}` && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname.startsWith(`/v1/occurrences/${occurrenceId}/`)) {
      return jsonResponse(apiOccurrence)
    }

    if (url.pathname === `/v1/occurrences/${occurrenceId}` && init?.method === 'PUT') {
      return jsonResponse(apiOccurrence)
    }

    if (url.pathname === `/v1/occurrences/${occurrenceId}`) {
      return jsonResponse(apiOccurrence)
    }

    if (url.pathname === '/v1/occurrences' && init?.method === 'POST') {
      return jsonResponse(apiOccurrence)
    }

    if (url.pathname === '/v1/occurrences') {
      return jsonResponse(paged([apiOccurrenceListItem]))
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

describe('occurrences management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('contributes the canonical occurrences route and permission contract', () => {
    const menuItem = adminMenuItems.find((item) => item.id === 'occurrences')

    expect(modulePageRoute.routeId).toBe('occurrences')
    expect(menuItem?.requiredPermission).toBe('occurrences.read')
  })

  it('loads occurrences with filters', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildOccurrenceSession())
    const fetchImpl = createOccurrencesFetch()

    renderWithApi(<OccurrencesListPage />, fetchImpl)

    expect(await screen.findByText('Vazamento na cozinha')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Ocorrencia' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Responsavel' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar ocorrencias' }), 'vazamento')
    await user.selectOptions(screen.getByLabelText('Tipo'), 'maintenance')
    await user.selectOptions(screen.getByLabelText('Prioridade'), 'urgent')
    await user.selectOptions(screen.getByLabelText('Status'), 'assigned')
    await user.selectOptions(screen.getByLabelText('Responsavel'), assigneeId)
    await user.type(screen.getByLabelText('ID do imovel'), propertyId)
    await user.type(screen.getByLabelText('ID do morador'), residentId)
    await user.type(screen.getByLabelText('ID do contrato'), contractId)
    await user.type(screen.getByLabelText('Data inicial'), '2026-07-01')
    await user.type(screen.getByLabelText('Data final'), '2026-07-31')
    await user.click(screen.getByLabelText('Somente nao resolvidas'))
    await user.click(screen.getByLabelText('Incluir arquivadas'))

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/occurrences?') &&
              String(input).includes('search=vazamento') &&
              String(input).includes('type=maintenance') &&
              String(input).includes('priority=urgent') &&
              String(input).includes('status=assigned') &&
              String(input).includes(`assignedUserId=${assigneeId}`) &&
              String(input).includes(`propertyId=${propertyId}`) &&
              String(input).includes(`residentId=${residentId}`) &&
              String(input).includes(`contractId=${contractId}`) &&
              String(input).includes('dateFrom=2026-07-01') &&
              String(input).includes('dateTo=2026-07-31') &&
              String(input).includes('unresolvedOnly=true') &&
              String(input).includes('includeArchived=true'),
          ),
      ).toBe(true),
    )
  })

  it('opens details and runs workflow actions, comments, and attachments', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildOccurrenceSession())
    const fetchImpl = createOccurrencesFetch()

    renderWithApi(<OccurrencesListPage />, fetchImpl)

    expect(await screen.findByText('Vazamento na cozinha')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Ver detalhes Vazamento na cozinha - Casa Calabria'))

    expect(await screen.findByText('Morador relatou vazamento recorrente.')).toBeInTheDocument()
    await user.click(screen.getByRole('tab', { name: 'Anexos' }))
    expect(screen.getAllByText('Foto do vazamento').length).toBeGreaterThan(0)

    await user.click(screen.getByRole('tab', { name: 'Workflow' }))
    await user.selectOptions(screen.getByLabelText('Prioridade'), 'urgent')
    await user.click(screen.getByRole('button', { name: 'Alterar prioridade' }))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/occurrences/${occurrenceId}/priority?locale=pt-BR`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )

    await user.selectOptions(screen.getByLabelText('Status'), 'in-progress')
    await user.click(screen.getByRole('button', { name: 'Alterar status' }))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/occurrences/${occurrenceId}/status?locale=pt-BR`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )

    await user.type(screen.getByLabelText('Notas da resolucao'), 'Conserto realizado.')
    await user.click(screen.getByRole('button', { name: 'Resolver' }))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/occurrences/${occurrenceId}/resolve?locale=pt-BR`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )

    await user.click(screen.getByRole('tab', { name: 'Comentarios' }))
    await user.type(screen.getByLabelText('Comentario'), 'Equipe avisada.')
    await user.click(screen.getByLabelText('Comentario interno'))
    await user.click(screen.getByRole('button', { name: 'Adicionar comentario' }))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/occurrences/${occurrenceId}/comments?locale=pt-BR`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )

    await user.click(screen.getByRole('tab', { name: 'Anexos' }))
    await user.selectOptions(screen.getByLabelText('ID do documento'), documentId)
    await user.click(screen.getByRole('button', { name: 'Anexar documento' }))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/occurrences/${occurrenceId}/attachments?locale=pt-BR`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )
  })
})
