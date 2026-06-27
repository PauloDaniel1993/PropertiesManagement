import { useEffect, type PropsWithChildren, type ReactNode } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { ForbiddenState, LoadingState } from '../../components'
import { getIdentityCopy } from '../../features/identity/identityCopy'
import { hasAnyPermission, hasEveryPermission } from '../../features/identity/session'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore, type AuthSessionUser } from '../../stores/useAuthSessionStore'

type GuardProps = PropsWithChildren<{
  fallback?: ReactNode
}>

type PermissionMode = 'all' | 'any'

export type PermissionGuardProps = GuardProps & {
  mode?: PermissionMode
  permissions: string[]
}

function GuardContent({ children }: PropsWithChildren) {
  return children ? <>{children}</> : <Outlet />
}

function getPermissionResult(
  permissions: string[],
  mode: PermissionMode,
  user: AuthSessionUser | null,
) {
  return mode === 'all'
    ? hasEveryPermission(permissions, user)
    : hasAnyPermission(permissions, user)
}

export function RequireAuthenticated({ children }: GuardProps) {
  const location = useLocation()
  const locale = useAppPreferencesStore((state) => state.locale)
  const isAuthenticated = useAuthSessionStore((state) => state.isAuthenticated)
  const sessionStatus = useAuthSessionStore((state) => state.sessionStatus)
  const setIntendedPath = useAuthSessionStore((state) => state.setIntendedPath)
  const copy = getIdentityCopy(locale)

  useEffect(() => {
    if (!isAuthenticated) {
      setIntendedPath(`${location.pathname}${location.search}`)
    }
  }, [isAuthenticated, location.pathname, location.search, setIntendedPath])

  if (sessionStatus === 'bootstrapping') {
    return (
      <LoadingState
        title={copy.bootstrap.loading}
        description={copy.bootstrap.loadingDescription}
      />
    )
  }

  if (!isAuthenticated) {
    return <Navigate replace to="/login" />
  }

  return <GuardContent>{children}</GuardContent>
}

export function RequireAdminRoute({ children, fallback }: GuardProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const user = useAuthSessionStore((state) => state.user)
  const copy = getIdentityCopy(locale)

  if (user?.accountType !== 'admin') {
    return (
      <>
        {fallback ?? (
          <ForbiddenState
            title={copy.guards.adminOnlyTitle}
            description={copy.guards.adminOnlyDescription}
          />
        )}
      </>
    )
  }

  return <GuardContent>{children}</GuardContent>
}

export function RequireResidentRoute({ children, fallback }: GuardProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const user = useAuthSessionStore((state) => state.user)
  const copy = getIdentityCopy(locale)

  if (user?.accountType !== 'resident') {
    return (
      <>
        {fallback ?? (
          <ForbiddenState
            title={copy.guards.residentOnlyTitle}
            description={copy.guards.residentOnlyDescription}
          />
        )}
      </>
    )
  }

  return <GuardContent>{children}</GuardContent>
}

export function RequireActiveOrganization({ children, fallback }: GuardProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const activeOrganization = useActiveOrganizationStore((state) => state.activeOrganization)
  const copy = getIdentityCopy(locale)

  if (!activeOrganization) {
    return (
      <>
        {fallback ?? (
          <ForbiddenState
            title={copy.guards.missingOrganizationTitle}
            description={copy.guards.missingOrganizationDescription}
          />
        )}
      </>
    )
  }

  return <GuardContent>{children}</GuardContent>
}

export function RequirePermission({
  children,
  fallback,
  mode = 'all',
  permissions,
}: PermissionGuardProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const user = useAuthSessionStore((state) => state.user)
  const copy = getIdentityCopy(locale)
  const canAccess = getPermissionResult(permissions, mode, user)

  if (!canAccess) {
    return (
      <>
        {fallback ?? (
          <ForbiddenState
            title={copy.guards.forbiddenTitle}
            description={copy.guards.forbiddenDescription}
          />
        )}
      </>
    )
  }

  return <GuardContent>{children}</GuardContent>
}

export function PermissionGate({
  children,
  fallback = null,
  mode = 'all',
  permissions,
}: PermissionGuardProps) {
  const user = useAuthSessionStore((state) => state.user)

  return getPermissionResult(permissions, mode, user) ? <>{children}</> : <>{fallback}</>
}
