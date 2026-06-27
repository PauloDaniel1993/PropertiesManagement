import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { ApiClient } from '../../lib/api/client'
import { ApiClientContext } from '../../lib/api/ApiClientContext'
import type { AuthSessionDto } from '../../lib/api/identity'
import {
  useActiveOrganizationStore,
  useAppPreferencesStore,
  useAuthSessionStore,
  useResidentPortalStore,
} from '../../stores'
import { IdentityLoginPage } from '../identity'
import { applyAuthSession } from '../identity/session'
import {
  ResidentPortalDocumentsPage,
  ResidentPortalOccurrencesPage,
  ResidentPortalPaymentsPage,
} from './ResidentPortalPages'
import { ResidentPortalShell } from './ResidentPortalShell'

const paymentId = '11111111-1111-1111-1111-111111111111'
const otherPaymentId = '22222222-2222-2222-2222-222222222222'

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

function renderWithApi(ui: ReactNode, fetchImpl: typeof fetch) {
  const apiClient = new ApiClient({
    baseUrl: 'https://api.alsappan.test',
    fetchImpl,
  })

  return render(
    <QueryClientProvider client={createQueryClient()}>
      <ApiClientContext.Provider value={apiClient}>{ui}</ApiClientContext.Provider>
    </QueryClientProvider>,
  )
}

function buildResidentSession(): AuthSessionDto {
  return {
    accessToken: 'access-resident',
    expiresAt: '2026-06-27T12:00:00.000Z',
    refreshToken: 'refresh-resident',
    tokenType: 'Bearer',
    user: {
      accountType: 'resident',
      activeOrganizationId: 'org-a',
      displayName: 'Joao Resident',
      email: 'resident@example.com',
      id: 'resident-user',
      organizations: [
        {
          currencyCode: 'BRL',
          displayName: 'Organizacao A',
          id: 'org-a',
          locale: 'pt-BR',
          name: 'Organizacao A',
          permissionCodes: [],
          roleCodes: ['resident.user'],
          slug: 'org-a',
        },
      ],
      permissions: [],
    },
  }
}

function portalSummary(overrides: Record<string, unknown> = {}) {
  return {
    contracts: [
      {
        dueDay: 10,
        endDate: '2027-06-30',
        id: '33333333-3333-3333-3333-333333333333',
        isArchived: false,
        monthlyRent: { amount: 1200, currency: 'BRL' },
        property: {
          description: 'Rua Calabria, 82',
          id: '44444444-4444-4444-4444-444444444444',
          name: 'Casa Calabria',
          route: '/portal/imovel',
        },
        startDate: '2026-01-01',
        status: { code: 'active', label: 'Active', tone: 'success' },
      },
    ],
    documents: [
      {
        category: 'resident',
        categoryLabel: 'Resident',
        contentType: 'application/pdf',
        downloadRoute: '/v1/resident-portal/documents/abc/download',
        fileName: 'resident.pdf',
        id: '55555555-5555-5555-5555-555555555555',
        sizeBytes: 2048,
        status: { code: 'active', label: 'Active', tone: 'success' },
        title: 'Resident document',
        uploadedAt: '2026-06-27T10:00:00.000Z',
      },
    ],
    inspections: [],
    linkedProperty: {
      city: 'Sao Paulo',
      garageSpaceCount: 1,
      id: '44444444-4444-4444-4444-444444444444',
      name: 'Casa Calabria',
      neighborhood: 'Centro',
      number: '82',
      stateCode: 'SP',
      status: { code: 'active', label: 'Active', tone: 'success' },
      streetLine: 'Rua Calabria',
      suggestedRent: { amount: 1200, currency: 'BRL' },
    },
    notifications: [
      {
        category: { code: 'payments', label: 'Payments', tone: 'info' },
        createdAt: '2026-06-27T10:00:00.000Z',
        id: '66666666-6666-6666-6666-666666666666',
        isRead: false,
        message: 'Payment reminder',
        title: 'Payment due',
      },
    ],
    occurrences: [],
    payments: [
      {
        amount: { amount: 1200, currency: 'BRL' },
        balance: { amount: 1200, currency: 'BRL' },
        dueDate: '2026-06-30',
        id: paymentId,
        isOverdue: false,
        preferredMethod: 'pix',
        preferredMethodLabel: 'Pix',
        property: {
          id: '44444444-4444-4444-4444-444444444444',
          name: 'Casa Calabria',
          route: '/portal/imovel',
        },
        settledAmount: { amount: 0, currency: 'BRL' },
        status: { code: 'pending', label: 'Pending', tone: 'warning' },
        title: 'June rent',
      },
    ],
    profile: {
      email: 'resident@example.com',
      fullName: 'Joao Resident',
      phone: '+55 11 99999-0000',
      portalStatus: { code: 'active', label: 'Active', tone: 'success' },
      residentId: '77777777-7777-7777-7777-777777777777',
      status: { code: 'active', label: 'Active', tone: 'success' },
    },
    settings: {
      allowDocumentUpload: true,
      allowOccurrenceCreation: true,
      allowProfileUpdateRequests: true,
      isEnabled: true,
    },
    ...overrides,
  }
}

