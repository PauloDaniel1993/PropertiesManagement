import type { AppLocale } from '../../i18n'

export type SearchCopy = ReturnType<typeof getSearchCopy>

const copies = {
  'en-US': {
    empty: 'No readable results found',
    error: 'Could not search',
    loading: 'Searching...',
    minLength: 'Type at least 2 characters',
    resultHint: (matchedField: string) => `Matched in ${matchedField}`,
    searchAll: 'Open results',
  },
  'pt-BR': {
    empty: 'Nenhum resultado permitido encontrado',
    error: 'Nao foi possivel buscar',
    loading: 'Buscando...',
    minLength: 'Digite ao menos 2 caracteres',
    resultHint: (matchedField: string) => `Encontrado em ${matchedField}`,
    searchAll: 'Abrir resultados',
  },
} as const

export function getSearchCopy(locale: AppLocale = 'pt-BR') {
  return copies[locale] ?? copies['pt-BR']
}
