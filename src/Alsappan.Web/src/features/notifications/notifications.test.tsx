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
import { NotificationsListPage } from './NotificationsListPage'

const notificationListItem = {
  category: {
    code: 'payments',
    label: 'Pagamentos',
    tone: 'info',
  },
  channel: {
    code: 'in-app',
    label: 'No aplicativo',
    tone: 'neutral',
  },
  createdAt: '2026-06-27T09:55:00.000Z',
  deepLink: '/pagamentos?paymentId=payment-a',
  deliveryStatus: {
    code: 'pending',
    label: 'Pendente',
    tone: 'warning',
  },
  eventId: 'event-a',
  eventName: 'payment.overdue',
  eventTypeLabel: 'Pagamento vencido',
  id: 'notification-a',
  isArchived: false,
  isRead: false,
  message: 'Aluguel junho possui pagamento vencido.',
  occurredAt: '2026-06-27T09:50:00.000Z',
  payload: {
    status: 'overdue',
  },
  readState: {
    code: 'unread',
    label: 'Nao lida',
    tone: 'info',
  },
  subjectDisplayName: 'Aluguel junho',
  subjectEntityId: 'payment-a',
  subjectEntityType: 'payment',
  title: 'Pagamento vencido',
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

function buildNotificationSession(): AuthSessionDto {
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
          permissionCodes: ['notifications.read'],
          roleCodes: ['Administrador'],
          slug: 'org-a',
        },
      ],
      permissions: ['notifications.read'],
    },
  }
}

function notificationListResponse() {
  return {
    hasNextPage: false,
    hasPreviousPage: false,
    items: [notificationListItem],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  }
}

function preferencesResponse() {
  return {
    isPersisted: false,
    notice: 'stub',
    preferences: [
      {
        category: 'payments',
        categoryLabel: 'Pagamentos',
        channel: 'in-app',
        channelLabel: 'No aplicativo',
        isEnabled: true,
        isMandatory: false,
      },
    ],
  }
}

function createNotificationsFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/notifications/unread-count') {
      return jsonResponse({ count: 1 })
    }

    if (url.pathname === '/v1/notifications/category-options') {
      return jsonResponse([{ label: 'Pagamentos', value: 'payments' }])
    }

    if (url.pathname === '/v1/notifications/channel-options') {
      return jsonResponse([{ label: 'No aplicativo', value: 'in-app' }])
    }

    if (url.pathname === '/v1/notifications/preferences') {
      return jsonResponse(preferencesResponse())
    }

    if (url.pathname === '/v1/notifications/notification-a/read') {
      return jsonResponse({
        ...notificationListItem,
        isRead: true,
        readState: {
          code: 'read',
          label: 'Lida',
          tone: 'neutral',
        },
      })
    }

    if (url.pathname === '/v1/notifications/read-all') {
      return jsonResponse({ updatedCount: 1 })
    }

    if (url.pathname === '/v1/notifications/notification-a' && init?.method === 'DELETE') {
      return emptyResponse()
    }

    if (url.pathname === '/v1/notifications') {
      return jsonResponse(notificationListResponse())
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

describe('notifications management UI', () => {
  beforeEach(() => {
    resetStores()
  })

  it('loads notifications with filters, read actions, and preferences', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildNotificationSession())
    const fetchImpl = createNotificationsFetch()

    renderWithApi(<NotificationsListPage />, fetchImpl)

    expect(await screen.findByText('Pagamento vencido')).toBeInTheDocument()
    expect(screen.getByText('1 nao lidas')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Notificacao' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Categoria' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Canal' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Estado' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Abrir origem Pagamento vencido' })).toHaveAttribute(
      'href',
      '/pagamentos?paymentId=payment-a',
    )

    await user.type(screen.getByRole('searchbox', { name: 'Buscar notificacoes' }), 'aluguel')
    await user.selectOptions(screen.getByLabelText('Categoria'), 'payments')
    await user.selectOptions(screen.getByLabelText('Estado de leitura'), 'false')

    await waitFor(() =>
      expect(
        vi.mocked(fetchImpl).mock.calls.some(([input]) => {
          const url = String(input)

          return (
            url.includes('/v1/notifications?') &&
            url.includes('search=aluguel') &&
            url.includes('category=payments') &&
            url.includes('isRead=false')
          )
        }),
      ).toBe(true),
    )

    await user.click(screen.getByLabelText('Marcar como lida Pagamento vencido'))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        expect.stringContaining('/v1/notifications/notification-a/read'),
        expect.objectContaining({ method: 'POST' }),
      ),
    )

    await user.click(screen.getByRole('button', { name: 'Preferencias' }))
    expect(await screen.findByText('Preferencias de notificacao')).toBeInTheDocument()
    expect(screen.getAllByText('Pagamentos').length).toBeGreaterThan(0)

    await user.click(screen.getByRole('button', { name: 'Salvar preferencias' }))

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        expect.stringContaining('/v1/notifications/preferences'),
        expect.objectContaining({ method: 'PUT' }),
      ),
    )
  })
})
