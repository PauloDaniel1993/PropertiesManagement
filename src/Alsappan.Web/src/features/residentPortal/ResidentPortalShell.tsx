import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Bell,
  Building2,
  ClipboardList,
  CreditCard,
  FileText,
  Home,
  Languages,
  LogOut,
  Menu,
  Moon,
  ScrollText,
  Sun,
  UserCircle,
  Wrench,
  X,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { LoadingState } from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { logoutAuthSession } from '../../lib/api/identity'
import { getResidentPortalSummary, type ResidentPortalSummary } from '../../lib/api/residentPortal'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import {
  useResidentPortalStore,
  type ResidentPortalSection,
} from '../../stores/useResidentPortalStore'
import { useShellStore } from '../../stores/useShellStore'
import { getResidentPortalCopy } from './residentPortalCopy'

type ResidentNavItem = {
  icon: typeof Home
  id: ResidentPortalSection
  path: string
}

const navItems: ResidentNavItem[] = [
  { icon: Home, id: 'overview', path: '/portal' },
  { icon: UserCircle, id: 'profile', path: '/portal/perfil' },
  { icon: Building2, id: 'property', path: '/portal/imovel' },
  { icon: ScrollText, id: 'contracts', path: '/portal/contratos' },
  { icon: CreditCard, id: 'payments', path: '/portal/pagamentos' },
  { icon: FileText, id: 'documents', path: '/portal/documentos' },
  { icon: Wrench, id: 'occurrences', path: '/portal/ocorrencias' },
  { icon: ClipboardList, id: 'inspections', path: '/portal/vistorias' },
  { icon: Bell, id: 'notifications', path: '/portal/notificacoes' },
]

function findActiveItem(pathname: string) {
  return [...navItems]
    .reverse()
    .find((item) => pathname === item.path || pathname.startsWith(`${item.path}/`))
}

