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
import { UtilityAccountsListPage } from './UtilityAccountsListPage'

const utilityAccountId = '11111111-1111-1111-1111-111111111111'
const billDocumentId = '22222222-2222-2222-2222-222222222222'
const receiptDocumentId = '33333333-3333-3333-3333-333333333333'
const uploadedReceiptDocumentId = '77777777-7777-7777-7777-777777777777'
const propertyId = '44444444-4444-4444-4444-444444444444'
const contractId = '55555555-5555-5555-5555-555555555555'
const residentId = '66666666-6666-6666-6666-666666666666'

const apiUtilityAccount = {
  amount: { amount: 350, currency: 'BRL' },
  archivedAt: undefined,
  auditRoute: `/auditoria?entityType=utilityAccount&entityId=${utilityAccountId}`,
  balance: { amount: 350, currency: 'BRL' },
  bankReference: undefined,
  billDocuments: [
    {
      documentId: billDocumentId,
      label: 'Fatura CPFL junho',
      route: `/documentos?entityType=utility-account&entityId=${utilityAccountId}`,
    },
  ],
  billingPeriodEnd: '2026-06-30',
  billingPeriodStart: '2026-06-01',
  concurrencyToken: 'utility-token',
  contract: {
    description: 'Casa Calabria - Joao da Silva',
    id: contractId,
    name: 'Contrato Casa Calabria',
    route: `/contratos?id=${contractId}`,
  },
  createdAt: '2026-06-27T10:00:00.000Z',
  description: 'Conta de energia do mes de junho',
  dueDate: '2026-07-10',
  id: utilityAccountId,
  isArchived: false,
  isOverdue: false,
  paidAmount: { amount: 0, currency: 'BRL' },
  paidOn: undefined,
  paymentMethod: undefined,
  property: {
    description: 'Rua Calabria, 82',
    id: propertyId,
    name: 'Casa Calabria',
    route: `/imoveis?id=${propertyId}`,
  },
  receiptDocuments: [
    {
      documentId: receiptDocumentId,
      label: 'Recibo CPFL junho',
      route: `/documentos?entityType=utility-account&entityId=${utilityAccountId}`,
    },
  ],
  resident: {
    id: residentId,
    name: 'Joao da Silva',
    route: `/moradores?id=${residentId}`,
  },
  responsibility: { code: 'contract', label: 'Contrato', tone: 'info' },
  status: { code: 'open', label: 'Aberta', tone: 'warning' },
  timelineRoute: `/timeline?entityType=utilityAccount&entityId=${utilityAccountId}`,
  title: 'Energia junho',
  type: { code: 'electricity', label: 'Energia', tone: 'warning' },
  updatedAt: '2026-06-27T10:30:00.000Z',
} as const

const apiUtilityAccountListItem = (() => {
  const item = { ...apiUtilityAccount } as Record<string, unknown>
  delete item.auditRoute
  delete item.description
  delete item.timelineRoute

  return item
})()

const apiBillDocument = {
  category: 'utility-account',
  categoryLabel: 'Conta de consumo',
  contentType: 'application/pdf',
  currentVersionNumber: 1,
  fileName: 'fatura-cpfl-junho.pdf',
  id: billDocumentId,
  isArchived: false,
  links: [
    {
      entityId: utilityAccountId,
      entityType: 'utility-account',
      label: 'Faturas',
      route: `/contas-de-consumo?id=${utilityAccountId}`,
    },
  ],
  sizeBytes: 2048,
  status: { code: 'active', label: 'Ativo', tone: 'success' },
  title: 'Fatura CPFL junho',
  uploadedAt: '2026-06-27T10:00:00.000Z',
}

const apiReceiptDocument = {
  ...apiBillDocument,
  fileName: 'recibo-cpfl-junho.pdf',
  id: receiptDocumentId,
  links: [
    {
      entityId: utilityAccountId,
      entityType: 'utility-account',
      label: 'Recibos',
      route: `/contas-de-consumo?id=${utilityAccountId}`,
    },
  ],
  title: 'Recibo CPFL junho',
}

