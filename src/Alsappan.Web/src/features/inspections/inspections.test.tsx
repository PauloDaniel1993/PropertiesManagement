import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
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
import { InspectionDetailPage } from './InspectionDetailPage'
import { InspectionsListPage } from './InspectionsListPage'

const inspectionId = '11111111-1111-1111-1111-111111111111'
const checklistItemId = '22222222-2222-2222-2222-222222222222'
const propertyId = '33333333-3333-3333-3333-333333333333'
const contractId = '44444444-4444-4444-4444-444444444444'
const residentId = '55555555-5555-5555-5555-555555555555'
const assigneeId = '66666666-6666-6666-6666-666666666666'

const apiInspection = {
  archivedAt: undefined,
  assignee: {
    description: 'ana@example.com',
    id: assigneeId,
    name: 'Ana Admin',
    route: `/administradores?id=${assigneeId}`,
  },
  auditRoute: `/auditoria?entityType=inspection&entityId=${inspectionId}`,
  cancellationReason: undefined,
  cancelledAt: undefined,
  checklistItems: [
    {
      areaName: 'Sala',
      conditionRating: { code: 'pending', label: 'Pendente', tone: 'warning' },
      createdAt: '2026-06-27T10:00:00.000Z',
      id: checklistItemId,
      isComplete: false,
      isRequired: true,
      itemName: 'Piso',
      observations: undefined,
      sortOrder: 0,
      updatedAt: undefined,
    },
  ],
  completedAt: undefined,
  completionNotes: undefined,
  concurrencyToken: 'inspection-token',
  contract: {
    description: 'Casa Calabria - Joao da Silva',
    id: contractId,
    name: 'Contrato Casa Calabria',
    route: `/contratos?id=${contractId}`,
  },
  createdAt: '2026-06-27T09:00:00.000Z',
  id: inspectionId,
  isArchived: false,
  isPending: true,
  linkedDocuments: [],
  notes: 'Conferir sala e cozinha.',
  photoDocuments: [],
  progress: { completedItems: 0, percentage: 0, totalItems: 1 },
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
  scheduledAt: '2026-06-28T14:00:00.000Z',
  signatureSlots: [],
  startedAt: undefined,
  status: { code: 'scheduled', label: 'Agendada', tone: 'warning' },
  timelineRoute: `/timeline?entityType=inspection&entityId=${inspectionId}`,
  title: 'Vistoria de entrada',
  type: { code: 'move-in', label: 'Entrada', tone: 'success' },
  updatedAt: '2026-06-27T10:00:00.000Z',
} as const

