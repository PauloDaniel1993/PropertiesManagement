import { create } from 'zustand'
import type { AppLocale } from '../i18n'
import type { OrganizationBranding } from '../lib/branding'

export type OrganizationSummary = {
  branding?: OrganizationBranding
  currencyCode: string
  displayName: string
  id: string
  locale: AppLocale
  name: string
  slug: string
}

export type ActiveOrganizationState = {
  activeOrganization: OrganizationSummary | null
  activeOrganizationId: string | null
  organizations: OrganizationSummary[]
  setActiveOrganizationId: (organizationId: string | null) => void
  setOrganizations: (organizations: OrganizationSummary[], activeOrganizationId?: string) => void
}

export const defaultOrganizations: OrganizationSummary[] = [
  {
    currencyCode: 'BRL',
    displayName: 'Alsappan',
    id: 'demo-alsappan',
    locale: 'pt-BR',
    name: 'Alsappan',
    slug: 'alsappan',
  },
]

function findOrganization(organizations: OrganizationSummary[], organizationId: string | null) {
  return organizations.find((organization) => organization.id === organizationId) ?? null
}

export const useActiveOrganizationStore = create<ActiveOrganizationState>()((set) => ({
  activeOrganization: defaultOrganizations[0],
  activeOrganizationId: defaultOrganizations[0]?.id ?? null,
  organizations: defaultOrganizations,
  setActiveOrganizationId: (organizationId) =>
    set((state) => {
      const activeOrganization = findOrganization(state.organizations, organizationId)

      return {
        activeOrganization,
        activeOrganizationId: activeOrganization?.id ?? null,
      }
    }),
  setOrganizations: (organizations, activeOrganizationId) =>
    set((state) => {
      const nextActiveOrganization =
        findOrganization(organizations, activeOrganizationId ?? state.activeOrganizationId) ??
        organizations[0] ??
        null

      return {
        activeOrganization: nextActiveOrganization,
        activeOrganizationId: nextActiveOrganization?.id ?? null,
        organizations,
      }
    }),
}))
