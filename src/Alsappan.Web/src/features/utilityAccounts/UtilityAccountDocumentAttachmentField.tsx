import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { X } from 'lucide-react'
import { ActionButton, FormField, SelectInput, TextInput } from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { listDocuments, type DocumentListItem } from '../../lib/api/documents'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getUtilityAccountCopy } from './utilityAccountCopy'

type UtilityDocumentAttachmentCopy = ReturnType<typeof getUtilityAccountCopy>['documentAttachment']

export type UtilityAccountDocumentAttachmentFieldProps = {
  canReadDocuments?: boolean
  label: string
  onChange: (documentId: string) => void
  value: string
}

const fieldGridStyle = {
  alignItems: 'end',
  display: 'grid',
  gap: 12,
  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
} as const

function documentOptionLabel(document: DocumentListItem) {
  return `${document.title} - ${document.fileName}`
}

function buildDocumentOptions(
  documents: DocumentListItem[],
  value: string,
  copy: UtilityDocumentAttachmentCopy,
) {
  const options = documents.map((document) => ({
    label: documentOptionLabel(document),
    value: document.id,
  }))

  if (value && !options.some((option) => option.value === value)) {
    options.unshift({
      label: copy.selectedId(value),
      value,
    })
  }

  return options
}

export function UtilityAccountDocumentAttachmentField({
  canReadDocuments = false,
  label,
  onChange,
  value,
}: UtilityAccountDocumentAttachmentFieldProps) {
  const apiClient = useApiClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getUtilityAccountCopy(locale).documentAttachment
  const [search, setSearch] = useState('')
  const documentsQuery = useQuery({
    enabled: canReadDocuments,
    queryFn: () =>
      listDocuments(apiClient, {
        category: 'utility-account',
        locale,
        pageSize: 25,
        search: search.trim() || undefined,
      }),
    queryKey: ['utility-document-picker', locale, search],
  })
  const documentOptions = useMemo(
    () => buildDocumentOptions(documentsQuery.data?.items ?? [], value, copy),
    [copy, documentsQuery.data?.items, value],
  )

  return (
    <div style={{ display: 'grid', gap: 12 }}>
      {canReadDocuments ? (
        <>
          <FormField label={copy.search}>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                id={id}
                aria-describedby={describedBy}
                autoComplete="off"
                isInvalid={isInvalid}
                onChange={(event) => setSearch(event.currentTarget.value)}
                placeholder={copy.searchPlaceholder}
                value={search}
              />
            )}
          </FormField>

          <div style={fieldGridStyle}>
            <FormField
              description={documentsQuery.isError ? copy.lookupError : undefined}
              label={label}
            >
              {({ describedBy, id, isInvalid }) => (
                <SelectInput
                  id={id}
                  aria-describedby={describedBy}
                  disabled={documentsQuery.isLoading}
                  isInvalid={isInvalid}
                  onChange={(event) => onChange(event.currentTarget.value)}
                  options={documentOptions}
                  placeholder={documentsQuery.isLoading ? copy.loading : copy.selectPlaceholder}
                  value={value}
                />
              )}
            </FormField>
            <ActionButton
              disabled={!value}
              icon={<X aria-hidden="true" size={16} />}
              onClick={() => onChange('')}
              size="sm"
              type="button"
            >
              {copy.clear}
            </ActionButton>
          </div>
        </>
      ) : null}

      <FormField label={canReadDocuments ? copy.manualDocumentId : label}>
        {({ describedBy, id, isInvalid }) => (
          <TextInput
            id={id}
            aria-describedby={describedBy}
            autoComplete="off"
            isInvalid={isInvalid}
            onChange={(event) => onChange(event.currentTarget.value)}
            placeholder={copy.manualDocumentIdPlaceholder}
            value={value}
          />
        )}
      </FormField>

      {!canReadDocuments ? (
        <p style={{ color: 'var(--als-color-text-muted, #64748b)', margin: 0 }}>
          {copy.manualFallback}
        </p>
      ) : null}
    </div>
  )
}
