import type { AppLocale } from '../../i18n'
import { formatNumber } from '../../lib/format'
import type { StatusBadgeTone } from '../../components'
import type { DocumentStatusLabel } from '../../lib/api/documents'

export function formatFileSize(sizeBytes: number, locale: AppLocale) {
  if (sizeBytes < 1024) {
    return `${formatNumber(sizeBytes, { locale })} B`
  }

  if (sizeBytes < 1024 * 1024) {
    return `${formatNumber(sizeBytes / 1024, { locale, maximumFractionDigits: 1 })} KB`
  }

  return `${formatNumber(sizeBytes / (1024 * 1024), { locale, maximumFractionDigits: 1 })} MB`
}

export function getDocumentStatusTone(status: DocumentStatusLabel): StatusBadgeTone {
  if (status.code === 'archived' || status.tone === 'archived') {
    return 'archived'
  }

  if (status.tone === 'warning') {
    return 'warning'
  }

  if (status.tone === 'danger') {
    return 'danger'
  }

  if (status.tone === 'info') {
    return 'info'
  }

  return status.code === 'active' ? 'success' : 'neutral'
}
