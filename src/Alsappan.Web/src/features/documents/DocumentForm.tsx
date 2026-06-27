import { useEffect, useMemo, useState } from 'react'
import { useForm, type FieldErrors } from 'react-hook-form'
import { Plus, Trash2 } from 'lucide-react'
import { z } from 'zod'
import {
  ActionButton,
  FormField,
  SelectInput,
  TextAreaInput,
  TextInput,
  ValidationSummary,
  type SelectOption,
} from '../../components'
import {
  documentCategories,
  documentLinkedEntityTypes,
  type DocumentCategory,
  type DocumentDetail,
  type DocumentLinkedEntityType,
  type DocumentLinkRequest,
  type DocumentUpdateRequest,
  type DocumentUploadRequest,
} from '../../lib/api/documents'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getDocumentCopy } from './documentCopy'

export type DocumentFormMode = 'create' | 'edit'

export type DocumentFormSubmitValue =
  | { mode: 'create'; values: DocumentUploadRequest }
  | { mode: 'edit'; values: DocumentUpdateRequest }

export type DocumentFormProps = {
  allowedFileTypes?: string[]
  categoryOptions: SelectOption[]
  initialValue?: Partial<DocumentDetail>
  isSubmitting?: boolean
  mode: DocumentFormMode
  onCancel?: () => void
  onSubmit: (value: DocumentFormSubmitValue) => Promise<void> | void
}

type DocumentFormValues = {
  category: DocumentCategory
  description: string
  file?: FileList
  title: string
  versionNotes: string
}

type DocumentLinkFormRow = {
  entityId: string
  entityType: DocumentLinkedEntityType | ''
  label: string
}

const fieldsetStyle = {
  border: '1px solid var(--als-color-border, #d7deea)',
  borderRadius: 'var(--als-radius-md, 8px)',
  display: 'grid',
  gap: 14,
  margin: 0,
  padding: 14,
} as const

const fieldGridStyle = {
  display: 'grid',
  gap: 12,
  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
} as const

function optionalText(value: string) {
  const trimmedValue = value.trim()

  return trimmedValue.length > 0 ? trimmedValue : undefined
}

const documentFormSchema = z.object({
  category: z
    .string()
    .refine(
      (value): value is DocumentCategory => documentCategories.includes(value as DocumentCategory),
      'categoryRequired',
    )
    .transform((value) => value as DocumentCategory),
  title: z.string().trim().min(1, 'titleRequired'),
})

function getMessage(key: string | undefined, copy: ReturnType<typeof getDocumentCopy>) {
  if (key === 'categoryRequired') {
    return copy.form.categoryRequired
  }

  if (key === 'entityIdRequired') {
    return copy.form.entityIdRequired
  }

  if (key === 'entityTypeRequired') {
    return copy.form.entityTypeRequired
  }

  if (key === 'fileRequired') {
    return copy.form.fileRequired
  }

  if (key === 'titleRequired') {
    return copy.form.titleRequired
  }

  return copy.form.titleRequired
}

function buildErrorSummary(
  errors: FieldErrors<DocumentFormValues>,
  linkErrors: string[],
  copy: ReturnType<typeof getDocumentCopy>,
) {
  const summary = Object.entries(errors).reduce<Record<string, string[]>>(
    (currentSummary, [field, error]) => {
      currentSummary[field] = [getMessage(String(error?.message), copy)]

      return currentSummary
    },
    {},
  )

  if (linkErrors.length > 0) {
    summary.links = linkErrors
  }

  return summary
}

function getDefaultValues(initialValue?: Partial<DocumentDetail>): DocumentFormValues {
  return {
    category: initialValue?.category ?? 'general',
    description: initialValue?.description ?? '',
    title: initialValue?.title ?? '',
    versionNotes: '',
  }
}

function getInitialLinks(initialValue?: Partial<DocumentDetail>): DocumentLinkFormRow[] {
  if (!initialValue?.links || initialValue.links.length === 0) {
    return [{ entityId: '', entityType: '', label: '' }]
  }

  return initialValue.links.map((link) => ({
    entityId: link.entityId,
    entityType: link.entityType,
    label: link.label ?? '',
  }))
}

