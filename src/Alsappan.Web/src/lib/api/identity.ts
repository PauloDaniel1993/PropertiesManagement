import type { AppLocale } from '../../i18n'
import type { OrganizationBranding } from '../branding'
import type { ApiClient } from './client'

export type AccountType = 'admin' | 'resident'

export type OrganizationMembershipSummaryDto = {
  branding?: OrganizationBranding
  currency?: string
  currencyCode?: string
  displayName: string
  id: string
  locale?: AppLocale
  name: string
  permissionCodes: string[]
  roleCodes: string[]
  slug: string
}

export type CurrentUserDto = {
  accountType: AccountType
  activeOrganizationId: string | null
  displayName: string
  email: string
  id: string
  organizations: OrganizationMembershipSummaryDto[]
  permissions: string[]
}

export type AuthSessionDto = {
  accessToken: string
  expiresAt: string
  refreshToken?: string
  tokenType: string
  user: CurrentUserDto
}

export type LoginRequest = {
  email: string
  organizationId?: string
  password: string
}

export type RefreshSessionRequest = {
  refreshToken?: string
}

export type LogoutRequest = {
  refreshToken?: string
}

export type SwitchOrganizationRequest = {
  organizationId: string
}

function compactLoginRequest(request: LoginRequest): LoginRequest {
  return {
    email: request.email,
    password: request.password,
    ...(request.organizationId ? { organizationId: request.organizationId } : {}),
  }
}

export function loginAdmin(client: ApiClient, request: LoginRequest) {
  return client.post<AuthSessionDto, LoginRequest>(
    '/v1/auth/admin/login',
    compactLoginRequest(request),
  )
}

export function loginResident(client: ApiClient, request: LoginRequest) {
  return client.post<AuthSessionDto, LoginRequest>(
    '/v1/auth/resident/login',
    compactLoginRequest(request),
  )
}

export function refreshAuthSession(client: ApiClient, request: RefreshSessionRequest = {}) {
  return client.post<AuthSessionDto, RefreshSessionRequest>('/v1/auth/refresh', request)
}

export function logoutAuthSession(client: ApiClient, request: LogoutRequest = {}) {
  return client.post<void, LogoutRequest>('/v1/auth/logout', request)
}

export function getCurrentUser(client: ApiClient) {
  return client.get<CurrentUserDto>('/v1/auth/me')
}

export function switchActiveOrganization(client: ApiClient, request: SwitchOrganizationRequest) {
  return client.post<AuthSessionDto, SwitchOrganizationRequest>(
    '/v1/auth/switch-organization',
    request,
  )
}
