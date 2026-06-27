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
import { DocumentForm } from './DocumentForm'
import { DocumentsListPage } from './DocumentsListPage'

const documentId = '77777777-7777-7777-7777-777777777777'
const contractId = '88888888-8888-8888-8888-888888888888'

const apiDocument = {
  archivedAt: null,
  auditRoute: `/auditoria?entityType=document&entityId=${documentId}`,
  category: 'contract',
  categoryLabel: 'Contrato',
  concurrencyToken: 'document-token',
  contentType: 'application/pdf',
  createdAt: '2026-06-27T10:00:00.000Z',
  currentVersionNumber: 1,
  description: 'Documento digitalizado',
  downloadRoute: `/v1/documents/${documentId}/download`,
  fileName: 'contrato.pdf',
  id: documentId,
  isArchived: false,
  links: [
    {
      entityId: contractId,
      entityType: 'contract',
      label: 'Contrato 1',
      route: `/contratos?id=${contractId}`,
    },
  ],
  sizeBytes: 3,
  status: { code: 'active', label: 'Ativo', tone: 'success' },
  timelineRoute: `/timeline?entityType=document&entityId=${documentId}`,
  title: 'Contrato assinado',
  updatedAt: '2026-06-27T10:20:00.000Z',
  uploadedAt: '2026-06-27T10:00:00.000Z',
  versions: [
    {
      contentType: 'application/pdf',
      fileName: 'contrato.pdf',
      id: '99999999-9999-9999-9999-999999999999',
      notes: 'Versao inicial',
      sizeBytes: 3,
      uploadedAt: '2026-06-27T10:00:00.000Z',
      versionNumber: 1,
    },
  ],
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

function buildDocumentSession(): AuthSessionDto {
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
            'documents.read',
            'documents.write',
            'documents.archive',
            'contracts.read',
          ],
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions: ['documents.read', 'documents.write', 'documents.archive', 'contracts.read'],
    },
  }
}

function createDocumentsFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/documents/category-options') {
      return jsonResponse([{ label: 'Contrato', value: 'contract' }])
    }

    if (url.pathname === '/v1/documents/allowed-file-types') {
      return jsonResponse([{ label: 'Arquivo .pdf', value: '.pdf' }])
    }

    if (url.pathname === `/v1/documents/${documentId}/download`) {
      return Promise.resolve(
        new Response(new Blob(['pdf'], { type: 'application/pdf' }), {
          headers: {
            'Content-Disposition': 'attachment; filename="contrato.pdf"',
            'Content-Type': 'application/pdf',
          },
          status: 200,
        }),
      )
    }

    if (url.pathname === `/v1/documents/${documentId}` && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === `/v1/documents/${documentId}`) {
      return jsonResponse(apiDocument)
    }

    if (url.pathname === '/v1/documents' && init?.method === 'POST') {
      return jsonResponse(apiDocument)
    }

    if (url.pathname === '/v1/documents') {
      return jsonResponse(paged([apiDocument]))
    }

    return jsonResponse({ title: 'Not found' }, 404)
  }) as unknown as typeof fetch
}

function resetStores() {
  window.history.pushState({}, '', '/')
  localStorage.clear()
  useActiveOrganizationStore.getState().setOrganizations(defaultOrganizations)
  useAppPreferencesStore.getState().resetPreferences()
  useAuthSessionStore.getState().signOut()
  useFiltersStore.getState().resetFilters()
}

describe('documents management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('validates and submits upload form values', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()

    render(
      <DocumentForm
        allowedFileTypes={['.pdf']}
        categoryOptions={[{ label: 'Contrato', value: 'contract' }]}
        mode="create"
        onSubmit={onSubmit}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Enviar documento' }))

    expect(screen.getByText('Informe o titulo do documento.')).toBeInTheDocument()
    expect(screen.getByText('Selecione um arquivo.')).toBeInTheDocument()

    await user.type(screen.getByLabelText(/Titulo/), 'Contrato assinado')
    await user.selectOptions(screen.getByLabelText(/Categoria/), 'contract')
    await user.upload(
      screen.getByLabelText(/Arquivo/),
      new File(['pdf'], 'contrato.pdf', { type: 'application/pdf' }),
    )
    await user.selectOptions(screen.getByLabelText(/Tipo do registro vinculado/), 'contract')
    await user.type(screen.getByLabelText(/ID do registro vinculado/), contractId)
    await user.type(screen.getByLabelText(/Rotulo do vinculo/), 'Contrato 1')
    await user.click(screen.getByRole('button', { name: 'Enviar documento' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        mode: 'create',
        values: expect.objectContaining({
          category: 'contract',
          links: [{ entityId: contractId, entityType: 'contract', label: 'Contrato 1' }],
          title: 'Contrato assinado',
        }),
      }),
    )
  })

  it('loads documents, applies filters, downloads, archives, and opens details', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildDocumentSession())
    const fetchImpl = createDocumentsFetch()
    const anchorClick = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => undefined)

    renderWithApi(<DocumentsListPage />, fetchImpl)

    expect(await screen.findByText('Contrato assinado')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Documento' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Categoria' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Status' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar documentos' }), 'contrato')
    await user.selectOptions(screen.getByLabelText('Categoria'), 'contract')

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/documents?') &&
              String(input).includes('search=contrato') &&
              String(input).includes('category=contract'),
          ),
      ).toBe(true),
    )

    await user.click(screen.getByLabelText('Baixar Contrato assinado'))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/documents/${documentId}/download`,
        expect.objectContaining({ method: 'GET' }),
      ),
    )

    await user.click(screen.getByLabelText('Arquivar Contrato assinado'))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/documents/${documentId}`,
        expect.objectContaining({ method: 'DELETE' }),
      ),
    )

    await user.click(screen.getByLabelText('Ver detalhes Contrato assinado'))

    expect(await screen.findByText('Resumo do documento')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Contrato 1' })).toHaveAttribute(
      'href',
      `/contratos?id=${contractId}`,
    )
    expect(screen.getByRole('link', { name: 'Timeline' })).toHaveAttribute(
      'href',
      `/timeline?entityType=document&entityId=${documentId}`,
    )
    expect(screen.getByRole('link', { name: 'Auditoria' })).toHaveAttribute(
      'href',
      `/auditoria?entityType=document&entityId=${documentId}`,
    )

    anchorClick.mockRestore()
  })

  it('applies category and linked entity route filters to the list query', async () => {
    applyAuthSession(buildDocumentSession())
    window.history.pushState({}, '', `/documentos?contractId=${contractId}&category=contract`)
    const fetchImpl = createDocumentsFetch()

    renderWithApi(<DocumentsListPage />, fetchImpl)

    expect(await screen.findByText('Contrato assinado')).toBeInTheDocument()

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/documents?') &&
              String(input).includes('category=contract') &&
              String(input).includes(`linkedEntityId=${contractId}`) &&
              String(input).includes('linkedEntityType=contract'),
          ),
      ).toBe(true),
    )
  })
})