function normalizeLinks(rows: DocumentLinkFormRow[], copy: ReturnType<typeof getDocumentCopy>) {
  const links: DocumentLinkRequest[] = []
  const errors: string[] = []

  for (const row of rows) {
    const entityId = row.entityId.trim()
    const label = optionalText(row.label)
    const hasAnyValue = Boolean(entityId || row.entityType || label)

    if (!hasAnyValue) {
      continue
    }

    if (!row.entityType || !documentLinkedEntityTypes.includes(row.entityType)) {
      errors.push(copy.form.entityTypeRequired)
      continue
    }

    if (!entityId) {
      errors.push(copy.form.entityIdRequired)
      continue
    }

    links.push({
      entityId,
      entityType: row.entityType,
      label,
    })
  }

  return { errors: Array.from(new Set(errors)), links }
}

export function DocumentForm({
  allowedFileTypes = [],
  categoryOptions,
  initialValue,
  isSubmitting = false,
  mode,
  onCancel,
  onSubmit,
}: DocumentFormProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getDocumentCopy(locale)
  const form = useForm<DocumentFormValues>({
    defaultValues: getDefaultValues(initialValue),
  })
  const [linkRows, setLinkRows] = useState<DocumentLinkFormRow[]>(() =>
    getInitialLinks(initialValue),
  )
  const [linkErrors, setLinkErrors] = useState<string[]>([])
  const errorSummary = buildErrorSummary(form.formState.errors, linkErrors, copy)
  const entityTypeOptions = useMemo(
    () =>
      documentLinkedEntityTypes.map((entityType) => ({
        label: copy.terms.entityTypes[entityType],
        value: entityType,
      })),
    [copy],
  )
  const accept = allowedFileTypes.join(',')

  useEffect(() => {
    form.reset(getDefaultValues(initialValue))
    setLinkRows(getInitialLinks(initialValue))
    setLinkErrors([])
  }, [form, initialValue])

  function updateLinkRow(index: number, nextRow: Partial<DocumentLinkFormRow>) {
    setLinkRows((currentRows) =>
      currentRows.map((row, rowIndex) => (rowIndex === index ? { ...row, ...nextRow } : row)),
    )
  }

  function removeLinkRow(index: number) {
    setLinkRows((currentRows) => {
      const nextRows = currentRows.filter((_, rowIndex) => rowIndex !== index)

      return nextRows.length > 0 ? nextRows : [{ entityId: '', entityType: '', label: '' }]
    })
  }

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        setLinkErrors([])
        const result = documentFormSchema.safeParse(values)
        const normalizedLinks = normalizeLinks(linkRows, copy)

        if (normalizedLinks.errors.length > 0) {
          setLinkErrors(normalizedLinks.errors)
        }

        if (!result.success) {
          for (const issue of result.error.issues) {
            const field = issue.path[0]

            if (typeof field === 'string' && field in values) {
              form.setError(field as keyof DocumentFormValues, { message: issue.message })
            }
          }
        }

        const file = values.file?.item(0) ?? undefined

        if (mode === 'create' && !file) {
          form.setError('file', { message: 'fileRequired' })
        }

        if (!result.success || normalizedLinks.errors.length > 0 || (mode === 'create' && !file)) {
          return
        }

        if (mode === 'create') {
          await onSubmit({
            mode,
            values: {
              category: result.data.category,
              description: optionalText(values.description),
              file: file!,
              links: normalizedLinks.links,
              title: result.data.title,
              versionNotes: optionalText(values.versionNotes),
            },
          })
          return
        }

        await onSubmit({
          mode,
          values: {
            category: result.data.category,
            concurrencyToken: initialValue?.concurrencyToken,
            description: optionalText(values.description),
            links: normalizedLinks.links,
            title: result.data.title,
          },
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{
          category: copy.form.category,
          file: copy.form.file,
          links: copy.form.linksSection,
          title: copy.form.title,
        }}
        title={copy.form.validationTitle}
      />

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.metadataSection}</legend>
        <FormField error={errorSummary.title} label={copy.form.title} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <TextInput
              {...form.register('title')}
              id={id}
              aria-describedby={describedBy}
              autoComplete="off"
              isInvalid={isInvalid}
              required={isRequired}
            />
          )}
        </FormField>

        <FormField error={errorSummary.category} label={copy.form.category} required>
          {({ describedBy, id, isInvalid, isRequired }) => (
            <SelectInput
              {...form.register('category')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
              options={categoryOptions}
              required={isRequired}
            />
          )}
        </FormField>

        <FormField label={copy.form.description}>
          {({ describedBy, id, isInvalid }) => (
            <TextAreaInput
              {...form.register('description')}
              id={id}
              aria-describedby={describedBy}
              isInvalid={isInvalid}
            />
          )}
        </FormField>

        {mode === 'create' ? (
          <>
            <FormField error={errorSummary.file} label={copy.form.file} required>
              {({ describedBy, id, isInvalid, isRequired }) => (
                <TextInput
                  {...form.register('file')}
                  id={id}
                  accept={accept || undefined}
                  aria-describedby={describedBy}
                  isInvalid={isInvalid}
                  required={isRequired}
                  type="file"
                />
              )}
            </FormField>

            <FormField label={copy.form.versionNotes}>
              {({ describedBy, id, isInvalid }) => (
                <TextAreaInput
                  {...form.register('versionNotes')}
                  id={id}
                  aria-describedby={describedBy}
                  isInvalid={isInvalid}
                />
              )}
            </FormField>
          </>
        ) : null}
      </fieldset>

      <fieldset style={fieldsetStyle}>
        <legend style={{ fontWeight: 800, paddingInline: 4 }}>{copy.form.linksSection}</legend>
        <div style={{ display: 'grid', gap: 12 }}>
          {linkRows.map((row, index) => (
            <div key={index} style={{ display: 'grid', gap: 10 }}>
              <div style={fieldGridStyle}>
                <FormField label={copy.form.entityType}>
                  {({ describedBy, id, isInvalid }) => (
                    <SelectInput
                      id={id}
                      aria-describedby={describedBy}
                      isInvalid={isInvalid}
                      onChange={(event) =>
                        updateLinkRow(index, {
                          entityType: event.currentTarget.value as DocumentLinkedEntityType | '',
                        })
                      }
                      options={entityTypeOptions}
                      placeholder={copy.form.entityType}
                      value={row.entityType}
                    />
                  )}
                </FormField>
                <FormField label={copy.form.entityId}>
                  {({ describedBy, id, isInvalid }) => (
                    <TextInput
                      id={id}
                      aria-describedby={describedBy}
                      autoComplete="off"
                      isInvalid={isInvalid}
                      onChange={(event) =>
                        updateLinkRow(index, { entityId: event.currentTarget.value })
                      }
                      value={row.entityId}
                    />
                  )}
                </FormField>
              </div>

              <div
                style={{
                  alignItems: 'end',
                  display: 'grid',
                  gap: 10,
                  gridTemplateColumns: '1fr auto',
                }}
              >
                <FormField label={copy.form.linkLabel}>
                  {({ describedBy, id, isInvalid }) => (
                    <TextInput
                      id={id}
                      aria-describedby={describedBy}
                      autoComplete="off"
                      isInvalid={isInvalid}
                      onChange={(event) =>
                        updateLinkRow(index, { label: event.currentTarget.value })
                      }
                      value={row.label}
                    />
                  )}
                </FormField>
                <ActionButton
                  aria-label={copy.form.removeLink}
                  icon={<Trash2 aria-hidden="true" size={16} />}
                  onClick={() => removeLinkRow(index)}
                  size="sm"
                  type="button"
                >
                  {copy.form.removeLink}
                </ActionButton>
              </div>
            </div>
          ))}
        </div>

        <ActionButton
          icon={<Plus aria-hidden="true" size={16} />}
          onClick={() =>
            setLinkRows((currentRows) => [
              ...currentRows,
              { entityId: '', entityType: '', label: '' },
            ])
          }
          size="sm"
          type="button"
        >
          {copy.form.addLink}
        </ActionButton>
      </fieldset>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, justifyContent: 'flex-end' }}>
        {onCancel ? (
          <ActionButton onClick={onCancel} type="button">
            {copy.form.cancel}
          </ActionButton>
        ) : null}
        <ActionButton isLoading={isSubmitting} tone="primary" type="submit">
          {mode === 'create' ? copy.form.submitCreate : copy.form.submitEdit}
        </ActionButton>
      </div>
    </form>
  )
}
