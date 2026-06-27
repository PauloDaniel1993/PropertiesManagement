import {
  getProblemValidationErrors,
  isApiProblemDetails,
  type ApiProblemDetails,
  type ValidationErrorMap,
} from './contracts'

export type ApiQueryValue =
  | boolean
  | null
  | number
  | readonly (boolean | number | string)[]
  | string
  | undefined

export type ApiRequestOptions<TBody = unknown> = {
  body?: TBody
  headers?: HeadersInit
  method?: 'DELETE' | 'GET' | 'PATCH' | 'POST' | 'PUT'
  query?: Record<string, ApiQueryValue>
  signal?: AbortSignal
}

export type ApiClientOptions = {
  baseUrl?: string
  fetchImpl?: typeof fetch
  getAccessToken?: () => Promise<string | undefined> | string | undefined
  getLocale?: () => Promise<string | undefined> | string | undefined
  getOrganizationId?: () => Promise<string | undefined> | string | undefined
  onUnauthorized?: () => void
}

export class ApiClientError extends Error {
  readonly problem?: ApiProblemDetails
  readonly responseHeaders: Headers
  readonly status: number
  readonly validationErrors: ValidationErrorMap

  constructor(status: number, responseHeaders: Headers, problem?: ApiProblemDetails) {
    super(problem?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiClientError'
    this.problem = problem
    this.responseHeaders = responseHeaders
    this.status = status
    this.validationErrors = problem ? getProblemValidationErrors(problem) : {}
  }
}

function normalizeBaseUrl(baseUrl: string) {
  return baseUrl.replace(/\/+$/, '')
}

function buildUrl(baseUrl: string, path: string, query?: Record<string, ApiQueryValue>) {
  const normalizedBaseUrl = normalizeBaseUrl(baseUrl)
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const url = `${normalizedBaseUrl}${normalizedPath}`
  const searchParams = new URLSearchParams()

  for (const [key, value] of Object.entries(query ?? {})) {
    if (value === undefined || value === null || value === '') {
      continue
    }

    if (Array.isArray(value)) {
      for (const item of value) {
        searchParams.append(key, String(item))
      }
      continue
    }

    searchParams.set(key, String(value))
  }

  const queryString = searchParams.toString()

  return queryString ? `${url}?${queryString}` : url
}

async function parseProblem(response: Response) {
  const contentType = response.headers.get('content-type') ?? ''

  if (!contentType.includes('application/json')) {
    return undefined
  }

  const body: unknown = await response.json()

  return isApiProblemDetails(body) ? body : undefined
}

export class ApiClient {
  private readonly baseUrl: string
  private readonly fetchImpl: typeof fetch
  private readonly getAccessToken?: ApiClientOptions['getAccessToken']
  private readonly getLocale?: ApiClientOptions['getLocale']
  private readonly getOrganizationId?: ApiClientOptions['getOrganizationId']
  private readonly onUnauthorized?: ApiClientOptions['onUnauthorized']

  constructor(options: ApiClientOptions = {}) {
    this.baseUrl = options.baseUrl ?? import.meta.env.VITE_API_BASE_URL ?? ''
    this.fetchImpl = options.fetchImpl ?? fetch
    this.getAccessToken = options.getAccessToken
    this.getLocale = options.getLocale
    this.getOrganizationId = options.getOrganizationId
    this.onUnauthorized = options.onUnauthorized
  }

  async request<TResponse, TBody = unknown>(path: string, options: ApiRequestOptions<TBody> = {}) {
    const headers = new Headers(options.headers)
    headers.set('Accept', 'application/json')

    if (options.body !== undefined) {
      headers.set('Content-Type', 'application/json')
    }

    const [accessToken, locale, organizationId] = await Promise.all([
      this.getAccessToken?.(),
      this.getLocale?.(),
      this.getOrganizationId?.(),
    ])

    if (accessToken) {
      headers.set('Authorization', `Bearer ${accessToken}`)
    }

    if (locale) {
      headers.set('Accept-Language', locale)
    }

    if (organizationId) {
      headers.set('X-Alsappan-Organization-Id', organizationId)
    }

    const response = await this.fetchImpl(buildUrl(this.baseUrl, path, options.query), {
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      credentials: 'include',
      headers,
      method: options.method ?? 'GET',
      signal: options.signal,
    })

    if (!response.ok) {
      const problem = await parseProblem(response)

      if (response.status === 401) {
        this.onUnauthorized?.()
      }

      throw new ApiClientError(response.status, response.headers, problem)
    }

    if (response.status === 204) {
      return undefined as TResponse
    }

    const contentType = response.headers.get('content-type') ?? ''

    if (contentType.includes('application/json')) {
      return (await response.json()) as TResponse
    }

    return (await response.text()) as TResponse
  }

  delete<TResponse>(path: string, options?: ApiRequestOptions) {
    return this.request<TResponse>(path, { ...options, method: 'DELETE' })
  }

  get<TResponse>(path: string, options?: ApiRequestOptions) {
    return this.request<TResponse>(path, { ...options, method: 'GET' })
  }

  patch<TResponse, TBody = unknown>(path: string, body: TBody, options?: ApiRequestOptions<TBody>) {
    return this.request<TResponse, TBody>(path, {
      ...options,
      body,
      method: 'PATCH',
    })
  }

  post<TResponse, TBody = unknown>(path: string, body: TBody, options?: ApiRequestOptions<TBody>) {
    return this.request<TResponse, TBody>(path, {
      ...options,
      body,
      method: 'POST',
    })
  }

  put<TResponse, TBody = unknown>(path: string, body: TBody, options?: ApiRequestOptions<TBody>) {
    return this.request<TResponse, TBody>(path, {
      ...options,
      body,
      method: 'PUT',
    })
  }
}

export const apiClient = new ApiClient()
