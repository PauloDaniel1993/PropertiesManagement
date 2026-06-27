import { FilePlus } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { AppLocale } from '../../i18n'
import { buildDocumentRelationshipRoute, type RelationshipContext } from './relationships'

export function DocumentLinkAction({
  context,
  locale,
}: {
  context: RelationshipContext
  locale: AppLocale
}) {
  const { t } = useTranslation()
  const href = buildDocumentRelationshipRoute(context)

  return (
    <a
      aria-label={t('components.relationships.documentLinkTitle', { lng: locale })}
      href={href}
      style={{
        alignItems: 'center',
        background: 'var(--als-color-surface, #ffffff)',
        border: '1px solid var(--als-color-border, #d7deea)',
        borderRadius: 'var(--als-radius-sm, 6px)',
        color: 'var(--als-color-text, #1f2937)',
        display: 'inline-flex',
        fontSize: '0.875rem',
        fontWeight: 800,
        gap: 8,
        minHeight: 36,
        paddingInline: 12,
        textDecoration: 'none',
        whiteSpace: 'nowrap',
      }}
      title={t('components.relationships.documentLinkTitle', { lng: locale })}
    >
      <FilePlus aria-hidden="true" size={16} />
      {t('components.relationships.documentLinkLabel', { lng: locale })}
    </a>
  )
}
