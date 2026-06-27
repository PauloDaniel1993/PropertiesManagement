import { FilePlus } from 'lucide-react'
import type { AppLocale } from '../../i18n'
import { buildDocumentRelationshipRoute, type RelationshipContext } from './relationships'

const documentLinkCopy = {
  'en-US': {
    label: 'Link document',
    title: 'Open linked documents',
  },
  'pt-BR': {
    label: 'Vincular documento',
    title: 'Abrir documentos vinculados',
  },
} as const

export function DocumentLinkAction({
  context,
  locale,
}: {
  context: RelationshipContext
  locale: AppLocale
}) {
  const href = buildDocumentRelationshipRoute(context)
  const copy = documentLinkCopy[locale === 'en-US' ? 'en-US' : 'pt-BR']

  return (
    <a
      aria-label={copy.title}
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
      title={copy.title}
    >
      <FilePlus aria-hidden="true" size={16} />
      {copy.label}
    </a>
  )
}
