import type { ButtonHTMLAttributes, CSSProperties, ReactNode } from 'react'
import { PrimaryActionButton } from './actions'
import { cx, mergeStyles, subtleTextStyle } from './utils'

export type PageHeaderPrimaryAction = Omit<
  ButtonHTMLAttributes<HTMLButtonElement>,
  'children' | 'className'
> & {
  icon?: ReactNode
  isLoading?: boolean
  label: ReactNode
}

export type PageHeaderProps = {
  actions?: ReactNode
  className?: string
  description?: ReactNode
  eyebrow?: ReactNode
  primaryAction?: PageHeaderPrimaryAction
  style?: CSSProperties
  title: ReactNode
}

const headerStyle: CSSProperties = {
  alignItems: 'flex-start',
  display: 'flex',
  flexWrap: 'wrap',
  gap: 16,
  justifyContent: 'space-between',
  width: '100%',
}

const copyStyle: CSSProperties = {
  display: 'grid',
  gap: 6,
  minWidth: 240,
}

const actionsStyle: CSSProperties = {
  alignItems: 'center',
  display: 'flex',
  flexWrap: 'wrap',
  gap: 10,
  justifyContent: 'flex-end',
}

export function PageHeader({
  actions,
  className,
  description,
  eyebrow,
  primaryAction,
  style,
  title,
}: PageHeaderProps) {
  const renderedPrimaryAction = primaryAction
    ? (() => {
        const { icon, isLoading, label, ...buttonProps } = primaryAction

        return (
          <PrimaryActionButton {...buttonProps} icon={icon} isLoading={isLoading}>
            {label}
          </PrimaryActionButton>
        )
      })()
    : null

  return (
    <header className={cx('als-page-header', className)} style={mergeStyles(headerStyle, style)}>
      <div className="als-page-header__copy" style={copyStyle}>
        {eyebrow ? (
          <p
            className="als-page-header__eyebrow"
            style={{
              color: 'var(--als-color-primary, #1877f2)',
              fontSize: '0.75rem',
              fontWeight: 800,
              letterSpacing: 0,
              lineHeight: 1.2,
              margin: 0,
              textTransform: 'uppercase',
            }}
          >
            {eyebrow}
          </p>
        ) : null}

        <h1
          className="als-page-header__title"
          style={{
            color: 'var(--als-color-text, #1f2937)',
            fontSize: 'clamp(1.55rem, 2.4vw, 2.25rem)',
            lineHeight: 1.12,
            margin: 0,
          }}
        >
          {title}
        </h1>

        {description ? (
          <p
            className="als-page-header__description"
            style={mergeStyles(subtleTextStyle, {
              fontSize: '0.98rem',
              lineHeight: 1.55,
              margin: 0,
              maxWidth: 760,
            })}
          >
            {description}
          </p>
        ) : null}
      </div>

      {primaryAction || actions ? (
        <div className="als-page-header__actions" style={actionsStyle}>
          {actions}
          {renderedPrimaryAction}
        </div>
      ) : null}
    </header>
  )
}
