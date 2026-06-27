import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import type { AuthSessionDto } from '../../lib/api/identity'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import {
  defaultOrganizations,
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
  useFiltersStore,
} from '../../stores'
import { applyAuthSession } from '../identity/session'
import { ResidentForm } from './ResidentForm'
import { ResidentsListPage } from './ResidentsListPage'

const residentListItem = {
  contactSummary: 'maria@example.com | +55 11 99999-0000',
  createdAt: '2026-06-01T10:00:00.000Z',
  documentIdentifier: '123.456.789-00',
  documentType: 'CPF',
  email: 'maria@example.com',
  emergencyContact: {
    name: 'Joao Silva',
    phone: '+55 11 98888-0000',
    relationship: 'Irmao',
  },
  fullName: 'Maria Silva',
  id: 'resident-a',
  isArchived: false,
  isSensitiveMasked: false,
  notes: 'Prefere contato por e-mail.',
  phone: '+55 11 99999-0000',
  portalStatus: {
    code: 'invited',
    label: 'Convidado',
    tone: 'info',
  },
  preferredName: 'Maria',
  privacyFlags: ['contact-data'],
  relationships: [],
  secondaryPhone: '+55 11 97777-0000',
  status: {
    code: 'active',
    label: 'Ativo',
    tone: 'success',
  },
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

function buildResidentSession(): AuthSessionDto {
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
          permissionCodes: ['residents.read', 'residents.write'],
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions: ['residents.read', 'residents.write'],
    },
  }
}

function residentListResponse() {
  return {
    hasNextPage: false,
    hasPreviousPage: false,
    items: [residentListItem],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  }
}

function maskedResidentDetailResponse() {
  return {
    ...residentListItem,
    contactSummary: 'Dados restritos',
    email: 'maria@example.com',
    isSensitiveMasked: true,
    relationships: [
      {
        count: 1,
        label: 'Contrato vigente',
        module: 'contracts',
        route: '/contratos?residentId=resident-a',
      },
      {
        count: 1,
        label: 'Apartamento Jardim',
        module: 'properties',
        route: '/imoveis?residentId=resident-a',
      },
    ],
  }
}

function createResidentsFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/residents/status-options') {
      return jsonResponse([
        { code: 'active', label: 'Ativo', tone: 'success' },
        { code: 'inactive', label: 'Inativo', tone: 'neutral' },
        { code: 'archived', label: 'Arquivado', tone: 'archived' },
      ])
    }

    if (url.pathname === '/v1/residents/portal-status-options') {
      return jsonResponse([
        { code: 'not-invited', label: 'Nao convidado', tone: 'neutral' },
        { code: 'invited', label: 'Convidado', tone: 'info' },
        { code: 'active', label: 'Ativo', tone: 'success' },
        { code: 'disabled', label: 'Desabilitado', tone: 'danger' },
      ])
    }

    if (url.pathname === '/v1/residents/duplicate-warnings') {
      return jsonResponse([
        {
          field: 'email',
          message: 'Ja existe um morador com este e-mail.',
          residentId: 'resident-existing',
          residentName: 'Maria Existente',
          value: 'maria@example.com',
        },
      ])
    }

    if (url.pathname === '/v1/residents/resident-a' && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === '/v1/residents/resident-a') {
      return jsonResponse(maskedResidentDetailResponse())
    }

    if (url.pathname === '/v1/residents' && init?.method !== 'POST') {
      return jsonResponse(residentListResponse())
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

describe('residents management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads residents and sends list filters to the API', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildResidentSession())
    const fetchImpl = createResidentsFetch()

    renderWithApi(<ResidentsListPage />, fetchImpl)

    expect(await screen.findByText('Maria Silva')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Morador' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Contato' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Status' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Status do portal' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Documento' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Acoes' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar moradores' }), 'maria')
    await user.selectOptions(screen.getByLabelText('Status'), 'inactive')
    await user.selectOptions(screen.getByLabelText('Status do portal'), 'active')
    await user.selectOptions(screen.getByLabelText('Acesso ao portal'), 'true')

    await waitFor(() =>
      expect(
        vi.mocked(fetchImpl).mock.calls.some(([input]) => {
          const url = String(input)

          return (
            url.includes('/v1/residents?') &&
            url.includes('search=maria') &&
            url.includes('status=inactive') &&
            url.includes('portalStatus=active') &&
            url.includes('hasPortalAccess=true')
          )
        }),
      ).toBe(true),
    )

    await user.click(screen.getByLabelText('Arquivar Maria Silva'))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        'https://api.alsappan.test/v1/residents/resident-a',
        expect.objectContaining({ method: 'DELETE' }),
      ),
    )
  })

  it('validates the form and shows duplicate warnings without blocking submit', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const onCheckDuplicates = vi.fn(async () => [
      {
        field: 'email',
        message: 'Ja existe um morador com este e-mail.',
        residentId: 'resident-existing',
        residentName: 'Maria Existente',
        value: 'maria@example.com',
      },
    ])

    render(<ResidentForm mode="create" onCheckDuplicates={onCheckDuplicates} onSubmit={onSubmit} />)

    await user.click(screen.getByRole('button', { name: 'Criar morador' }))

    expect(screen.getByText('Informe o nome completo.')).toBeInTheDocument()
    expect(screen.getByText('Informe e-mail ou telefone.')).toBeInTheDocument()

    await user.type(screen.getByLabelText(/Nome completo/), 'Maria Silva')
    await user.type(screen.getByLabelText(/E-mail/), 'maria@example.com')
    await user.click(screen.getByRole('button', { name: 'Verificar duplicidades' }))

    expect(await screen.findByText('Ja existe um morador com este e-mail.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Criar morador' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        birthDate: undefined,
        documentIdentifier: undefined,
        documentType: undefined,
        email: 'maria@example.com',
        emergencyContact: {
          name: undefined,
          phone: undefined,
          relationship: undefined,
        },
        fullName: 'Maria Silva',
        notes: undefined,
        phone: undefined,
        portalStatus: 'not-invited',
        preferredName: undefined,
        privacyFlags: [],
        secondaryPhone: undefined,
        status: 'active',
      }),
    )
  })

  it('opens resident details with masked sensitive fields and relationship panels', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildResidentSession())
    const fetchImpl = createResidentsFetch()

    renderWithApi(<ResidentsListPage />, fetchImpl)

    expect(await screen.findByText('Maria Silva')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Ver detalhes Maria Silva'))

    expect(await screen.findByText('Resumo do morador')).toBeInTheDocument()
    expect(screen.getAllByText('Dados restritos').length).toBeGreaterThan(0)
    expect(screen.getByText('Contratos')).toBeInTheDocument()
    expect(screen.getByText('Imoveis')).toBeInTheDocument()
    expect(screen.getByText('Pagamentos')).toBeInTheDocument()
    expect(screen.getByText('Documentos')).toBeInTheDocument()
    expect(screen.getByText('Pets')).toBeInTheDocument()
    expect(screen.getByText('Veiculos')).toBeInTheDocument()
    expect(screen.getByText('Ocorrencias')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Abrir timeline do morador' })).toHaveAttribute(
      'href',
      '/timeline?residentId=resident-a',
    )
    expect(screen.getByRole('link', { name: 'Abrir auditoria do morador' })).toHaveAttribute(
      'href',
      '/auditoria?residentId=resident-a',
    )
  })

  it('renders resident form copy in English', () => {
    useAppPreferencesStore.getState().setLocale('en-US')

    render(<ResidentForm mode="create" onSubmit={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Create resident' })).toBeInTheDocument()
    expect(screen.getByLabelText(/Full name/)).toBeInTheDocument()
    expect(screen.getByText('Duplicate warnings')).toBeInTheDocument()
  })
})
