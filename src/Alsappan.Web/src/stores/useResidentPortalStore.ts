import { create } from 'zustand'

export type ResidentPortalSection =
  | 'contracts'
  | 'documents'
  | 'inspections'
  | 'notifications'
  | 'occurrences'
  | 'overview'
  | 'payments'
  | 'profile'
  | 'property'

export type ResidentPortalState = {
  activeSection: ResidentPortalSection
  closeOccurrenceComposer: () => void
  isOccurrenceComposerOpen: boolean
  openOccurrenceComposer: () => void
  resetResidentPortalState: () => void
  selectedOccurrenceId: string | null
  setActiveSection: (section: ResidentPortalSection) => void
  setUnreadNotificationCount: (count: number) => void
  selectOccurrence: (occurrenceId: string | null) => void
  unreadNotificationCount: number
}

const defaultResidentPortalState = {
  activeSection: 'overview' as const,
  isOccurrenceComposerOpen: false,
  selectedOccurrenceId: null,
  unreadNotificationCount: 0,
}

export const useResidentPortalStore = create<ResidentPortalState>()((set) => ({
  ...defaultResidentPortalState,
  closeOccurrenceComposer: () => set({ isOccurrenceComposerOpen: false }),
  openOccurrenceComposer: () => set({ isOccurrenceComposerOpen: true }),
  resetResidentPortalState: () => set(defaultResidentPortalState),
  selectOccurrence: (selectedOccurrenceId) => set({ selectedOccurrenceId }),
  setActiveSection: (activeSection) => set({ activeSection }),
  setUnreadNotificationCount: (count) =>
    set({
      unreadNotificationCount: Math.max(0, Math.trunc(count)),
    }),
}))
