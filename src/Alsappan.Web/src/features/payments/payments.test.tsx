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
import { PaymentsListPage } from './PaymentsListPage'

const paymentId = '99999999-9999-9999-9999-999999999999'
const transactionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const contractId = '44444444-4444-4444-4444-444444444444'
const propertyId = '33333333-3333-3333-3333-333333333333'
const residentId = '55555555-5555-5555-5555-555555555555'

const apiPayment = {
  amount: { amount: 1000, currency: 'BRL' },
  auditRoute: `/auditoria?entityType=payment&entityId=${paymentId}`,
  balance: { amount: 1000, currency: 'BRL' },
  concurrencyToken: 'payment-token',
  contract: {
    description: 'Casa Calabria - Joao da Silva',
    id: contractId,
    name: 'Contrato Casa Calabria',
    route: `/contratos?id=${contractId}`,
  },
  createdAt: '2026-06-27T10:00:00.000Z',
  description: 'Mensalidade',
  discountAmount: { amount: 0, currency: 'BRL' },
  dueDate: '2026-06-30',
  grossAmount: { amount: 1000, currency: 'BRL' },
  id: paymentId,
  isArchived: false,
  isOverdue: false,
  notes: 'Observacoes',
  penaltyAmount: { amount: 0, currency: 'BRL' },
  preferredMethod: 'pix',
  preferredMethodLabel: 'Pix',
  property: {
    description: 'Rua Calabria, 82',
    id: propertyId,
    name: 'Casa Calabria',
    route: `/imoveis?id=${propertyId}`,
  },
  providerCode: 'mock-pix',
  providerMetadataJson: '{}',
  providerReference: 'mock-pix-reference',
  receiptDocuments: [],
  reconciliationStatus: { code: 'pending', label: 'Pendente', tone: 'warning' },
  resident: {
    id: residentId,
    name: 'Joao da Silva',
    route: `/moradores?id=${residentId}`,
  },
  settledAmount: { amount: 0, currency: 'BRL' },
  status: { code: 'pending', label: 'Pendente', tone: 'warning' },
  timelineRoute: `/timeline?entityType=payment&entityId=${paymentId}`,
  title: 'Aluguel junho',
  transactions: [
    {
      amount: { amount: 100, currency: 'BRL' },
      bankReference: 'PIX-1',
      createdAt: '2026-06-27T11:00:00.000Z',
      id: transactionId,
      isReversed: false,
      method: 'pix',
      methodLabel: 'Pix',
      settledOn: '2026-06-27',
    },
  ],
  updatedAt: '2026-06-27T10:30:00.000Z',
} as const

const apiInstruction = {
  amount: { amount: 1000, currency: 'BRL' },
  chargeId: paymentId,
  copyPasteCode: 'PIX-COPIA-E-COLA',
  dueDate: '2026-06-30',
  expiresAt: '2026-06-28T10:00:00.000Z',
  kind: 'pix',
  metadata: { mock: 'true' },
  payerSummary: 'Joao da Silva',
  providerCode: 'mock-pix',
  providerReference: 'mock-pix-reference',
  qrPayload: 'pix://mock/999',
  status: 'issued',
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

function buildPaymentSession(): AuthSessionDto {
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
            'payments.read',
            'payments.write',
            'payments.manage',
            'payments.archive',
          ],
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions: ['payments.read', 'payments.write', 'payments.manage', 'payments.archive'],
    },
  }
}

function createPaymentsFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/payments/method-options') {
      return jsonResponse([
        { label: 'Pix', value: 'pix' },
        { label: 'Boleto', value: 'boleto' },
      ])
    }

    if (url.pathname === '/v1/payments/reconciliation-status-options') {
      return jsonResponse([
        { code: 'not-required', label: 'Nao requer', tone: 'neutral' },
        { code: 'pending', label: 'Pendente', tone: 'warning' },
      ])
    }

    if (url.pathname === '/v1/payments/provider-options') {
      return jsonResponse([
        { label: 'Pix mock', value: 'mock-pix' },
        { label: 'Boleto mock', value: 'mock-boleto' },
      ])
    }

    if (url.pathname === `/v1/payments/${paymentId}/transactions`) {
      return jsonResponse({
        ...apiPayment,
        balance: { amount: 0, currency: 'BRL' },
        settledAmount: { amount: 1000, currency: 'BRL' },
        status: { code: 'paid', label: 'Pago', tone: 'success' },
      })
    }

    if (url.pathname === `/v1/payments/${paymentId}/instructions`) {
      return jsonResponse(apiInstruction)
    }

    if (url.pathname === `/v1/payments/${paymentId}` && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === `/v1/payments/${paymentId}`) {
      return jsonResponse(apiPayment)
    }

    if (url.pathname === '/v1/payments') {
      return jsonResponse(paged([apiPayment]))
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

describe('payments management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads payments with filters and records a settlement', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildPaymentSession())
    const fetchImpl = createPaymentsFetch()

    renderWithApi(<PaymentsListPage />, fetchImpl)

    expect(await screen.findByText('Aluguel junho')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Pagamento' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Status' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar pagamentos' }), 'junho')
    await user.selectOptions(screen.getByLabelText('Status'), 'pending')

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/payments?') &&
              String(input).includes('search=junho') &&
              String(input).includes('status=pending'),
          ),
      ).toBe(true),
    )

    await user.click(screen.getByLabelText('Receber Aluguel junho - Casa Calabria'))

    const settlementDialog = await screen.findByRole('dialog', {
      name: 'Registrar recebimento',
    })
    const settlementAmountInput = within(settlementDialog).getByLabelText(/Valor/)

    await user.clear(settlementAmountInput)
    await user.type(settlementAmountInput, '1000')
    await user.click(
      within(settlementDialog).getByRole('button', { name: 'Registrar recebimento' }),
    )

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/payments/${paymentId}/transactions?locale=pt-BR`,
        expect.objectContaining({
          body: expect.stringContaining('"amount":{"amount":1000,"currency":"BRL"}'),
          method: 'POST',
        }),
      ),
    )
  })

  it('generates mocked Pix instructions and opens details', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildPaymentSession())
    const fetchImpl = createPaymentsFetch()

    renderWithApi(<PaymentsListPage />, fetchImpl)

    expect(await screen.findByText('Aluguel junho')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Instrucoes Aluguel junho - Casa Calabria'))
    await user.click(screen.getByRole('button', { name: 'Gerar instrucoes' }))

    expect(await screen.findByText('PIX-COPIA-E-COLA')).toBeInTheDocument()

    const instructionDialog = screen.getByRole('dialog', { name: 'Instrucoes de pagamento' })

    await user.click(within(instructionDialog).getByText('Fechar'))
    await user.click(screen.getByLabelText('Ver detalhes Aluguel junho - Casa Calabria'))

    expect(await screen.findByText('Resumo do pagamento')).toBeInTheDocument()
    expect(screen.getByText('Recebimentos')).toBeInTheDocument()
    expect(screen.getByText('PIX-1')).toBeInTheDocument()
  })
})
