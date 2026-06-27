import type { UseQueryResult } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import type { AppLocale } from '../../stores/useAppPreferencesStore'
import type { ResidentPortalSummary } from '../../lib/api/residentPortal'
import type { getResidentPortalCopy } from './residentPortalCopy'

export type ResidentPortalShellContext = {
  copy: ReturnType<typeof getResidentPortalCopy>
  locale: AppLocale
  query: UseQueryResult<ResidentPortalSummary, Error>
  summary?: ResidentPortalSummary
}

export function useResidentPortalContext() {
  return useOutletContext<ResidentPortalShellContext>()
}