export function ResidentPortalShell() {
  const apiClient = useApiClient()
  const { i18n } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()
  const activeOrganization = useActiveOrganizationStore((state) => state.activeOrganization)
  const activeOrganizationId = useActiveOrganizationStore((state) => state.activeOrganizationId)
  const { locale, setLocale, theme, toggleTheme } = useAppPreferencesStore()
  const refreshToken = useAuthSessionStore((state) => state.refreshToken)
  const signOut = useAuthSessionStore((state) => state.signOut)
  const user = useAuthSessionStore((state) => state.user)
  const setActiveSection = useResidentPortalStore((state) => state.setActiveSection)
  const setUnreadNotificationCount = useResidentPortalStore(
    (state) => state.setUnreadNotificationCount,
  )
  const {
    closeMobileNavigation,
    isMobileNavigationOpen,
    isSidebarCollapsed,
    openMobileNavigation,
    toggleSidebarCollapsed,
  } = useShellStore()
  const copy = getResidentPortalCopy(locale)
  const activeItem = findActiveItem(location.pathname) ?? navItems[0]
  const nextLocale = locale === 'pt-BR' ? 'en-US' : 'pt-BR'
  const summaryQuery = useQuery<ResidentPortalSummary, Error>({
    queryFn: () => getResidentPortalSummary(apiClient, locale),
    queryKey: ['resident-portal', activeOrganizationId, 'summary', locale],
    staleTime: 30_000,
  })
  const unreadNotificationCount =
    summaryQuery.data?.notifications.filter((notification) => !notification.isRead).length ?? 0

  useEffect(() => {
    setActiveSection(activeItem.id)
  }, [activeItem.id, setActiveSection])

  useEffect(() => {
    setUnreadNotificationCount(unreadNotificationCount)
  }, [setUnreadNotificationCount, unreadNotificationCount])

  function handleLocaleToggle() {
    setLocale(nextLocale)
    void i18n.changeLanguage(nextLocale)
  }

  async function handleSignOut() {
    try {
      await logoutAuthSession(apiClient, { refreshToken })
    } finally {
      signOut()
      navigate('/resident-login', { replace: true })
    }
  }

  return (
    <div
      className="admin-app resident-portal-app"
      data-sidebar={isSidebarCollapsed ? 'collapsed' : 'expanded'}
      data-theme={theme}
    >
      <button
        aria-label={copy.shell.menu}
        className="mobile-nav-button"
        onClick={openMobileNavigation}
        type="button"
      >
        <Menu size={20} />
      </button>

      <aside className="admin-sidebar" data-mobile-open={isMobileNavigationOpen}>
        <div className="admin-sidebar__brand">
          <span aria-hidden="true" className="brand-symbol">
            <Building2 size={24} strokeWidth={2.25} />
          </span>
          <div>
            <strong>{copy.shell.brandName}</strong>
            <small>{copy.shell.brandSubtitle}</small>
          </div>
        </div>

        <nav aria-label={copy.shell.menu} className="admin-sidebar__nav">
          {navItems.map((item) => {
            const Icon = item.icon
            return (
              <NavLink
                end={item.id === 'overview'}
                className={({ isActive }) =>
                  isActive ? 'admin-nav-link is-active' : 'admin-nav-link'
                }
                key={item.id}
                onClick={closeMobileNavigation}
                title={copy.nav[item.id]}
                to={item.path}
              >
                <Icon aria-hidden="true" size={20} strokeWidth={2.1} />
                <span>{copy.nav[item.id]}</span>
              </NavLink>
            )
          })}
        </nav>

        <button
          aria-label={isSidebarCollapsed ? copy.shell.menu : copy.actions.close}
          className="admin-sidebar__collapse"
          onClick={toggleSidebarCollapsed}
          type="button"
        >
          <Menu size={18} />
          <span>{copy.shell.menu}</span>
        </button>

        <button
          aria-label={copy.actions.close}
          className="admin-sidebar__close"
          onClick={closeMobileNavigation}
          type="button"
        >
          <X size={20} />
        </button>
      </aside>

      <div className="admin-workspace">
        <header className="admin-topbar">
          <div className="admin-topbar__title">
            <span>{activeOrganization?.displayName ?? copy.shell.account}</span>
            <strong>{copy.nav[activeItem.id]}</strong>
          </div>

          <div className="admin-topbar__actions">
            <button aria-label={copy.shell.localeToggle} onClick={handleLocaleToggle} type="button">
              <Languages size={18} />
            </button>
            <button aria-label={copy.shell.themeToggle} onClick={toggleTheme} type="button">
              {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
            </button>
            <details className="notification-menu">
              <summary aria-label={copy.shell.notifications} className="notification-button">
                <Bell size={18} />
                {unreadNotificationCount > 0 ? (
                  <span>{unreadNotificationCount > 99 ? '99+' : unreadNotificationCount}</span>
                ) : null}
              </summary>
              <div className="notification-menu__content" role="menu">
                <div className="notification-menu__header">
                  <strong>{copy.shell.notifications}</strong>
                  <small>{unreadNotificationCount}</small>
                </div>
                {(summaryQuery.data?.notifications ?? []).slice(0, 5).map((notification) => (
                  <NavLink key={notification.id} role="menuitem" to="/portal/notificacoes">
                    <strong>{notification.title}</strong>
                    <span>{notification.message}</span>
                  </NavLink>
                ))}
              </div>
            </details>
            <details className="profile-menu">
              <summary aria-label={copy.shell.account}>
                <UserCircle size={18} />
                <span>{user?.displayName ?? user?.email}</span>
              </summary>
              <div className="profile-menu__content">
                <strong>{user?.displayName ?? copy.shell.account}</strong>
                <span>{user?.email}</span>
                <button onClick={() => void handleSignOut()} type="button">
                  <LogOut size={16} />
                  {copy.shell.signOut}
                </button>
              </div>
            </details>
          </div>
        </header>

        <main className="admin-content" id="main-content">
          {summaryQuery.isLoading ? (
            <LoadingState title={copy.loading} />
          ) : (
            <Outlet
              context={{
                copy,
                locale,
                query: summaryQuery,
                summary: summaryQuery.data,
              }}
            />
          )}
        </main>
      </div>
    </div>
  )
}
