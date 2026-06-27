import { create } from 'zustand'
import type { AuthSessionDto, CurrentUserDto } from '../lib/api/identity'

export type AuthSessionStatus =
  | 'authenticated'
  | 'bootstrapping'
  | 'error'
  | 'idle'
  | 'unauthenticated'

export type AuthSessionUser = CurrentUserDto & {
  name: string
  roles: string[]
}

export type AuthSessionState = {
  accessToken?: string
  bootstrapError: string | null
  clearIntendedPath: () => void
  completeUnauthenticated: () => void
  expiresAt?: string
  failBootstrap: (message?: string) => void
  beginBootstrap: () => void
  intendedPath: string | null
  isAuthenticated: boolean
  refreshToken?: string
  sessionStatus: AuthSessionStatus
  setCurrentUser: (user: CurrentUserDto) => void
  setIntendedPath: (intendedPath: string | null) => void
  setSession: (session: AuthSessionDto) => void
  signInDemo: () => void
  signOut: () => void
  tokenType?: string
  user: AuthSessionUser | null
}

export const demoAuthSessionUser: AuthSessionUser = {
  accountType: 'admin',
  activeOrganizationId: 'demo-alsappan',
  displayName: 'Demo Administrator',
  email: 'admin@alsappan.local',
  id: 'demo-admin',
  name: 'Demo Administrator',
  organizations: [
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
  ],
  permissions: ['administrators.read', 'administrators.write'],
  roles: ['administrator'],
}

function getActiveRoleCodes(user: CurrentUserDto) {
  return (
    user.organizations.find((organization) => organization.id === user.activeOrganizationId)
      ?.roleCodes ?? []
  )
}

function normalizeSessionUser(user: CurrentUserDto): AuthSessionUser {
  return {
    ...user,
    name: user.displayName,
    roles: getActiveRoleCodes(user),
  }
}

export const useAuthSessionStore = create<AuthSessionState>()((set) => ({
  accessToken: undefined,
  bootstrapError: null,
  beginBootstrap: () => set({ bootstrapError: null, sessionStatus: 'bootstrapping' }),
  clearIntendedPath: () => set({ intendedPath: null }),
  completeUnauthenticated: () =>
    set({
      accessToken: undefined,
      bootstrapError: null,
      expiresAt: undefined,
      isAuthenticated: false,
      refreshToken: undefined,
      sessionStatus: 'unauthenticated',
      tokenType: undefined,
      user: null,
    }),
  expiresAt: undefined,
  failBootstrap: (message = 'Session bootstrap failed') =>
    set({
      accessToken: undefined,
      bootstrapError: message,
      expiresAt: undefined,
      isAuthenticated: false,
      refreshToken: undefined,
      sessionStatus: 'error',
      tokenType: undefined,
      user: null,
    }),
  intendedPath: null,
  isAuthenticated: false,
  refreshToken: undefined,
  sessionStatus: 'idle',
  setCurrentUser: (user) =>
    set({
      bootstrapError: null,
      isAuthenticated: true,
      sessionStatus: 'authenticated',
      user: normalizeSessionUser(user),
    }),
  setIntendedPath: (intendedPath) => set({ intendedPath }),
  setSession: (session) =>
    set({
      accessToken: session.accessToken,
      bootstrapError: null,
      expiresAt: session.expiresAt,
      isAuthenticated: true,
      refreshToken: session.refreshToken,
      sessionStatus: 'authenticated',
      tokenType: session.tokenType,
      user: normalizeSessionUser(session.user),
    }),
  signInDemo: () =>
    set({
      accessToken: 'demo-access-token',
      bootstrapError: null,
      expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      isAuthenticated: true,
      refreshToken: 'demo-refresh-token',
      sessionStatus: 'authenticated',
      tokenType: 'Bearer',
      user: demoAuthSessionUser,
    }),
  signOut: () =>
    set({
      accessToken: undefined,
      bootstrapError: null,
      expiresAt: undefined,
      intendedPath: null,
      isAuthenticated: false,
      refreshToken: undefined,
      sessionStatus: 'unauthenticated',
      tokenType: undefined,
      user: null,
    }),
  tokenType: undefined,
  user: null,
}))
