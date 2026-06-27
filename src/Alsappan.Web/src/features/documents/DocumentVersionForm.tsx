import { useForm, type FieldErrors } from 'react-hook-form'
import {
  ActionButton,
  FormField,
  TextAreaInput,
  TextInput,
  ValidationSummary,
} from '../../components'
import type { DocumentVersionUploadRequest } from '../../lib/api/documents'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { getDocumentCopy } from './documentCopy'

export type DocumentVersionFormProps = {
  allowedFileTypes?: string[]
  isSubmitting?: boolean
  onCancel?: () => void
  onSubmit: (values: DocumentVersionUploadRequest) => Promise<void> | void
}

type DocumentVersionFormValues = {
  file?: FileList
  notes: string
}

function optionalText(value: string) {
  const trimmedValue = value.trim()

  return trimmedValue.length > 0 ? trimmedValue : undefined
}

function getMessage(key: string | undefined, copy: ReturnType<typeof getDocumentCopy>) {
  if (key === 'fileRequired') {
    return copy.form.fileRequired
  }

  return copy.form.fileRequired
}

function buildErrorSummary(
  errors: FieldErrors<DocumentVersionFormValues>,
  copy: ReturnType<typeof getDocumentCopy>,
) {
  return Object.entries(errors).reduce<Record<string, string[]>>((summary, [field, error]) => {
    summary[field] = [getMessage(String(error?.message), copy)]

    return summary
  }, {})
}

export function DocumentVersionForm({
  allowedFileTypes = [],
  isSubmitting = false,
  onCancel,
  onSubmit,
}: DocumentVersionFormProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getDocumentCopy(locale)
  const form = useForm<DocumentVersionFormValues>({
    defaultValues: {
      notes: '',
    },
  })
  const errorSummary = buildErrorSummary(form.formState.errors, copy)

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit(async (values) => {
        form.clearErrors()
        const file = values.file?.item(0) ?? undefined

        if (!file) {
          form.setError('file', { message: 'fileRequired' })
          return
        }

        await onSubmit({
          file,
          notes: optionalText(values.notes),
        })
      })}
      style={{ display: 'grid', gap: 16 }}
    >
      <ValidationSummary
        errors={errorSummary}
        fieldLabels={{ file: copy.form.file }}
        title={copy.form.validationTitle}
      />

      <FormField error={errorSummary.file} label={copy.form.file} required>
        {({ describedBy, id, isInvalid, isRequired }) => (
          <TextInput
            {...form.register('file')}
            id={id}
            accept={allowedFileTypes.join(',') || undefined}
            aria-describedby={describedBy}
            isInvalid={isInvalid}
            required={isRequired}
            type="file"
          />
        )}
      </FormField>

      <FormField label={copy.form.notes}>
        {({ describedBy, id, isInvalid }) => (
          <TextAreaInput
            {...form.register('notes')}
            id={id}
            aria-describedby={describedBy}
            isInvalid={isInvalid}
          />
        )}
      </FormField>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, justifyContent: 'flex-end' }}>
        {onCancel ? (
          <ActionButton onClick={onCancel} type="button">
            {copy.form.cancel}
          </ActionButton>
        ) : null}
        <ActionButton isLoading={isSubmitting} tone="primary" type="submit">
          {copy.detail.actions.uploadVersion}
        </ActionButton>
      </div>
    </form>
  )
}
