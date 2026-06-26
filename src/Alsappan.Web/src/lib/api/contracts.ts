export type ValidationErrorMap = Record<string, string[]>

export type ApiProblemDetails = {
  detail?: string
  errors?: ValidationErrorMap
  instance?: string
  status?: number
  title: string
  traceId?: string
  type?: string
  [extension: string]: unknown
}

export type ApiPagedResult<TItem> = {
  hasNextPage: boolean
  hasPreviousPage: boolean
  items: TItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type ApiListRequest = {
  filters?: Record<string, string | number | boolean | undefined>
  page?: number
  pageSize?: number
  search?: string
  sort?: string
}

export type ApiSelectOption<TValue extends string = string> = {
  disabled?: boolean
  label: string
  value: TValue
}

export type ApiStatusLabel<TCode extends string = string> = {
  code: TCode
  color?: 'blue' | 'gray' | 'green' | 'red' | 'yellow'
  label: string
}

export type ApiAuditMetadata = {
  createdAt: string
  createdBy?: string
  deletedAt?: string
  deletedBy?: string
  rowVersion?: string
  updatedAt?: string
  updatedBy?: string
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function isValidationErrorMap(value: unknown): value is ValidationErrorMap {
  if (!isRecord(value)) {
    return false
  }

  return Object.values(value).every(
    (messages) =>
      Array.isArray(messages) && messages.every((message) => typeof message === 'string'),
  )
}

export function isApiProblemDetails(value: unknown): value is ApiProblemDetails {
  if (!isRecord(value)) {
    return false
  }

  const hasTitle = typeof value.title === 'string'
  const hasStatus =
    value.status === undefined ||
    (typeof value.status === 'number' && Number.isInteger(value.status))
  const hasErrors = value.errors === undefined || isValidationErrorMap(value.errors)

  return hasTitle && hasStatus && hasErrors
}

export function getProblemValidationErrors(problem: ApiProblemDetails): ValidationErrorMap {
  return problem.errors ?? {}
}
