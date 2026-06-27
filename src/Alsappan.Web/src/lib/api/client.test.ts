import { describe, expect, it, vi } from 'vitest'
import { ApiClient } from './client'

describe('ApiClient', () => {
  it('sends auth, locale, and active organization headers', async () => {
    const fetchImpl = vi.fn(
      async () =>
        new Response(JSON.stringify({ ok: true }), {
          headers: { 'Content-Type': 'application/json' },
          status: 200,
        }),
    ) as unknown as typeof fetch
    const client = new ApiClient({
      baseUrl: 'https://api.test',
      fetchImpl,
      getAccessToken: () => 'access-token',
      getLocale: () => 'pt-BR',
      getOrganizationId: () => 'organization-1',
    })

    await client.get<{ ok: true }>('/v1/system/info')

    const [, init] = vi.mocked(fetchImpl).mock.calls[0]
    const headers = init?.headers

    expect(headers).toBeInstanceOf(Headers)
    expect((headers as Headers).get('Authorization')).toBe('Bearer access-token')
    expect((headers as Headers).get('Accept-Language')).toBe('pt-BR')
    expect((headers as Headers).get('X-Alsappan-Organization-Id')).toBe('organization-1')
  })
})
