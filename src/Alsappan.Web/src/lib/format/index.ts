import { defaultLocale, normalizeAppLocale, type AppLocale } from '../../i18n'

export type DateTimeInput = Date | number | string

export type LocaleFormatOptions = {
  locale?: AppLocale
}

export type MoneyFormatOptions = LocaleFormatOptions & {
  currency?: string
}

export type NumberFormatOptions = LocaleFormatOptions & Intl.NumberFormatOptions

export type DateFormatOptions = LocaleFormatOptions & Intl.DateTimeFormatOptions

export type PluralForms = Partial<Record<Intl.LDMLPluralRule, string>> & {
  other: string
}

function resolveLocale(locale?: AppLocale) {
  return normalizeAppLocale(locale ?? defaultLocale)
}

function isDateOnlyString(value: string) {
  return /^\d{4}-\d{2}-\d{2}$/.test(value)
}

function toDate(value: DateTimeInput) {
  if (value instanceof Date) {
    return value
  }

  if (typeof value === 'string' && isDateOnlyString(value)) {
    return new Date(`${value}T00:00:00.000Z`)
  }

  return new Date(value)
}

export function formatDate(value: DateTimeInput, options: DateFormatOptions = {}) {
  const { locale, ...dateOptions } = options

  return new Intl.DateTimeFormat(resolveLocale(locale), {
    dateStyle: 'short',
    timeZone: typeof value === 'string' && isDateOnlyString(value) ? 'UTC' : undefined,
    ...dateOptions,
  }).format(toDate(value))
}

export function formatTime(value: DateTimeInput, options: DateFormatOptions = {}) {
  const { locale, ...dateOptions } = options

  return new Intl.DateTimeFormat(resolveLocale(locale), {
    timeStyle: 'short',
    ...dateOptions,
  }).format(toDate(value))
}

export function formatDateTime(value: DateTimeInput, options: DateFormatOptions = {}) {
  const { locale, ...dateOptions } = options

  return new Intl.DateTimeFormat(resolveLocale(locale), {
    dateStyle: 'short',
    timeStyle: 'short',
    ...dateOptions,
  }).format(toDate(value))
}

export function formatMoney(value: number, options: MoneyFormatOptions = {}) {
  const { currency = resolveLocale(options.locale) === 'pt-BR' ? 'BRL' : 'USD', locale } = options

  return new Intl.NumberFormat(resolveLocale(locale), {
    currency,
    style: 'currency',
  }).format(value)
}

export function formatNumber(value: number, options: NumberFormatOptions = {}) {
  const { locale, ...numberOptions } = options

  return new Intl.NumberFormat(resolveLocale(locale), numberOptions).format(value)
}

export function formatPercent(value: number, options: NumberFormatOptions = {}) {
  const { locale, ...numberOptions } = options

  return new Intl.NumberFormat(resolveLocale(locale), {
    maximumFractionDigits: 1,
    style: 'percent',
    ...numberOptions,
  }).format(value)
}

export function getPluralCategory(value: number, locale: AppLocale = defaultLocale) {
  return new Intl.PluralRules(resolveLocale(locale)).select(value)
}

export function formatPlural(value: number, forms: PluralForms, options: LocaleFormatOptions = {}) {
  const locale = resolveLocale(options.locale)
  const category = getPluralCategory(value, locale)
  const template = forms[category] ?? forms.other

  return template.replace('{{count}}', formatNumber(value, { locale }))
}
