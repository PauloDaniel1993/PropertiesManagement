import { create } from 'zustand'

export type ShellState = {
  activeRouteKey: string | null
  closeMobileNavigation: () => void
  isMobileNavigationOpen: boolean
  isSidebarCollapsed: boolean
  openMobileNavigation: () => void
  resetShellState: () => void
  setActiveRouteKey: (routeKey: string | null) => void
  setSidebarCollapsed: (isSidebarCollapsed: boolean) => void
  toggleSidebarCollapsed: () => void
}

const defaultShellState = {
  activeRouteKey: null,
  isMobileNavigationOpen: false,
  isSidebarCollapsed: false,
}

export const useShellStore = create<ShellState>()((set) => ({
  ...defaultShellState,
  closeMobileNavigation: () => set({ isMobileNavigationOpen: false }),
  openMobileNavigation: () => set({ isMobileNavigationOpen: true }),
  resetShellState: () => set(defaultShellState),
  setActiveRouteKey: (activeRouteKey) => set({ activeRouteKey }),
  setSidebarCollapsed: (isSidebarCollapsed) => set({ isSidebarCollapsed }),
  toggleSidebarCollapsed: () =>
    set((state) => ({
      isSidebarCollapsed: !state.isSidebarCollapsed,
    })),
}))