function instructionResponse() {
  return {
    amount: { amount: 1200, currency: 'BRL' },
    chargeId: paymentId,
    copyPasteCode: 'PIX-COPY-PASTE',
    dueDate: '2026-06-30',
    expiresAt: '2026-06-28T10:00:00.000Z',
    kind: 'pix',
    metadata: { mock: 'true' },
    payerSummary: 'Joao Resident',
    providerCode: 'mock-pix',
    providerReference: 'mock-pix-reference',
    qrPayload: 'pix://mock/111',
    status: 'issued',
  }
}

function createPortalFetch(summary = portalSummary()) {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/resident-portal/summary') {
      return jsonResponse(summary)
    }

    if (url.pathname === `/v1/resident-portal/payments/${paymentId}/instructions`) {
      return jsonResponse(instructionResponse())
    }

    if (url.pathname === `/v1/resident-portal/payments/${otherPaymentId}/instructions`) {
      return jsonResponse({ title: 'Not found' }, 404)
    }

    return jsonResponse({ title: `Unexpected ${init?.method ?? 'GET'} ${url.pathname}` }, 404)
  }) as unknown as typeof fetch
}

function renderPortal(path: string, element: ReactNode, fetchImpl = createPortalFetch()) {
  applyAuthSession(buildResidentSession())

  return renderWithApi(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route element={<ResidentPortalShell />} path="/portal/*">
          <Route element={element} path={path.replace('/portal/', '')} />
        </Route>
      </Routes>
    </MemoryRouter>,
    fetchImpl,
  )
}

function resetStores() {
  localStorage.clear()
  useAuthSessionStore.getState().signOut()
  useActiveOrganizationStore.getState().setOrganizations([])
  useAppPreferencesStore.getState().resetPreferences()
  useResidentPortalStore.getState().resetResidentPortalState()
}

describe('resident portal frontend', () => {
  beforeEach(() => {
    resetStores()
  })

  it('submits resident login to the resident auth endpoint', async () => {
    const user = userEvent.setup()
    const fetchImpl = vi.fn(() => jsonResponse(buildResidentSession())) as unknown as typeof fetch

    renderWithApi(
      <MemoryRouter>
        <IdentityLoginPage accountType="resident" defaultRedirectPath="/portal" />
      </MemoryRouter>,
      fetchImpl,
    )

    await user.type(screen.getByLabelText(/E-mail/), 'resident@example.com')
    await user.type(screen.getByLabelText(/Senha/), 'secret123')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    await waitFor(() => expect(useAuthSessionStore.getState().isAuthenticated).toBe(true))
    expect(fetchImpl).toHaveBeenCalledWith(
      'https://api.alsappan.test/v1/auth/resident/login',
      expect.objectContaining({
        body: JSON.stringify({
          email: 'resident@example.com',
          password: 'secret123',
        }),
        method: 'POST',
      }),
    )
  })

  it('shows localized resident navigation and only linked resident payment data', async () => {
    useAppPreferencesStore.getState().setLocale('en-US')
    renderPortal('/portal/pagamentos', <ResidentPortalPaymentsPage />)

    expect((await screen.findAllByText('Payments')).length).toBeGreaterThan(0)
    expect(screen.getByRole('link', { name: /Property/ })).toBeInTheDocument()
    expect(await screen.findByText('June rent')).toBeInTheDocument()
    expect(screen.queryByText('Other resident rent')).not.toBeInTheDocument()
  })

  it('generates and displays resident-visible mocked Pix payment instructions', async () => {
    const user = userEvent.setup()
    const fetchImpl = createPortalFetch()
    renderPortal('/portal/pagamentos', <ResidentPortalPaymentsPage />, fetchImpl)

    await user.click(await screen.findByRole('button', { name: 'Gerar instrucoes' }))
    await user.click(screen.getAllByRole('button', { name: 'Gerar instrucoes' }).at(-1)!)

    expect(await screen.findByText('PIX-COPY-PASTE')).toBeInTheDocument()
    expect(screen.getByText('mock-pix-reference')).toBeInTheDocument()
    expect(fetchImpl).toHaveBeenCalledWith(
      `https://api.alsappan.test/v1/resident-portal/payments/${paymentId}/instructions?locale=pt-BR`,
      expect.objectContaining({
        body: JSON.stringify({ providerCode: 'mock-pix' }),
        method: 'POST',
      }),
    )
  })

  it('disables resident occurrence and document flows when organization settings deny them', async () => {
    const disabledSummary = portalSummary({
      settings: {
        allowDocumentUpload: false,
        allowOccurrenceCreation: false,
        allowProfileUpdateRequests: true,
        isEnabled: true,
      },
    })
    const fetchImpl = createPortalFetch(disabledSummary)

    const documents = renderPortal('/portal/documentos', <ResidentPortalDocumentsPage />, fetchImpl)

    expect(
      await screen.findByText('Envio de documentos esta desativado pela organizacao.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Enviar documento' })).toBeDisabled()

    documents.unmount()
    renderPortal('/portal/ocorrencias', <ResidentPortalOccurrencesPage />, fetchImpl)

    expect(
      await screen.findByText('Criacao de ocorrencias esta desativada pela organizacao.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Criar ocorrencia' })).toBeDisabled()
  })
})
