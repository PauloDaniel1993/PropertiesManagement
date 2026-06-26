import { createContext, useContext } from 'react'
import type { ApiClient } from './client'

export const ApiClientContext = createContext<ApiClient | undefined>(undefined)

export function useApiClient() {
  const client = useContext(ApiClientContext)

  if (!client) {
    throw new Error('useApiClient must be used within ApiClientProvider')
  }

  return client
}