const apiUploadedReceiptDocument = {
  ...apiReceiptDocument,
  fileName: 'recibo-enviado.pdf',
  id: uploadedReceiptDocumentId,
  title: 'Recibo enviado',
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

function buildUtilityAccountSession(): AuthSessionDto {
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
            'utilityAccounts.read',
            'utilityAccounts.write',
            'utilityAccounts.manage',
            'utilityAccounts.archive',
          ],
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions: [
        'utilityAccounts.read',
        'utilityAccounts.write',
        'utilityAccounts.manage',
        'utilityAccounts.archive',
      ],
    },
  }
}

function createUtilityAccountsFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/documents' && init?.method === 'POST') {
      return jsonResponse(apiUploadedReceiptDocument)
    }

    if (url.pathname === '/v1/documents') {
      return jsonResponse(paged([apiBillDocument, apiReceiptDocument]))
    }

    if (url.pathname === '/v1/utility-accounts/type-options') {
      return jsonResponse([
        { label: 'Energia', value: 'electricity' },
        { label: 'Agua', value: 'water' },
      ])
    }

    if (url.pathname === '/v1/utility-accounts/status-options') {
      return jsonResponse([
        { label: 'Aberta', value: 'open' },
        { label: 'Paga', value: 'paid' },
      ])
    }

    if (url.pathname === '/v1/utility-accounts/responsibility-options') {
      return jsonResponse([
        { label: 'Contrato', value: 'contract' },
        { label: 'Imovel', value: 'property' },
      ])
    }

    if (url.pathname === `/v1/utility-accounts/${utilityAccountId}/mark-paid`) {
      return jsonResponse({
        ...apiUtilityAccount,
        balance: { amount: 0, currency: 'BRL' },
        paidAmount: { amount: 350, currency: 'BRL' },
        paidOn: '2026-06-27',
        paymentMethod: 'pix',
        status: { code: 'paid', label: 'Paga', tone: 'success' },
      })
    }

    if (url.pathname === `/v1/utility-accounts/${utilityAccountId}` && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === `/v1/utility-accounts/${utilityAccountId}` && init?.method === 'PUT') {
      return jsonResponse(apiUtilityAccount)
    }

    if (url.pathname === `/v1/utility-accounts/${utilityAccountId}`) {
      return jsonResponse(apiUtilityAccount)
    }

    if (url.pathname === '/v1/utility-accounts') {
      return jsonResponse(paged([apiUtilityAccountListItem]))
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

describe('utility accounts management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads utility accounts with filters', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildUtilityAccountSession())
    const fetchImpl = createUtilityAccountsFetch()

    renderWithApi(<UtilityAccountsListPage />, fetchImpl)

    expect(await screen.findByText('Energia junho')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Conta de consumo' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Responsavel' })).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Buscar contas de consumo' }), 'junho')
    await user.selectOptions(screen.getByLabelText('Tipo'), 'electricity')
    await user.selectOptions(screen.getByLabelText('Status'), 'open')
    await user.selectOptions(screen.getByLabelText('Responsavel'), 'contract')
    await user.type(screen.getByLabelText('ID do imovel'), propertyId)
    await user.type(screen.getByLabelText('ID do contrato'), contractId)

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input]) =>
              String(input).includes('/v1/utility-accounts?') &&
              String(input).includes('search=junho') &&
              String(input).includes('type=electricity') &&
              String(input).includes('status=open') &&
              String(input).includes('responsibility=contract') &&
              String(input).includes(`propertyId=${propertyId}`) &&
              String(input).includes(`contractId=${contractId}`),
          ),
      ).toBe(true),
    )
  })

  it('marks a utility account as paid', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildUtilityAccountSession())
    const fetchImpl = createUtilityAccountsFetch()

    renderWithApi(<UtilityAccountsListPage />, fetchImpl)

    expect(await screen.findByText('Energia junho')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Pagar Energia junho - Casa Calabria'))

    const markPaidDialog = await screen.findByRole('dialog', {
      name: 'Marcar conta como paga',
    })
    const paidAmountInput = within(markPaidDialog).getByLabelText(/Valor pago/)

    await user.clear(paidAmountInput)
    await user.type(paidAmountInput, '350')
    await user.selectOptions(within(markPaidDialog).getByLabelText(/Metodo de pagamento/), 'pix')
    await user.type(within(markPaidDialog).getByLabelText(/Referencia bancaria/), 'PIX-CPFL-1')
    await user.upload(
      within(markPaidDialog).getByLabelText('Arquivo'),
      new File(['recibo'], 'recibo.pdf', { type: 'application/pdf' }),
    )
    await user.click(within(markPaidDialog).getByRole('button', { name: 'Enviar e anexar' }))

    await waitFor(() =>
      expect(
        vi.mocked(fetchImpl).mock.calls.some(
          ([input, init]) =>
            String(input).includes('/v1/documents?') &&
            init?.method === 'POST' &&
            init.body instanceof FormData &&
            init.body.get('category') === 'utility-account' &&
            init.body.get('linksJson') ===
              JSON.stringify([
                {
                  entityId: utilityAccountId,
                  entityType: 'utility-account',
                  label: 'Recibos',
                },
              ]),
        ),
      ).toBe(true),
    )

    await user.click(within(markPaidDialog).getByRole('button', { name: 'Marcar como paga' }))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        `https://api.alsappan.test/v1/utility-accounts/${utilityAccountId}/mark-paid?locale=pt-BR`,
        expect.objectContaining({
          body: expect.stringContaining('"amount":{"amount":350,"currency":"BRL"}'),
          method: 'POST',
        }),
      ),
    )

    expect(
      vi
        .mocked(fetchImpl)
        .mock.calls.some(
          ([, init]) =>
            init?.method === 'POST' &&
            String(init.body).includes('"bankReference":"PIX-CPFL-1"') &&
            String(init.body).includes(`"receiptDocumentId":"${uploadedReceiptDocumentId}"`),
        ),
    ).toBe(true)
  })

  it('opens details and preserves detail-only fields when editing from the list', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildUtilityAccountSession())
    const fetchImpl = createUtilityAccountsFetch()

    renderWithApi(<UtilityAccountsListPage />, fetchImpl)

    expect(await screen.findByText('Energia junho')).toBeInTheDocument()

    await user.click(screen.getByLabelText('Ver detalhes Energia junho - Casa Calabria'))

    expect(await screen.findByText('Resumo da conta de consumo')).toBeInTheDocument()
    expect(screen.getByText('Fatura CPFL junho')).toBeInTheDocument()
    expect(screen.getByText('Recibo CPFL junho')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Marcar como paga' }))
    expect(
      await screen.findByRole('dialog', { name: 'Marcar conta como paga' }),
    ).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Cancelar' }))

    await user.click(screen.getByRole('button', { name: 'Voltar para contas de consumo' }))
    await user.click(screen.getByLabelText('Editar Energia junho - Casa Calabria'))

    await waitFor(() =>
      expect(
        vi
          .mocked(fetchImpl)
          .mock.calls.some(
            ([input, init]) =>
              String(input) ===
                `https://api.alsappan.test/v1/utility-accounts/${utilityAccountId}?locale=pt-BR` &&
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
              String(input) ===
                `https://api.alsappan.test/v1/utility-accounts/${utilityAccountId}?locale=pt-BR` &&
              init?.method === 'PUT' &&
              String(init.body).includes('"description":"Conta de energia do mes de junho"') &&
              String(init.body).includes(`"billDocumentId":"${billDocumentId}"`) &&
              String(init.body).includes('"concurrencyToken":"utility-token"'),
          ),
      ).toBe(true),
    )
  })
})
