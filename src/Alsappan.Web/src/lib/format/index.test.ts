import { formatDate, formatMoney, formatNumber, formatPlural, formatTime } from '.'

describe('locale-aware format helpers', () => {
  it('formats dates and times with the requested locale', () => {
    expect(formatDate('2026-02-03')).toBe('03/02/2026')
    expect(formatDate('2026-02-03', { locale: 'en-US' })).toBe('2/3/26')
    expect(formatTime('2026-02-03T13:45:00.000Z', { locale: 'en-US', timeZone: 'UTC' })).toBe(
      '1:45 PM',
    )
  })

  it('formats money and numbers with locale-specific separators', () => {
    expect(formatMoney(1234.5, { currency: 'USD', locale: 'en-US' })).toBe('$1,234.50')
    expect(formatMoney(1234.5, { currency: 'BRL', locale: 'pt-BR' })).toContain('1.234,50')
    expect(formatNumber(1234.5, { locale: 'pt-BR' })).toBe('1.234,5')
  })

  it('selects plural forms and injects formatted counts', () => {
    expect(
      formatPlural(1, { one: '{{count}} record', other: '{{count}} records' }, { locale: 'en-US' }),
    ).toBe('1 record')
    expect(formatPlural(2, { one: '{{count}} registro', other: '{{count}} registros' })).toBe(
      '2 registros',
    )
  })
})
