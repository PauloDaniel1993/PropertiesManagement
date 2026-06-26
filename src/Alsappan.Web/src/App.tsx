import { Building2, Globe2, Moon, Sun } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useAppPreferencesStore } from './stores/useAppPreferencesStore'

export function App() {
  const { i18n, t } = useTranslation()
  const { locale, setLocale, theme, toggleTheme } = useAppPreferencesStore()
  const nextLocale = locale === 'pt-BR' ? 'en-US' : 'pt-BR'

  function handleLocaleToggle() {
    setLocale(nextLocale)
    void i18n.changeLanguage(nextLocale)
  }

  return (
    <main className="app-shell" data-theme={theme}>
      <section className="app-panel" aria-labelledby="app-title">
        <div className="brand-mark" aria-hidden="true">
          <Building2 size={32} strokeWidth={2.2} />
        </div>

        <div className="app-copy">
          <p className="eyebrow">{t('app.eyebrow')}</p>
          <h1 id="app-title">{t('app.title')}</h1>
          <p>{t('app.subtitle')}</p>
        </div>

        <div className="foundation-grid" aria-label={t('app.foundationLabel')}>
          <span>{t('foundation.react')}</span>
          <span>{t('foundation.dotnet')}</span>
          <span>{t('foundation.postgres')}</span>
          <span>{t('foundation.i18n')}</span>
        </div>

        <div className="app-actions">
          <button type="button" onClick={handleLocaleToggle}>
            <Globe2 size={18} />
            {t('actions.language', { locale: nextLocale })}
          </button>
          <button type="button" onClick={toggleTheme}>
            {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
            {t(`actions.${theme === 'dark' ? 'light' : 'dark'}`)}
          </button>
        </div>
      </section>
    </main>
  )
}
