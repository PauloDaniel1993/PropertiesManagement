import { z } from 'zod'
import {
  dateRangeSchema,
  optionalEmailSchema,
  pageRequestSchema,
  requiredTrimmedString,
  toFieldErrorMap,
  validationMessageKeys,
} from './schemas'

describe('shared validation schemas', () => {
  it('trims required strings and rejects blank values with message keys', () => {
    const schema = requiredTrimmedString(10)

    expect(schema.parse('  Casa  ')).toBe('Casa')

    const result = schema.safeParse('   ')
    expect(result.success).toBe(false)

    if (result.success) {
      throw new Error('Expected blank value to fail validation')
    }

    expect(result.error.issues[0]?.message).toBe(validationMessageKeys.required)
  })

  it('normalizes empty optional email fields and validates provided emails', () => {
    expect(optionalEmailSchema.parse('   ')).toBeUndefined()
    expect(optionalEmailSchema.parse('user@example.com')).toBe('user@example.com')
    expect(optionalEmailSchema.safeParse('invalid-email').success).toBe(false)
  })

  it('coerces list paging defaults for API requests', () => {
    expect(pageRequestSchema.parse({ page: '2', pageSize: '50' })).toMatchObject({
      page: 2,
      pageSize: 50,
    })
    expect(pageRequestSchema.parse({})).toMatchObject({
      page: 1,
      pageSize: 20,
    })
  })

  it('rejects date ranges where the end date precedes the start date', () => {
    const result = dateRangeSchema.safeParse({
      endDate: '2026-01-09',
      startDate: '2026-01-10',
    })

    expect(result.success).toBe(false)

    if (result.success) {
      throw new Error('Expected inverted date range to fail validation')
    }

    expect(result.error.issues[0]?.message).toBe(validationMessageKeys.dateRange)
  })

  it('maps zod issues into backend-compatible field error dictionaries', () => {
    const result = z.object({ name: requiredTrimmedString(3) }).safeParse({ name: 'Aluguel' })

    expect(result.success).toBe(false)

    if (result.success) {
      throw new Error('Expected long name to fail validation')
    }

    expect(toFieldErrorMap(result.error)).toEqual({
      name: [validationMessageKeys.maxLength],
    })
  })
})
