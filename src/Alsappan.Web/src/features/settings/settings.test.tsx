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
import { SettingsPage } from './SettingsPage'

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
      <ApiClientContext.Provider value={apiClient}>
        <MemoryRouter>{ui}</MemoryRouter>
      </ApiClientContext.Provider>
    </QueryClientProvider>,
  )
}

function buildSettingsSession(
  permissions: string[] = ['settings.read', 'settings.write', 'settings.manage'],
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

function settingsDashboardResponse() {
  return {
    branding: {
      accentColor: '#00a884',
      accentForegroundColor: '#ffffff',
      concurrencyToken: 'brand-token',
      displayName: 'Moradas A',
      logo: null,
      logoAlt: 'Moradas A',
      logoUrl: '',
      primaryColor: '#1877f2',
      primaryForegroundColor: '#ffffff',
      supportEmail: 'suporte@example.com',
      supportPhone: '+55 11 99999-0000',
      supportUrl: 'https://example.com/suporte',
    },
    catalogs: [
      {
        catalogType: 'property-types',
        items: [
          {
            catalogType: 'property-types',
            code: 'apartment',
            concurrencyToken: 'catalog-token',
            id: 'catalog-a',
            isEnabled: true,
            isSystem: true,
            labels: {
              'en-US': 'Apartment',
              'pt-BR': 'Apartamento',
            },
            sortOrder: 10,
          },
        ],
        label: 'Tipos de imovel',
      },
    ],
    localization: {
      concurrencyToken: 'locale-token',
      defaultLocale: 'pt-BR',
      enabledLocales: ['pt-BR', 'en-US'],
      fallbackLocale: 'pt-BR',
      supportedLocales: [
        {
          code: 'pt-BR',
          isDefault: true,
          isEnabled: true,
          isFallback: true,
          label: 'Portugues (Brasil)',
        },
        {
          code: 'en-US',
          isDefault: false,
          isEnabled: true,
          isFallback: false,
          label: 'English (US)',
        },
      ],
    },
    notifications: {
      availableCategories: [{ code: 'occurrences', isEnabled: true, label: 'Ocorrencias' }],
      availableChannels: [{ code: 'in-app', isEnabled: true, label: 'No aplicativo' }],
      concurrencyToken: 'notification-token',
      emailEnabled: true,
      enabledCategories: ['occurrences'],
      enabledChannels: ['in-app'],
      inAppEnabled: true,
      whatsAppEnabled: false,
    },
    organization: {
      concurrencyToken: 'org-token',
      contactEmail: 'contato@example.com',
      contactPhone: '+55 11 98888-0000',
      contactWebsite: 'https://example.com',
      createdAt: '2026-06-27T10:00:00.000Z',
      currencyCode: 'BRL',
      displayName: 'Moradas A',
      name: 'Moradas A Ltda',
      organizationId: 'org-a',
      slug: 'moradas-a',
      timeZone: 'America/Sao_Paulo',
      updatedAt: '2026-06-27T10:05:00.000Z',
    },
    profilePreferences: {
      effectiveLocale: 'pt-BR',
      isPersisted: false,
      locale: 'pt-BR',
      notice: 'Usando o locale padrao da organizacao.',
      supportedLocales: [
        {
          code: 'pt-BR',
          isDefault: true,
          isEnabled: true,
          isFallback: true,
          label: 'Portugues (Brasil)',
        },
        {
          code: 'en-US',
          isDefault: false,
          isEnabled: true,
          isFallback: false,
          label: 'English (US)',
        },
      ],
    },
    residentPortal: {
      allowDocumentUpload: true,
      allowOccurrenceCreation: true,
      allowProfileUpdateRequests: true,
      concurrencyToken: 'portal-token',
      isEnabled: true,
    },
    security: {
      concurrencyToken: 'security-token',
      mfaPolicy: 'optional',
      passwordRules: {
        minimumLength: 10,
        requireDigit: true,
        requireLowercase: true,
        requireSymbol: false,
        requireUppercase: true,
      },
      sessionTimeoutMinutes: 45,
      supportedMfaPolicies: ['disabled', 'optional', 'required'],
    },
    tenantBehavior: {
      allowOrganizationSwitching: true,
      concurrencyToken: 'tenant-token',
      requireActiveOrganization: true,
      strictTenantIsolation: true,
    },
  }
}

function createSettingsFetch() {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input))

    if (url.pathname === '/v1/settings/organization' && init?.method === 'PUT') {
      return jsonResponse({
        ...settingsDashboardResponse().organization,
        ...JSON.parse(String(init.body ?? '{}')),
        updatedAt: '2026-06-27T10:10:00.000Z',
      })
    }

    if (url.pathname === '/v1/settings') {
      return jsonResponse(settingsDashboardResponse())
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

describe('settings page', () => {
  beforeEach(() => {
    resetStores()
  })

  it('navigates settings tabs and saves organization profile updates', async () => {
    const user = userEvent.setup()
    applyAuthSession(buildSettingsSession())
    const fetchImpl = createSettingsFetch()

    renderWithApi(<SettingsPage />, fetchImpl)

    expect(await screen.findByRole('heading', { name: 'Configuracoes' })).toBeInTheDocument()
    expect(screen.getByRole('tabpanel', { name: 'Organizacao' })).toHaveTextContent(
      'Perfil da organizacao',
    )

    await user.click(screen.getByRole('tab', { name: /Catalogos/ }))
    expect(screen.getByRole('tabpanel', { name: 'Catalogos' })).toHaveTextContent(
      'Rotulos dos catalogos',
    )
    expect(screen.getByDisplayValue('Apartamento')).toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: /Seguranca/ }))
    expect(screen.getByRole('tabpanel', { name: 'Seguranca' })).toHaveTextContent(
      'Politica de seguranca',
    )

    await user.click(screen.getByRole('tab', { name: /Perfil/ }))
    expect(screen.getByRole('tabpanel', { name: 'Perfil' })).toHaveTextContent('Meu locale')

    await user.click(screen.getByRole('tab', { name: /Organizacao/ }))
    const displayNameInput = screen.getByLabelText(/Nome exibido/)
    await user.clear(displayNameInput)
    await user.type(displayNameInput, 'Moradas Prime')
    await user.click(screen.getAllByRole('button', { name: 'Salvar' })[0])

    await waitFor(() =>
      expect(fetchImpl).toHaveBeenCalledWith(
        expect.stringContaining('/v1/settings/organization'),
        expect.objectContaining({ method: 'PUT' }),
      ),
    )

    const saveCall = vi.mocked(fetchImpl).mock.calls.find(([input, init]) => {
      const url = String(input)

      return url.includes('/v1/settings/organization') && init?.method === 'PUT'
    })
    expect(JSON.parse(String(saveCall?.[1]?.body))).toMatchObject({
      displayName: 'Moradas Prime',
      slug: 'moradas-a',
    })
  })

  it('keeps read-only users away from write and manage settings actions', async () => {
    applyAuthSession(buildSettingsSession(['settings.read']))
    const fetchImpl = createSettingsFetch()

    renderWithApi(<SettingsPage />, fetchImpl)

    expect(await screen.findByRole('heading', { name: 'Configuracoes' })).toBeInTheDocument()
    expect(screen.getByRole('tabpanel', { name: 'Organizacao' })).toHaveTextContent(
      'Perfil da organizacao',
    )

    expect(screen.queryByRole('tab', { name: /Catalogos/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: /Seguranca/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Enviar logo' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Redefinir design' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Salvar' })).not.toBeInTheDocument()
    expect(screen.getByLabelText(/Nome exibido/)).toBeDisabled()
  })
})
