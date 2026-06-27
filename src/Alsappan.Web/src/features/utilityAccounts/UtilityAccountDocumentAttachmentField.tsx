import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { UploadCloud, X } from 'lucide-react'
import { ActionButton, FormField, SelectInput, TextInput } from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { listDocuments, uploadDocument, type DocumentListItem } from '../../lib/api/documents'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getUtilityAccountCopy } from './utilityAccountCopy'

type UtilityDocumentAttachmentCopy = ReturnType<typeof getUtilityAccountCopy>['documentAttachment']

export type UtilityAccountDocumentAttachmentFieldProps = {
  entityId?: string
  label: string
  linkLabel: string
  onChange: (documentId: string) => void
  uploadDefaultTitle: string
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
  entityId,
  label,
  linkLabel,
  onChange,
  uploadDefaultTitle,
  value,
}: UtilityAccountDocumentAttachmentFieldProps) {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getUtilityAccountCopy(locale).documentAttachment
  const [search, setSearch] = useState('')
  const [uploadTitle, setUploadTitle] = useState(uploadDefaultTitle)
  const [uploadFile, setUploadFile] = useState<File | null>(null)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const documentsQuery = useQuery({
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
  const uploadMutation = useMutation({
    mutationFn: async () => {
      if (!uploadFile) {
        throw new Error(copy.fileRequired)
      }

      return uploadDocument(
        apiClient,
        {
          category: 'utility-account',
          file: uploadFile,
          links: entityId
            ? [
                {
                  entityId,
                  entityType: 'utility-account',
                  label: linkLabel,
                },
              ]
            : [],
          title: uploadTitle.trim() || uploadDefaultTitle,
        },
        locale,
      )
    },
    onError: (error) => {
      setUploadError(error instanceof Error ? error.message : copy.uploadError)
    },
    onSuccess: async (document) => {
      onChange(document.id)
      setUploadError(null)
      setUploadFile(null)
      setUploadTitle(uploadDefaultTitle)
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['documents'] }),
        queryClient.invalidateQueries({ queryKey: ['utility-document-picker'] }),
      ])
    },
  })

  return (
    <div style={{ display: 'grid', gap: 12 }}>
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
        <FormField label={label}>
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

      <div style={fieldGridStyle}>
        <FormField label={copy.uploadTitle}>
          {({ describedBy, id, isInvalid }) => (
            <TextInput
              id={id}
              aria-describedby={describedBy}
              autoComplete="off"
              isInvalid={isInvalid}
              onChange={(event) => setUploadTitle(event.currentTarget.value)}
              value={uploadTitle}
            />
          )}
        </FormField>
        <FormField error={uploadError ? [uploadError] : undefined} label={copy.file}>
          {({ describedBy, id, isInvalid }) => (
            <TextInput
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              onChange={(event) => {
                setUploadError(null)
                setUploadFile(event.currentTarget.files?.item(0) ?? null)
              }}
              type="file"
            />
          )}
        </FormField>
      </div>

      <div style={{ display: 'flex', justifyContent: 'flex-start' }}>
        <ActionButton
          icon={<UploadCloud aria-hidden="true" size={16} />}
          isLoading={uploadMutation.isPending}
          onClick={() => uploadMutation.mutate()}
          size="sm"
          type="button"
        >
          {copy.upload}
        </ActionButton>
      </div>
    </div>
  )
}
