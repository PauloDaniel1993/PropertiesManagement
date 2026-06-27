import type { DocumentDownload } from '../../lib/api/documents'

export function saveDownloadedDocument(download: DocumentDownload) {
  if (
    typeof document === 'undefined' ||
    typeof URL === 'undefined' ||
    typeof URL.createObjectURL !== 'function'
  ) {
    return
  }

  const objectUrl = URL.createObjectURL(download.blob)
  const anchor = document.createElement('a')
  anchor.href = objectUrl
  anchor.download = download.fileName
  anchor.style.display = 'none'
  document.body.append(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(objectUrl)
}
