import { Building2, Languages, Moon, Sun } from 'lucide-react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate } from 'react-router-dom'
import { defaultAuthenticatedRoute } from '../navigation/menuContract'
import { useAppPreferencesStore } from '../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../stores/useAuthSessionStore'

export function LoginPage() {
  const navigate = useNavigate()
  const { i18n, t } = useTranslation()
  const isAuthenticated = useAuthSessionStore((state) => state.isAuthenticated)
  const intendedPath = useAuthSessionStore((state) => state.intendedPath)
  const signInDemo = useAuthSessionStore((state) => state.signInDemo)
  const clearIntendedPath = useAuthSessionStore((state) => state.clearIntendedPath)
  const locale = useAppPreferencesStore((state) => state.locale)
  const setLocale = useAppPreferencesStore((state) => state.setLocale)
  const theme = useAppPreferencesStore((state) => state.theme)
  const toggleTheme = useAppPreferencesStore((state) => state.toggleTheme)
  const nextLocale = locale === 'pt-BR' ? 'en-US' : 'pt-BR'

  if (isAuthenticated) {
    return <Navigate replace to={intendedPath ?? defaultAuthenticatedRoute} />
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    signInDemo()
    const destination = intendedPath ?? defaultAuthenticatedRoute
    clearIntendedPath()
    navigate(destination, { replace: true })
  }

  function handleLocaleToggle() {
    setLocale(nextLocale)
    void i18n.changeLanguage(nextLocale)
  }

  return (
    <main className="login-screen" data-theme={theme}>
      <section aria-labelledby="login-title" className="login-panel">
        <div className="login-panel__brand">
          <span aria-hidden="true" className="brand-symbol">
            <Building2 size={28} strokeWidth={2.25} />
          </span>
          <div>
            <p>{t('shell.brand.subtitle')}</p>
            <strong>{t('shell.brand.name')}</strong>
          </div>
        </div>

        <div className="login-panel__copy">
          <h1 id="login-title">{t('shell.auth.loginTitle')}</h1>
          <p>{t('shell.auth.loginSubtitle')}</p>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <label>
            <span>{t('shell.auth.email')}</span>
            <input
              autoComplete="email"
              defaultValue="admin@alsappan.local"
              name="email"
              type="email"
            />
          </label>
          <label>
            <span>{t('shell.auth.password')}</span>
            <input
              autoComplete="current-password"
              defaultValue="alsappan"
              name="password"
              type="password"
            />
          </label>
          <button className="button button--primary" type="submit">
            {t('shell.auth.submit')}
          </button>
        </form>

        <div className="login-panel__actions">
          <button aria-label={t('shell.locale.toggle')} onClick={handleLocaleToggle} type="button">
            <Languages size={18} />
            {nextLocale}
          </button>
          <button aria-label={t('shell.theme.toggle')} onClick={toggleTheme} type="button">
            {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
          </button>
        </div>
      </section>
    </main>
  )
}
