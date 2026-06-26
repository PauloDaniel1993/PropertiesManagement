import { useTranslation } from 'react-i18next'
import { EmptyState, PageHeader, SearchInput } from '../components'
import type { AdminMenuItemContract } from '../navigation/menuContract'

type ModulePlaceholderPageProps = {
  item: AdminMenuItemContract
}

export function ModulePlaceholderPage({ item }: ModulePlaceholderPageProps) {
  const { t } = useTranslation()

  return (
    <section aria-labelledby={`${item.id}-title`} className="module-page">
      <PageHeader
        description={t(item.page.descriptionKey)}
        primaryAction={{ label: t('shell.actions.newRecord') }}
        title={<span id={`${item.id}-title`}>{t(item.page.titleKey)}</span>}
      />

      <div className="module-page__toolbar">
        <SearchInput
          label={t('shell.topbar.searchLabel')}
          placeholder={t('shell.topbar.searchPlaceholder')}
        />
        <button type="button">{t('shell.actions.refresh')}</button>
      </div>

      <EmptyState
        description={t(item.page.emptyDescriptionKey)}
        title={t(item.page.emptyTitleKey)}
      />
    </section>
  )
}
