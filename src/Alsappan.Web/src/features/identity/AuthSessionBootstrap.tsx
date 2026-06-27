import { useEffect, useRef, type PropsWithChildren, type ReactNode } from 'react'
import { LoadingState } from '../../components'
import { getCurrentUser, refreshAuthSession } from '../../lib/api/identity'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { getIdentityCopy } from './identityCopy'
import { applyAuthSession, applyCurrentUser } from './session'

export type AuthSessionBootstrapProps = PropsWithChildren<{
  loadingFallback?: ReactNode
  onBootstrapped?: (isAuthenticated: boolean) => void
}>

export function AuthSessionBootstrap({
  children,
  loadingFallback,
  onBootstrapped,
}: AuthSessionBootstrapProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const accessToken = useAuthSessionStore((state) => state.accessToken)
  const isAuthenticated = useAuthSessionStore((state) => state.isAuthenticated)
  const refreshToken = useAuthSessionStore((state) => state.refreshToken)
  const sessionStatus = useAuthSessionStore((state) => state.sessionStatus)
  const user = useAuthSessionStore((state) => state.user)
  const didBootstrapRef = useRef(false)
  const copy = getIdentityCopy(locale)

  useEffect(() => {
    if (didBootstrapRef.current || (isAuthenticated && user)) {
      return undefined
    }

    let isCurrent = true
    didBootstrapRef.current = true
    useAuthSessionStore.getState().beginBootstrap()

    async function bootstrapSession() {
      try {
        if (accessToken) {
          const currentUser = await getCurrentUser(apiClient)

          if (isCurrent) {
            applyCurrentUser(currentUser)
            onBootstrapped?.(true)
          }

          return
        }

        if (!refreshToken) {
          if (isCurrent) {
            useAuthSessionStore.getState().completeUnauthenticated()
            onBootstrapped?.(false)
          }

          return
        }

        const session = await refreshAuthSession(apiClient, { refreshToken })

        if (isCurrent) {
          applyAuthSession(session)
          onBootstrapped?.(true)
        }
      } catch (error) {
        if (isCurrent) {
          const message = error instanceof Error ? error.message : undefined
          useAuthSessionStore.getState().failBootstrap(message)
          onBootstrapped?.(false)
        }
      }
    }

    void bootstrapSession()

    return () => {
      isCurrent = false
    }
  }, [accessToken, apiClient, isAuthenticated, onBootstrapped, refreshToken, user])

  if (sessionStatus === 'bootstrapping') {
    return (
      <>
        {loadingFallback ?? (
          <LoadingState
            title={copy.bootstrap.loading}
            description={copy.bootstrap.loadingDescription}
          />
        )}
      </>
    )
  }

  return <>{children}</>
}
