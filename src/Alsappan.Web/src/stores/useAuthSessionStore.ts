import { create } from 'zustand'

export type AuthSessionUser = {
  displayName: string
  email: string
  id: string
  name: string
  roles: string[]
}

export type AuthSessionState = {
  accessToken?: string
  clearIntendedPath: () => void
  intendedPath: string | null
  isAuthenticated: boolean
  setIntendedPath: (intendedPath: string | null) => void
  signInDemo: () => void
  signOut: () => void
  user: AuthSessionUser | null
}

export const demoAuthSessionUser: AuthSessionUser = {
  displayName: 'Demo Administrator',
  email: 'admin@alsappan.local',
  id: 'demo-admin',
  name: 'Demo Administrator',
  roles: ['administrator'],
}

export const useAuthSessionStore = create<AuthSessionState>()((set) => ({
  accessToken: undefined,
  clearIntendedPath: () => set({ intendedPath: null }),
  intendedPath: null,
  isAuthenticated: false,
  setIntendedPath: (intendedPath) => set({ intendedPath }),
  signInDemo: () =>
    set({
      accessToken: 'demo-access-token',
      isAuthenticated: true,
      user: demoAuthSessionUser,
    }),
  signOut: () =>
    set({
      accessToken: undefined,
      intendedPath: null,
      isAuthenticated: false,
      user: null,
    }),
  user: null,
}))
