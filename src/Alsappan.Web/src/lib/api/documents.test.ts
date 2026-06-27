import { describe, expect, it, vi } from 'vitest'
import { ApiClient } from './client'
import { downloadDocument, uploadDocument, type DocumentUploadRequest } from './documents'

const documentId = '77777777-7777-7777-7777-777777777777'

const apiDocument = {
  auditRoute: `/auditoria?entityType=document&entityId=${documentId}`,
  category: 'contract',
  categoryLabel: 'Contrato',
  contentType: 'application/pdf',
  createdAt: '2026-06-27T10:00:00.000Z',
  currentVersionNumber: 1,
  description: 'Documento digitalizado',
  downloadRoute: `/v1/documents/${documentId}/download`,
  fileName: 'contrato.pdf',
  id: documentId,
  isArchived: false,
  links: [],
  sizeBytes: 3,
  status: { code: 'active', label: 'Ativo', tone: 'success' },
  timelineRoute: `/timeline?entityType=document&entityId=${documentId}`,
  title: 'Contrato assinado',
  uploadedAt: '2026-06-27T10:00:00.000Z',
  versions: [],
} as const

describe('documents api client', () => {
  it('uploads documents as multipart form data without JSON content type', async () => {
    const fetchImpl = vi.fn(
      async () =>
        new Response(JSON.stringify(apiDocument), {
          headers: { 'Content-Type': 'application/json' },
          status: 200,
        }),
    ) as unknown as typeof fetch
    const client = new ApiClient({ baseUrl: 'https://api.test', fetchImpl })
    const request: DocumentUploadRequest = {
      category: 'contract',
      description: 'Documento digitalizado',
      file: new File(['pdf'], 'contrato.pdf', { type: 'application/pdf' }),
      links: [{ entityId: documentId, entityType: 'contract', label: 'Contrato 1' }],
      title: 'Contrato assinado',
      versionNotes: 'Versao inicial',
    }

    await uploadDocument(client, request, 'pt-BR')

    const [url, init] = vi.mocked(fetchImpl).mock.calls[0]
    const body = init?.body as FormData
    const headers = init?.headers as Headers

    expect(String(url)).toBe('https://api.test/v1/documents?locale=pt-BR')
    expect(init?.method).toBe('POST')
    expect(headers.get('Content-Type')).toBeNull()
    expect(body).toBeInstanceOf(FormData)
    expect(body.get('category')).toBe('contract')
    expect(body.get('title')).toBe('Contrato assinado')
    expect(body.get('linksJson')).toContain('"entityType":"contract"')
  })

  it('downloads document blobs with server provided file names', async () => {
    const fetchImpl = vi.fn(
      async () =>
        new Response('pdf', {
          headers: {
            'Content-Disposition': 'attachment; filename="contrato.pdf"',
            'Content-Type': 'application/pdf',
          },
          status: 200,
        }),
    ) as unknown as typeof fetch
    const client = new ApiClient({ baseUrl: 'https://api.test', fetchImpl })

    const result = await downloadDocument(client, documentId)

    expect(result.fileName).toBe('contrato.pdf')
    expect(result.contentType).toBe('application/pdf')
    expect(result.sizeBytes).toBe(3)
    expect(fetchImpl).toHaveBeenCalledWith(
      `https://api.test/v1/documents/${documentId}/download`,
      expect.objectContaining({ method: 'GET' }),
    )
  })
})