const apiInspectionListItem = (() => {
  const item = { ...apiInspection } as Record<string, unknown>
  delete item.archivedAt
  delete item.auditRoute
  delete item.cancellationReason
  delete item.cancelledAt
  delete item.checklistItems
  delete item.completionNotes
  delete item.linkedDocuments
  delete item.notes
  delete item.photoDocuments
  delete item.signatureSlots
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

function buildInspectionSession(
  permissions: string[] = [
    'inspections.read',
    'inspections.write',
    'inspections.manage',
    'inspections.archive',
  ],
): AuthSessionDto {
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

function createInspectionsFetch(inspection: Record<string, unknown> = apiInspection) {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/inspections/type-options') {
      return jsonResponse([{ code: 'move-in', label: 'Entrada', tone: 'success' }])
    }

    if (url.pathname === '/v1/inspections/status-options') {
      return jsonResponse([
        { code: 'scheduled', label: 'Agendada', tone: 'warning' },
        { code: 'in-progress', label: 'Em andamento', tone: 'info' },
        { code: 'completed', label: 'Concluida', tone: 'success' },
        { code: 'archived', label: 'Arquivada', tone: 'archived' },
      ])
    }

    if (url.pathname === '/v1/inspections/condition-rating-options') {
      return jsonResponse([{ code: 'pending', label: 'Pendente', tone: 'warning' }])
    }

    if (url.pathname === '/v1/inspections/document-kind-options') {
      return jsonResponse([
        { code: 'photo', label: 'Foto', tone: 'info' },
        { code: 'attachment', label: 'Anexo', tone: 'neutral' },
      ])
    }

    if (url.pathname === '/v1/documents') {
      return jsonResponse(paged([]))
    }

    if (url.pathname === `/v1/inspections/${inspectionId}/start`) {
      return jsonResponse({
        ...inspection,
        status: { code: 'in-progress', label: 'Em andamento', tone: 'info' },
      })
    }

    if (url.pathname === `/v1/inspections/${inspectionId}` && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === `/v1/inspections/${inspectionId}`) {
      return jsonResponse(inspection)
    }

    if (url.pathname === '/v1/inspections') {
      return jsonResponse(paged([apiInspectionListItem]))
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

describe('inspections management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads inspections with filters and lifecycle actions', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildInspectionSession())
    const fetchImpl = createInspectionsFetch()

    renderWithApi(<InspectionsListPage />, fetchImpl)

    expect(await screen.findByText('Vistoria de entrada')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Data' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Responsavel' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar vistorias' }), 'entrada')
    await user.selectOptions(screen.getByLabelText('Tipo'), 'move-in')
    await user.selectOptions(screen.getByLabelText('Status'), 'scheduled')
    await user.type(screen.getByLabelText('ID do imovel'), propertyId)
    await user.type(screen.getByLabelText('ID do contrato'), contractId)
    await user.type(screen.getByLabelText('Responsavel'), assigneeId)
    await user.type(screen.getByLabelText('Data inicial'), '2026-06-27')
    await user.type(screen.getByLabelText('Data final'), '2026-06-30')
    await user.click(screen.getByLabelText('Pendentes'))
    await user.click(screen.getByLabelText('Incluir arquivadas'))

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/inspections?') &&
              String(input).includes('search=entrada') &&
              String(input).includes('type=move-in') &&
              String(input).includes('status=scheduled') &&
              String(input).includes(`propertyId=${propertyId}`) &&
              String(input).includes(`contractId=${contractId}`) &&
              String(input).includes(`assignedUserId=${assigneeId}`) &&
              String(input).includes('scheduledFrom=') &&
              String(input).includes('scheduledTo=') &&
              String(input).includes('pendingOnly=true') &&
              String(input).includes('includeArchived=true'),
          ),
      ).toBe(true),
    )

    await user.click(screen.getByLabelText('Iniciar Vistoria de entrada - Casa Calabria'))
    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/inspections/${inspectionId}/start?locale=pt-BR`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )
  })

  it('renders photo attachments in details and includes them in completed report counts', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildInspectionSession(['inspections.read']))
    const completedInspection = {
      ...apiInspection,
      checklistItems: [
        {
          ...apiInspection.checklistItems[0],
          conditionRating: { code: 'good', label: 'Bom', tone: 'success' },
          isComplete: true,
        },
      ],
      completedAt: '2026-06-28T16:00:00.000Z',
      completionNotes: 'Checklist completo.',
      isPending: false,
      linkedDocuments: [
        {
          documentId: '77777777-7777-7777-7777-777777777777',
          kind: { code: 'report', label: 'Relatorio' },
          label: 'Laudo final',
          route: '/documentos?id=77777777-7777-7777-7777-777777777777',
        },
      ],
      photoDocuments: [
        {
          checklistItemId,
          documentId: '88888888-8888-8888-8888-888888888888',
          kind: { code: 'photo', label: 'Foto' },
          label: 'Foto da sala',
          route: '/documentos?id=88888888-8888-8888-8888-888888888888',
        },
      ],
      progress: { completedItems: 1, percentage: 100, totalItems: 1 },
      status: { code: 'completed', label: 'Concluida', tone: 'success' },
    } as const
    const fetchImpl = createInspectionsFetch(completedInspection)

    renderWithApi(
      <InspectionDetailPage inspectionId={inspectionId} onBack={() => undefined} />,
      fetchImpl,
    )

    expect(await screen.findByRole('heading', { name: 'Relatorio concluido' })).toBeInTheDocument()
    await user.click(screen.getByRole('tab', { name: 'Fotos e documentos' }))
    expect(screen.getByRole('link', { name: 'Foto da sala' })).toHaveAttribute(
      'href',
      '/documentos?id=88888888-8888-8888-8888-888888888888',
    )
    expect(screen.getByRole('link', { name: 'Laudo final' })).toHaveAttribute(
      'href',
      '/documentos?id=77777777-7777-7777-7777-777777777777',
    )

    const reportSection = screen
      .getByRole('heading', { name: 'Relatorio concluido' })
      .closest('section')
    expect(reportSection).not.toBeNull()
    expect(within(reportSection!).getByText('2')).toBeInTheDocument()
    expect(
      vi.mocked(fetchImpl).mock.calls.some(([input]) => String(input).includes('/v1/documents')),
    ).toBe(false)
  })

  it('hides detail mutation controls and skips document picker queries for read-only users', async () => {
    applyAuthSession(buildInspectionSession(['inspections.read']))
    const fetchImpl = createInspectionsFetch()

    renderWithApi(
      <InspectionDetailPage
        inspectionId={inspectionId}
        onBack={() => undefined}
        onCancel={vi.fn()}
        onComplete={vi.fn()}
        onEdit={vi.fn()}
        onStart={vi.fn()}
      />,
      fetchImpl,
    )

    expect(await screen.findByText('Vistoria de entrada')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Iniciar' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument()
    expect(screen.queryByText('Vincular foto ou documento')).not.toBeInTheDocument()
    expect(
      vi.mocked(fetchImpl).mock.calls.some(([input]) => String(input).includes('/v1/documents')),
    ).toBe(false)
  })
})
