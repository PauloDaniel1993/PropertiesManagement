import { create } from 'zustand'
import { defaultLocale, normalizeAppLocale, type AppLocale } from '../i18n'
import type { OrganizationMembershipSummaryDto } from '../lib/api/identity'
import type { OrganizationBranding } from '../lib/branding'

export type OrganizationSummary = {
  branding?: OrganizationBranding
  currencyCode: string
  displayName: string
  id: string
  locale: AppLocale
  name: string
  permissionCodes?: string[]
  roleCodes?: string[]
  slug: string
}

export type ActiveOrganizationState = {
  activeOrganization: OrganizationSummary | null
  activeOrganizationId: string | null
  organizations: OrganizationSummary[]
  setActiveOrganizationId: (organizationId: string | null) => void
  setOrganizations: (organizations: OrganizationSummary[], activeOrganizationId?: string) => void
  setOrganizationsFromMemberships: (
    organizations: OrganizationMembershipSummaryDto[],
    activeOrganizationId?: string | null,
  ) => void
}

export const defaultOrganizations: OrganizationSummary[] = [
  {
    currencyCode: 'BRL',
    displayName: 'Alsappan',
    id: 'demo-alsappan',
    locale: 'pt-BR',
    name: 'Alsappan',
    permissionCodes: ['administrators.read', 'administrators.write'],
    roleCodes: ['Administrador'],
    slug: 'alsappan',
  },
]

function findOrganization(organizations: OrganizationSummary[], organizationId: string | null) {
  return organizations.find((organization) => organization.id === organizationId) ?? null
}

function normalizeOrganization(
  organization: OrganizationMembershipSummaryDto,
): OrganizationSummary {
  return {
    branding: organization.branding,
    currencyCode: organization.currencyCode ?? organization.currency ?? 'BRL',
    displayName: organization.displayName,
    id: organization.id,
    locale: normalizeAppLocale(organization.locale, defaultLocale),
    name: organization.name,
    permissionCodes: organization.permissionCodes,
    roleCodes: organization.roleCodes,
    slug: organization.slug,
  }
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
  setOrganizationsFromMemberships: (organizations, activeOrganizationId) =>
    set((state) => {
      const normalizedOrganizations = organizations.map(normalizeOrganization)
      const nextActiveOrganization =
        findOrganization(
          normalizedOrganizations,
          activeOrganizationId ?? state.activeOrganizationId,
        ) ??
        normalizedOrganizations[0] ??
        null

      return {
        activeOrganization: nextActiveOrganization,
        activeOrganizationId: nextActiveOrganization?.id ?? null,
        organizations: normalizedOrganizations,
      }
    }),
}))
