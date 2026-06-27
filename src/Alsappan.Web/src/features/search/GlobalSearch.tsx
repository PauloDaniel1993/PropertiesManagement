import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ExternalLink, LoaderCircle, Search } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { globalSearch, type GlobalSearchGroup, type GlobalSearchResult } from '../../lib/api/search'
import { useActiveOrganizationStore } from '../../stores/useActiveOrganizationStore'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getSearchCopy } from './searchCopy'

type FlattenedResult = {
  group: GlobalSearchGroup
  result: GlobalSearchResult
}

const emptySearchGroups: GlobalSearchGroup[] = []

function useDebouncedValue(value: string, delayMs: number) {
  const [debouncedValue, setDebouncedValue] = useState(value)

  useEffect(() => {
    const timeoutId = window.setTimeout(() => setDebouncedValue(value), delayMs)
    return () => window.clearTimeout(timeoutId)
  }, [delayMs, value])

  return debouncedValue
}

function flattenGroups(groups: GlobalSearchGroup[]) {
  return groups.flatMap((group) => group.results.map((result) => ({ group, result })))
}

export function GlobalSearch() {
  const apiClient = useApiClient()
  const navigate = useNavigate()
  const { t } = useTranslation()
  const activeOrganizationId = useActiveOrganizationStore((state) => state.activeOrganizationId)
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getSearchCopy(locale)
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const debouncedQuery = useDebouncedValue(query.trim(), 220)
  const canSearch = debouncedQuery.length >= 2
  const searchQuery = useQuery({
    enabled: canSearch,
    queryFn: () => globalSearch(apiClient, debouncedQuery, { limit: 5, locale }),
    queryKey: ['global-search', activeOrganizationId, debouncedQuery, locale],
    staleTime: 20_000,
  })
  const groups = searchQuery.data?.groups ?? emptySearchGroups
  const flatResults = useMemo(() => flattenGroups(groups), [groups])

  useEffect(() => {
    setActiveIndex(0)
  }, [debouncedQuery])

  function navigateToResult(item: FlattenedResult | undefined) {
    if (!item) {
      return
    }

    setIsOpen(false)
    setQuery('')
    navigate(item.result.route)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Escape') {
      setIsOpen(false)
      inputRef.current?.blur()
      return
    }

    if (!isOpen && ['ArrowDown', 'ArrowUp', 'Enter'].includes(event.key)) {
      setIsOpen(true)
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setActiveIndex((current) =>
        flatResults.length === 0 ? 0 : (current + 1) % flatResults.length,
      )
      return
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActiveIndex((current) =>
        flatResults.length === 0 ? 0 : (current - 1 + flatResults.length) % flatResults.length,
      )
      return
    }

    if (event.key === 'Enter') {
      event.preventDefault()
      navigateToResult(flatResults[activeIndex] ?? flatResults[0])
    }
  }

  return (
    <div className="global-search">
      <label className="topbar-search">
        <Search aria-hidden="true" size={18} />
        <span className="sr-only">{t('shell.topbar.searchLabel')}</span>
        <input
          ref={inputRef}
          aria-autocomplete="list"
          aria-controls="global-search-results"
          aria-expanded={isOpen}
          aria-label={t('shell.topbar.searchLabel')}
          onBlur={() => {
            window.setTimeout(() => setIsOpen(false), 120)
          }}
          onChange={(event) => {
            setQuery(event.currentTarget.value)
            setIsOpen(true)
          }}
          onFocus={() => setIsOpen(true)}
          onKeyDown={handleKeyDown}
          placeholder={t('shell.topbar.searchPlaceholder')}
          role="combobox"
          type="search"
          value={query}
        />
      </label>

      {isOpen && query.trim().length > 0 ? (
        <div className="global-search__panel" id="global-search-results" role="listbox">
          {!canSearch ? (
            <p>{copy.minLength}</p>
          ) : searchQuery.isLoading || searchQuery.isFetching ? (
            <p>
              <LoaderCircle aria-hidden="true" className="als-action-button__spinner" size={16} />
              {copy.loading}
            </p>
          ) : searchQuery.isError ? (
            <p>{copy.error}</p>
          ) : flatResults.length === 0 ? (
            <p>{copy.empty}</p>
          ) : (
            groups.map((group) => (
              <section className="global-search__group" key={group.entityType}>
                <div className="global-search__group-header">
                  <strong>{group.entityTypeLabel}</strong>
                  <button
                    onMouseDown={(event) => event.preventDefault()}
                    onClick={() => {
                      setIsOpen(false)
                      setQuery('')
                      navigate(group.route)
                    }}
                    type="button"
                  >
                    {copy.searchAll}
                  </button>
                </div>
                {group.results.map((result) => {
                  const flattenedIndex = flatResults.findIndex((item) => item.result === result)
                  const isActive = flattenedIndex === activeIndex

                  return (
                    <button
                      aria-selected={isActive}
                      className="global-search__result"
                      data-active={isActive ? 'true' : 'false'}
                      key={`${result.entityType}:${result.id}`}
                      onMouseDown={(event) => event.preventDefault()}
                      onClick={() => navigateToResult({ group, result })}
                      role="option"
                      type="button"
                    >
                      <span>
                        <strong>{result.label}</strong>
                        {result.summary ? <small>{result.summary}</small> : null}
                        <small>{copy.resultHint(result.matchedField)}</small>
                      </span>
                      <ExternalLink aria-hidden="true" size={15} />
                    </button>
                  )
                })}
              </section>
            ))
          )}
        </div>
      ) : null}
    </div>
  )
}
