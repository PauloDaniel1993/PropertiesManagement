import { forwardRef, type ButtonHTMLAttributes, type CSSProperties, type ReactNode } from 'react'
import { LoaderCircle, Plus } from 'lucide-react'
import { cx, mergeStyles } from './utils'

export type ActionButtonTone = 'primary' | 'secondary' | 'ghost' | 'danger'
export type ActionButtonSize = 'sm' | 'md'

export type ActionButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  icon?: ReactNode
  isLoading?: boolean
  loadingLabel?: string
  size?: ActionButtonSize
  tone?: ActionButtonTone
}

const baseButtonStyle: CSSProperties = {
  alignItems: 'center',
  borderRadius: 'var(--als-radius-md, 8px)',
  display: 'inline-flex',
  font: 'inherit',
  fontWeight: 700,
  gap: 8,
  justifyContent: 'center',
  minHeight: 44,
  minWidth: 44,
  textDecoration: 'none',
  transition: 'background-color 120ms ease, border-color 120ms ease, color 120ms ease',
  whiteSpace: 'nowrap',
}

const toneStyles: Record<ActionButtonTone, CSSProperties> = {
  danger: {
    background: 'var(--als-color-danger, #b42318)',
    border: '1px solid var(--als-color-danger, #b42318)',
    color: 'var(--als-color-danger-foreground, #ffffff)',
  },
  ghost: {
    background: 'transparent',
    border: '1px solid transparent',
    color: 'var(--als-color-text, #1f2937)',
  },
  primary: {
    background: 'var(--als-color-primary, #1877f2)',
    border: '1px solid var(--als-color-primary, #1877f2)',
    color: 'var(--als-color-primary-foreground, #ffffff)',
  },
  secondary: {
    background: 'var(--als-color-surface, #ffffff)',
    border: '1px solid var(--als-color-border, #d7deea)',
    color: 'var(--als-color-text, #1f2937)',
  },
}

const sizeStyles: Record<ActionButtonSize, CSSProperties> = {
  md: {
    fontSize: '0.95rem',
    paddingInline: 16,
  },
  sm: {
    fontSize: '0.875rem',
    minHeight: 36,
    paddingInline: 12,
  },
}

export const ActionButton = forwardRef<HTMLButtonElement, ActionButtonProps>(
  (
    {
      children,
      className,
      disabled,
      icon,
      isLoading = false,
      loadingLabel = 'Carregando',
      size = 'md',
      style,
      tone = 'secondary',
      type = 'button',
      ...props
    },
    ref,
  ) => {
    const isDisabled = disabled || isLoading

    return (
      <button
        ref={ref}
        aria-busy={isLoading || undefined}
        aria-disabled={isDisabled || undefined}
        className={cx('als-action-button', `als-action-button--${tone}`, className)}
        disabled={isDisabled}
        style={mergeStyles(
          baseButtonStyle,
          toneStyles[tone],
          sizeStyles[size],
          isDisabled
            ? {
                cursor: 'not-allowed',
                opacity: 0.62,
              }
            : undefined,
          style,
        )}
        type={type}
        {...props}
      >
        {isLoading ? (
          <LoaderCircle aria-hidden="true" className="als-action-button__spinner" size={16} />
        ) : (
          icon
        )}
        {isLoading ? loadingLabel : children}
      </button>
    )
  },
)

ActionButton.displayName = 'ActionButton'

export type PrimaryActionButtonProps = Omit<ActionButtonProps, 'tone'> & {
  icon?: ReactNode
}

export const PrimaryActionButton = forwardRef<HTMLButtonElement, PrimaryActionButtonProps>(
  ({ icon = <Plus aria-hidden="true" size={18} />, ...props }, ref) => (
    <ActionButton ref={ref} icon={icon} tone="primary" {...props} />
  ),
)

PrimaryActionButton.displayName = 'PrimaryActionButton'
