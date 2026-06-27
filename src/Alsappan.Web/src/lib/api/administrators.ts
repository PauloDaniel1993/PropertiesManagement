import type { ApiClient } from './client'
import type { ApiPagedResult, ApiSelectOption } from './contracts'

export type AdministratorStatus = 'active' | 'archived' | 'inactive' | 'invited' | 'locked'

export type AdministratorListFilters = {
  page?: number
  pageSize?: number
  role?: string
  search?: string
  status?: AdministratorStatus | ''
}

export type AdministratorListItem = {
  archivedAt?: string
  displayName: string
  email: string
  id: string
  invitedAt?: string
  lastLoginAt?: string
  roleCodes: string[]
  roleLabels?: string[]
  status: AdministratorStatus
  statusLabel?: string
  updatedAt?: string
}

export type AdministratorDetail = AdministratorListItem & {
  createdAt?: string
  permissionCodes?: string[]
}

export type AdministratorFormRequest = {
  displayName: string
  email: string
  roleCodes: string[]
  temporaryPassword?: string
}

export function listAdministrators(client: ApiClient, filters: AdministratorListFilters = {}) {
  return client.get<ApiPagedResult<AdministratorListItem>>('/v1/administrators', {
    query: {
      page: filters.page,
      pageSize: filters.pageSize,
      role: filters.role,
      search: filters.search,
      status: filters.status,
    },
  })
}

export function getAdministrator(client: ApiClient, administratorId: string) {
  return client.get<AdministratorDetail>(`/v1/administrators/${administratorId}`)
}

export function createAdministrator(client: ApiClient, request: AdministratorFormRequest) {
  return client.post<AdministratorDetail, AdministratorFormRequest>('/v1/administrators', request)
}

export function updateAdministrator(
  client: ApiClient,
  administratorId: string,
  request: AdministratorFormRequest,
) {
  return client.put<AdministratorDetail, AdministratorFormRequest>(
    `/v1/administrators/${administratorId}`,
    request,
  )
}

export function deactivateAdministrator(client: ApiClient, administratorId: string) {
  return client.post<AdministratorDetail, Record<string, never>>(
    `/v1/administrators/${administratorId}/deactivate`,
    {},
  )
}

export function reactivateAdministrator(client: ApiClient, administratorId: string) {
  return client.post<AdministratorDetail, Record<string, never>>(
    `/v1/administrators/${administratorId}/reactivate`,
    {},
  )
}

export function archiveAdministrator(client: ApiClient, administratorId: string) {
  return client.delete<void>(`/v1/administrators/${administratorId}`)
}

export function listAdministratorRoleOptions(client: ApiClient) {
  return client.get<ApiSelectOption[]>('/v1/administrators/role-options')
}
