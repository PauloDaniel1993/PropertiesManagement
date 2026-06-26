import {
  Bell,
  Building2,
  Languages,
  LogOut,
  Menu,
  Moon,
  PanelLeftClose,
  PanelLeftOpen,
  Search,
  Settings,
  Sun,
  UserCircle,
  X,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { adminMenuItems, findAdminMenuItemByPath } from '../navigation/menuContract'
import { menuIconComponents } from '../navigation/menuIcons'
import { useActiveOrganizationStore } from '../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../stores/useAuthSessionStore'
import { useShellStore } from '../stores/useShellStore'

export function AdminShell() {
  const { i18n, t } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()
  const activeItem = findAdminMenuItemByPath(location.pathname)
  const { organizations, activeOrganization, setActiveOrganizationId } =
    useActiveOrganizationStore()
  const { locale, setLocale, theme, toggleTheme } = useAppPreferencesStore()
  const user = useAuthSessionStore((state) => state.user)
  const signOut = useAuthSessionStore((state) => state.signOut)
  const {
    closeMobileNavigation,
    isMobileNavigationOpen,
    isSidebarCollapsed,
    openMobileNavigation,
    toggleSidebarCollapsed,
  } = useShellStore()
  const nextLocale = locale === 'pt-BR' ? 'en-US' : 'pt-BR'

  function handleLocaleToggle() {
    setLocale(nextLocale)
    void i18n.changeLanguage(nextLocale)
  }

  function handleSignOut() {
    signOut()
    navigate('/login', { replace: true })
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
          {adminMenuItems.map((item) => {
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

          <label className="topbar-search">
            <Search aria-hidden="true" size={18} />
            <span className="sr-only">{t('shell.topbar.searchLabel')}</span>
            <input placeholder={t('shell.topbar.searchPlaceholder')} type="search" />
          </label>

          <label className="organization-select">
            <span className="sr-only">{t('shell.topbar.organizationLabel')}</span>
            <select
              aria-label={t('shell.topbar.organizationLabel')}
              onChange={(event) => setActiveOrganizationId(event.target.value)}
              value={activeOrganization?.id ?? ''}
            >
              {organizations.map((organization) => (
                <option key={organization.id} value={organization.id}>
                  {organization.displayName}
                </option>
              ))}
            </select>
          </label>

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
            <button
              aria-label={t('shell.topbar.notifications')}
              className="notification-button"
              type="button"
            >
              <Bell size={18} />
              <span aria-label={t('shell.topbar.unreadNotifications')}>3</span>
            </button>
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
                <button onClick={handleSignOut} role="menuitem" type="button">
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
