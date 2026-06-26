import { useMemo, type PropsWithChildren } from 'react'
import { ApiClientContext } from './ApiClientContext'
import { ApiClient } from './client'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'

export function ApiClientProvider({ children }: PropsWithChildren) {
  const client = useMemo(
    () =>
      new ApiClient({
        getAccessToken: () => useAuthSessionStore.getState().accessToken,
        getLocale: () => useAppPreferencesStore.getState().locale,
        getOrganizationId: () => useActiveOrganizationStore.getState().activeOrganization?.id,
        onUnauthorized: () => useAuthSessionStore.getState().signOut(),
      }),
    [],
  )

  return <ApiClientContext.Provider value={client}>{children}</ApiClientContext.Provider>
}
