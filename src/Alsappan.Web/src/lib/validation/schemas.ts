import { z, type ZodError } from 'zod'

export const validationMessageKeys = {
  currencyCode: 'validation.currencyCode',
  dateRange: 'validation.dateRange',
  email: 'validation.email',
  invalidDate: 'validation.invalidDate',
  invalidId: 'validation.invalidId',
  maxLength: 'validation.maxLength',
  minValue: 'validation.minValue',
  required: 'validation.required',
} as const

export type FieldErrorMap = Record<string, string[]>

function blankStringToUndefined(value: unknown) {
  if (typeof value === 'string' && value.trim().length === 0) {
    return undefined
  }

  return value
}

function isValidIsoDate(value: string) {
  const timestamp = Date.parse(`${value}T00:00:00.000Z`)

  if (Number.isNaN(timestamp)) {
    return false
  }

  return new Date(timestamp).toISOString().slice(0, 10) === value
}

export function requiredTrimmedString(maxLength = 200) {
  return z
    .string()
    .trim()
    .min(1, { message: validationMessageKeys.required })
    .max(maxLength, { message: validationMessageKeys.maxLength })
}

export function optionalTrimmedString(maxLength = 500) {
  return z.preprocess(
    blankStringToUndefined,
    z.string().trim().max(maxLength, { message: validationMessageKeys.maxLength }).optional(),
  )
}

export const entityIdSchema = z.string().uuid({ message: validationMessageKeys.invalidId })

export const emailSchema = z
  .string()
  .trim()
  .email({ message: validationMessageKeys.email })
  .max(254, { message: validationMessageKeys.maxLength })

export const optionalEmailSchema = z.preprocess(blankStringToUndefined, emailSchema.optional())

export const currencyCodeSchema = z
  .string()
  .trim()
  .regex(/^[A-Z]{3}$/, { message: validationMessageKeys.currencyCode })

export const moneyAmountSchema = z.coerce
  .number()
  .min(0, { message: validationMessageKeys.minValue })

export const isoDateSchema = z
  .string()
  .regex(/^\d{4}-\d{2}-\d{2}$/, {
    message: validationMessageKeys.invalidDate,
  })
  .refine(isValidIsoDate, { message: validationMessageKeys.invalidDate })

export const dateRangeSchema = z
  .object({
    endDate: isoDateSchema.optional(),
    startDate: isoDateSchema.optional(),
  })
  .refine(({ endDate, startDate }) => !endDate || !startDate || endDate >= startDate, {
    message: validationMessageKeys.dateRange,
    path: ['endDate'],
  })

export const pageRequestSchema = z.object({
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().min(1).max(100).default(20),
  search: optionalTrimmedString(200),
  sort: optionalTrimmedString(80),
})

export const selectOptionSchema = z.object({
  disabled: z.boolean().optional(),
  label: requiredTrimmedString(120),
  value: requiredTrimmedString(80),
})

export const localizedLabelSchema = z.object({
  label: requiredTrimmedString(120),
  locale: z.enum(['pt-BR', 'en-US']),
})

export const auditMetadataSchema = z.object({
  createdAt: z.string().datetime(),
  createdBy: optionalTrimmedString(120),
  deletedAt: z.string().datetime().optional(),
  deletedBy: optionalTrimmedString(120),
  rowVersion: optionalTrimmedString(120),
  updatedAt: z.string().datetime().optional(),
  updatedBy: optionalTrimmedString(120),
})

export function toFieldErrorMap(error: ZodError): FieldErrorMap {
  return error.issues.reduce<FieldErrorMap>((errors, issue) => {
    const field = issue.path.length > 0 ? issue.path.join('.') : '$'
    errors[field] = [...(errors[field] ?? []), issue.message]

    return errors
  }, {})
}
