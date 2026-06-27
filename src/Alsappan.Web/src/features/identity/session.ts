import type { AuthSessionDto, CurrentUserDto } from '../../lib/api/identity'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'

function syncActiveOrganizations(user: CurrentUserDto) {
  useActiveOrganizationStore
    .getState()
    .setOrganizationsFromMemberships(user.organizations, user.activeOrganizationId)
}

export function applyAuthSession(session: AuthSessionDto) {
  useAuthSessionStore.getState().setSession(session)
  syncActiveOrganizations(session.user)
}

export function applyCurrentUser(user: CurrentUserDto) {
  useAuthSessionStore.getState().setCurrentUser(user)
  syncActiveOrganizations(user)
}

export function clearIdentitySession() {
  useAuthSessionStore.getState().signOut()
}

export function getActivePermissionCodes(user = useAuthSessionStore.getState().user) {
  if (!user) {
    return []
  }

  const activeOrganization = user.organizations.find(
    (organization) => organization.id === user.activeOrganizationId,
  )

  return Array.from(
    new Set([...(user.permissions ?? []), ...(activeOrganization?.permissionCodes ?? [])]),
  )
}

export function hasEveryPermission(
  requiredPermissions: string[],
  user = useAuthSessionStore.getState().user,
) {
  if (requiredPermissions.length === 0) {
    return true
  }

  const permissionCodes = getActivePermissionCodes(user)

  return requiredPermissions.every((permission) => permissionCodes.includes(permission))
}

export function hasAnyPermission(
  requiredPermissions: string[],
  user = useAuthSessionStore.getState().user,
) {
  if (requiredPermissions.length === 0) {
    return true
  }

  const permissionCodes = getActivePermissionCodes(user)

  return requiredPermissions.some((permission) => permissionCodes.includes(permission))
}
