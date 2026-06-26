import {
  forwardRef,
  type CSSProperties,
  type InputHTMLAttributes,
  type LabelHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from 'react'
import { AlertCircle } from 'lucide-react'
import {
  composeIds,
  cx,
  mergeStyles,
  subtleTextStyle,
  useComponentId,
  visuallyHiddenStyle,
} from './utils'

export type ValidationMessages = string | string[] | null | undefined

function normalizeMessages(messages: ValidationMessages) {
  if (!messages) {
    return []
  }

  return Array.isArray(messages) ? messages : [messages]
}

export type ValidationMessageProps = {
  className?: string
  id?: string
  messages?: ValidationMessages
  style?: CSSProperties
  tone?: 'error' | 'hint' | 'success'
}

const validationToneStyles: Record<NonNullable<ValidationMessageProps['tone']>, CSSProperties> = {
  error: {
    color: 'var(--als-color-danger, #b42318)',
  },
  hint: subtleTextStyle,
  success: {
    color: 'var(--als-color-success, #027a48)',
  },
}

export function ValidationMessage({
  className,
  id,
  messages,
  style,
  tone = 'error',
}: ValidationMessageProps) {
  const normalizedMessages = normalizeMessages(messages)

  if (normalizedMessages.length === 0) {
    return null
  }

  return (
    <div
      id={id}
      className={cx('als-validation-message', `als-validation-message--${tone}`, className)}
      role={tone === 'error' ? 'alert' : undefined}
      style={mergeStyles(
        {
          alignItems: 'flex-start',
          display: 'grid',
          fontSize: '0.86rem',
          gap: 4,
          lineHeight: 1.45,
        },
        validationToneStyles[tone],
        style,
      )}
    >
      {normalizedMessages.map((message) => (
        <span key={message} className="als-validation-message__item">
          {message}
        </span>
      ))}
    </div>
  )
}

export type FormFieldRenderProps = {
  describedBy?: string
  id: string
  isInvalid: boolean
  isRequired: boolean
}

export type FormFieldProps = {
  children: (props: FormFieldRenderProps) => ReactNode
  className?: string
  description?: ReactNode
  error?: ValidationMessages
  hideLabel?: boolean
  id?: string
  label: ReactNode
  labelProps?: LabelHTMLAttributes<HTMLLabelElement>
  required?: boolean
  style?: CSSProperties
}

export function FormField({
  children,
  className,
  description,
  error,
  hideLabel = false,
  id,
  label,
  labelProps,
  required = false,
  style,
}: FormFieldProps) {
  const fieldId = useComponentId('als-field', id)
  const descriptionId = description ? `${fieldId}-description` : undefined
  const errorId = normalizeMessages(error).length > 0 ? `${fieldId}-error` : undefined
  const describedBy = composeIds(descriptionId, errorId)

  return (
    <div
      className={cx('als-form-field', className)}
      style={mergeStyles({ display: 'grid', gap: 7 }, style)}
    >
      <label
        {...labelProps}
        className={cx('als-form-field__label', labelProps?.className)}
        htmlFor={fieldId}
        style={mergeStyles(
          {
            color: 'var(--als-color-text, #1f2937)',
            fontSize: '0.92rem',
            fontWeight: 800,
            lineHeight: 1.35,
          },
          hideLabel ? visuallyHiddenStyle : undefined,
          labelProps?.style,
        )}
      >
        {label}
        {required ? (
          <>
            <span aria-hidden="true" className="als-form-field__required">
              {' '}
              *
            </span>
            <span style={visuallyHiddenStyle}>obrigatório</span>
          </>
        ) : null}
      </label>

      {children({
        describedBy,
        id: fieldId,
        isInvalid: Boolean(errorId),
        isRequired: required,
      })}

      {description ? (
        <p
          id={descriptionId}
          className="als-form-field__description"
          style={mergeStyles(subtleTextStyle, { fontSize: '0.86rem', lineHeight: 1.45, margin: 0 })}
        >
          {description}
        </p>
      ) : null}

      <ValidationMessage id={errorId} messages={error} />
    </div>
  )
}

export type TextInputProps = InputHTMLAttributes<HTMLInputElement> & {
  isInvalid?: boolean
}

const controlStyle: CSSProperties = {
  background: 'var(--als-color-surface, #ffffff)',
  border: '1px solid var(--als-color-border, #d7deea)',
  borderRadius: 'var(--als-radius-md, 8px)',
  color: 'var(--als-color-text, #1f2937)',
  font: 'inherit',
  minHeight: 44,
  padding: '0 12px',
  width: '100%',
}

export const TextInput = forwardRef<HTMLInputElement, TextInputProps>(
  ({ className, isInvalid = false, style, ...props }, ref) => (
    <input
      ref={ref}
      aria-invalid={isInvalid || undefined}
      className={cx('als-text-input', isInvalid && 'als-text-input--invalid', className)}
      style={mergeStyles(
        controlStyle,
        isInvalid ? { borderColor: 'var(--als-color-danger, #b42318)' } : undefined,
        style,
      )}
      {...props}
    />
  ),
)

TextInput.displayName = 'TextInput'

export type TextAreaInputProps = TextareaHTMLAttributes<HTMLTextAreaElement> & {
  isInvalid?: boolean
}

export const TextAreaInput = forwardRef<HTMLTextAreaElement, TextAreaInputProps>(
  ({ className, isInvalid = false, style, ...props }, ref) => (
    <textarea
      ref={ref}
      aria-invalid={isInvalid || undefined}
      className={cx('als-textarea-input', isInvalid && 'als-textarea-input--invalid', className)}
      style={mergeStyles(
        controlStyle,
        {
          minHeight: 112,
          paddingBlock: 10,
          resize: 'vertical',
        },
        isInvalid ? { borderColor: 'var(--als-color-danger, #b42318)' } : undefined,
        style,
      )}
      {...props}
    />
  ),
)

TextAreaInput.displayName = 'TextAreaInput'

export type SelectOption = {
  disabled?: boolean
  label: ReactNode
  value: string
}

export type SelectInputProps = SelectHTMLAttributes<HTMLSelectElement> & {
  isInvalid?: boolean
  options: SelectOption[]
  placeholder?: string
}

export const SelectInput = forwardRef<HTMLSelectElement, SelectInputProps>(
  ({ className, isInvalid = false, options, placeholder, style, ...props }, ref) => (
    <select
      ref={ref}
      aria-invalid={isInvalid || undefined}
      className={cx('als-select-input', isInvalid && 'als-select-input--invalid', className)}
      style={mergeStyles(
        controlStyle,
        isInvalid ? { borderColor: 'var(--als-color-danger, #b42318)' } : undefined,
        style,
      )}
      {...props}
    >
      {placeholder ? <option value="">{placeholder}</option> : null}
      {options.map((option) => (
        <option key={option.value} disabled={option.disabled} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  ),
)

SelectInput.displayName = 'SelectInput'

export type CheckboxInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> & {
  description?: ReactNode
  error?: ValidationMessages
  label: ReactNode
}

export const CheckboxInput = forwardRef<HTMLInputElement, CheckboxInputProps>(
  ({ className, description, error, id, label, style, ...props }, ref) => {
    const inputId = useComponentId('als-checkbox', id)
    const descriptionId = description ? `${inputId}-description` : undefined
    const errorId = normalizeMessages(error).length > 0 ? `${inputId}-error` : undefined
    const describedBy = composeIds(descriptionId, errorId)

    return (
      <div
        className={cx('als-checkbox-input', className)}
        style={mergeStyles({ display: 'grid', gap: 6 }, style)}
      >
        <label
          className="als-checkbox-input__label"
          htmlFor={inputId}
          style={{ alignItems: 'flex-start', display: 'flex', gap: 10 }}
        >
          <input
            ref={ref}
            id={inputId}
            aria-describedby={describedBy}
            aria-invalid={Boolean(errorId) || undefined}
            className="als-checkbox-input__control"
            style={{
              accentColor: 'var(--als-color-primary, #1877f2)',
              height: 20,
              margin: 0,
              marginTop: 2,
              width: 20,
            }}
            type="checkbox"
            {...props}
          />

          <span className="als-checkbox-input__text" style={{ display: 'grid', gap: 4 }}>
            <span style={{ color: 'var(--als-color-text, #1f2937)', fontWeight: 800 }}>
              {label}
            </span>
            {description ? (
              <span
                id={descriptionId}
                className="als-checkbox-input__description"
                style={mergeStyles(subtleTextStyle, { fontSize: '0.86rem', lineHeight: 1.45 })}
              >
                {description}
              </span>
            ) : null}
          </span>
        </label>

        <ValidationMessage id={errorId} messages={error} />
      </div>
    )
  },
)

CheckboxInput.displayName = 'CheckboxInput'

export type ValidationSummaryProps = {
  className?: string
  errors: Record<string, string[]>
  fieldLabels?: Record<string, string>
  style?: CSSProperties
  title?: ReactNode
}

export function ValidationSummary({
  className,
  errors,
  fieldLabels = {},
  style,
  title = 'Revise os campos destacados',
}: ValidationSummaryProps) {
  const entries = Object.entries(errors).filter(([, messages]) => messages.length > 0)

  if (entries.length === 0) {
    return null
  }

  return (
    <div
      className={cx('als-validation-summary', className)}
      role="alert"
      style={mergeStyles(
        {
          background: 'var(--als-color-danger-soft, #fef3f2)',
          border: '1px solid var(--als-color-danger-border, #fecdca)',
          borderRadius: 'var(--als-radius-md, 8px)',
          color: 'var(--als-color-danger, #b42318)',
          display: 'grid',
          gap: 10,
          padding: 14,
        },
        style,
      )}
    >
      <strong
        className="als-validation-summary__title"
        style={{ alignItems: 'center', display: 'flex', gap: 8 }}
      >
        <AlertCircle aria-hidden="true" size={18} />
        {title}
      </strong>

      <ul className="als-validation-summary__list" style={{ margin: 0, paddingInlineStart: 20 }}>
        {entries.map(([field, messages]) => (
          <li key={field} className="als-validation-summary__item">
            <a className="als-validation-summary__link" href={`#${field}`}>
              {fieldLabels[field] ?? field}
            </a>
            : {messages.join(' ')}
          </li>
        ))}
      </ul>
    </div>
  )
}
