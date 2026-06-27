import { useQuery } from '@tanstack/react-query'
import {
  Bell,
  Building2,
  Languages,
  LogOut,
  Menu,
  Moon,
  PanelLeftClose,
  PanelLeftOpen,
  Settings,
  Sun,
  UserCircle,
  X,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { OrganizationSwitcher } from '../features/identity'
import { hasAnyPermission } from '../features/identity/session'
import { GlobalSearch } from '../features/search'
import { useApiClient } from '../lib/api/ApiClientContext'
import { logoutAuthSession } from '../lib/api/identity'
import { getNotificationUnreadCount, listNotifications } from '../lib/api/notifications'
import { adminMenuItems, findAdminMenuItemByPath } from '../navigation/menuContract'
import { menuIconComponents } from '../navigation/menuIcons'
import { useActiveOrganizationStore } from '../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../stores/useAuthSessionStore'
import { useShellStore } from '../stores/useShellStore'

export function AdminShell() {
  const apiClient = useApiClient()
  const { i18n, t } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()
  const activeItem = findAdminMenuItemByPath(location.pathname)
  const activeOrganizationId = useActiveOrganizationStore((state) => state.activeOrganizationId)
  const { locale, setLocale, theme, toggleTheme } = useAppPreferencesStore()
  const user = useAuthSessionStore((state) => state.user)
  const refreshToken = useAuthSessionStore((state) => state.refreshToken)
  const signOut = useAuthSessionStore((state) => state.signOut)
  const {
    closeMobileNavigation,
    isMobileNavigationOpen,
    isSidebarCollapsed,
    openMobileNavigation,
    toggleSidebarCollapsed,
  } = useShellStore()
  const nextLocale = locale === 'pt-BR' ? 'en-US' : 'pt-BR'
  const canReadNotifications = hasAnyPermission(['notifications.read'], user)
  const notificationUnreadCountQuery = useQuery({
    enabled: canReadNotifications,
    queryFn: () => getNotificationUnreadCount(apiClient),
    queryKey: ['notifications', activeOrganizationId, 'unread-count'],
    refetchInterval: 60_000,
    staleTime: 30_000,
  })
  const notificationPreviewQuery = useQuery({
    enabled: canReadNotifications,
    queryFn: () =>
      listNotifications(apiClient, {
        isRead: false,
        locale,
        page: 1,
        pageSize: 5,
      }),
    queryKey: ['notifications', activeOrganizationId, 'topbar-preview', locale],
    staleTime: 30_000,
  })
  const unreadNotificationCount = canReadNotifications
    ? (notificationUnreadCountQuery.data?.count ?? 0)
    : 0
  const notificationPreviewItems = notificationPreviewQuery.data?.items ?? []
  const visibleMenuItems = adminMenuItems.filter((item) =>
    hasAnyPermission([item.requiredPermission], user),
  )

  function handleLocaleToggle() {
    setLocale(nextLocale)
    void i18n.changeLanguage(nextLocale)
  }

  async function handleSignOut() {
    try {
      await logoutAuthSession(apiClient, { refreshToken })
    } finally {
      signOut()
      navigate('/login', { replace: true })
    }
  }

  return (
    <div
      className="admin-app"
      data-sidebar={isSidebarCollapsed ? 'collapsed' : 'expanded'}
      data-theme={theme}
    >
      <button
        aria-label={t('shell.topbar.openNavigation')}
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
            <strong>{t('shell.brand.name')}</strong>
            <small>{t('shell.brand.subtitle')}</small>
          </div>
        </div>

        <nav aria-label={t('shell.navigation.admin')} className="admin-sidebar__nav">
          {visibleMenuItems.map((item) => {
            const Icon = menuIconComponents[item.iconId]

            return (
              <NavLink
                className={({ isActive }) =>
                  isActive ? 'admin-nav-link is-active' : 'admin-nav-link'
                }
                key={item.id}
                onClick={closeMobileNavigation}
                title={t(item.menuLabelKey)}
                to={item.path}
              >
                <Icon aria-hidden="true" size={20} strokeWidth={2.1} />
                <span>{t(item.menuLabelKey)}</span>
              </NavLink>
            )
          })}
        </nav>

        <button
          aria-label={
            isSidebarCollapsed ? t('shell.topbar.expandSidebar') : t('shell.topbar.collapseSidebar')
          }
          className="admin-sidebar__collapse"
          onClick={toggleSidebarCollapsed}
          type="button"
        >
          {isSidebarCollapsed ? <PanelLeftOpen size={18} /> : <PanelLeftClose size={18} />}
          <span>{isSidebarCollapsed ? t('shell.topbar.expand') : t('shell.topbar.collapse')}</span>
        </button>

        <button
          aria-label={t('shell.topbar.closeNavigation')}
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
            <span>{t('shell.areaLabel')}</span>
            <strong>{activeItem ? t(activeItem.menuLabelKey) : t('shell.brand.name')}</strong>
          </div>

          <GlobalSearch />

          <OrganizationSwitcher className="organization-select" />

          <div className="admin-topbar__actions">
            <button
              aria-label={t('shell.locale.toggle')}
              onClick={handleLocaleToggle}
              type="button"
            >
              <Languages size={18} />
            </button>
            <button aria-label={t('shell.theme.toggle')} onClick={toggleTheme} type="button">
              {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
            </button>
            <details className="notification-menu">
              <summary
                aria-label={t('shell.topbar.unreadNotifications', {
                  count: unreadNotificationCount,
                })}
                className="notification-button"
              >
                <Bell size={18} />
                {unreadNotificationCount > 0 ? (
                  <span
                    aria-label={t('shell.topbar.unreadNotifications', {
                      count: unreadNotificationCount,
                    })}
                  >
                    {unreadNotificationCount > 99 ? '99+' : unreadNotificationCount}
                  </span>
                ) : null}
              </summary>
              <div className="notification-menu__content" role="menu">
                <div className="notification-menu__header">
                  <strong>{t('shell.topbar.notifications')}</strong>
                  <small>
                    {t('shell.topbar.unreadNotifications', {
                      count: unreadNotificationCount,
                    })}
                  </small>
                </div>

                {!canReadNotifications ? (
                  <p>{t('shell.topbar.notificationsForbidden')}</p>
                ) : notificationPreviewQuery.isLoading ? (
                  <p>{t('shell.topbar.notificationsLoading')}</p>
                ) : notificationPreviewQuery.isError ? (
                  <p>{t('shell.states.error')}</p>
                ) : notificationPreviewItems.length === 0 ? (
                  <p>{t('shell.topbar.noUnreadNotifications')}</p>
                ) : (
                  notificationPreviewItems.map((notification) => (
                    <button
                      key={notification.id}
                      onClick={() => navigate(notification.deepLink ?? '/notificacoes')}
                      role="menuitem"
                      type="button"
                    >
                      <strong>{notification.title}</strong>
                      <span>{notification.message}</span>
                    </button>
                  ))
                )}

                <button
                  className="notification-menu__all"
                  onClick={() => navigate('/notificacoes')}
                  role="menuitem"
                  type="button"
                >
                  {t('shell.topbar.viewNotifications')}
                </button>
              </div>
            </details>
            <details className="profile-menu">
              <summary aria-label={t('shell.topbar.profile')}>
                <UserCircle size={20} />
                <span>{user?.displayName ?? t('shell.auth.userFallback')}</span>
              </summary>
              <div className="profile-menu__content" role="menu">
                <button role="menuitem" type="button">
                  <UserCircle size={18} />
                  {t('shell.topbar.menu.account')}
                </button>
                <button role="menuitem" type="button">
                  <Settings size={18} />
                  {t('shell.topbar.menu.settings')}
                </button>
                <button onClick={() => void handleSignOut()} role="menuitem" type="button">
                  <LogOut size={18} />
                  {t('shell.topbar.menu.signOut')}
                </button>
              </div>
            </details>
          </div>
        </header>

        <main className="admin-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
