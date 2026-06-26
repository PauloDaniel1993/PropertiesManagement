import {
  useEffect,
  useRef,
  type CSSProperties,
  type KeyboardEvent,
  type ReactNode,
  type RefObject,
} from 'react'
import { X } from 'lucide-react'
import { cx, mergeStyles, subtleTextStyle, surfaceStyle, useComponentId } from './utils'

const focusableSelector = [
  'a[href]',
  'button:not([disabled])',
  'textarea:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',')

function getFocusableElements(container: HTMLElement) {
  return Array.from(container.querySelectorAll<HTMLElement>(focusableSelector)).filter(
    (element) =>
      !element.hasAttribute('disabled') && element.getAttribute('aria-hidden') !== 'true',
  )
}

type OverlayPanelProps = {
  children: ReactNode
  className?: string
  closeLabel?: string
  description?: ReactNode
  footer?: ReactNode
  initialFocusRef?: RefObject<HTMLElement | null>
  isOpen: boolean
  onOpenChange: (isOpen: boolean) => void
  panelClassName?: string
  panelStyle?: CSSProperties
  preventBackdropClose?: boolean
  style?: CSSProperties
  title: ReactNode
  titlePrefix: string
}

function OverlayPanel({
  children,
  className,
  closeLabel = 'Fechar',
  description,
  footer,
  initialFocusRef,
  isOpen,
  onOpenChange,
  panelClassName,
  panelStyle,
  preventBackdropClose = false,
  style,
  title,
  titlePrefix,
}: OverlayPanelProps) {
  const panelRef = useRef<HTMLDivElement>(null)
  const previousFocusRef = useRef<Element | null>(null)
  const titleId = useComponentId(`${titlePrefix}-title`)
  const descriptionId = description ? `${titleId}-description` : undefined

  useEffect(() => {
    if (!isOpen) {
      return undefined
    }

    previousFocusRef.current = document.activeElement
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    const focusTarget =
      initialFocusRef?.current ?? getFocusableElements(panelRef.current!).at(0) ?? panelRef.current
    focusTarget?.focus()

    return () => {
      document.body.style.overflow = previousOverflow

      if (previousFocusRef.current instanceof HTMLElement) {
        previousFocusRef.current.focus()
      }
    }
  }, [initialFocusRef, isOpen])

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      onOpenChange(false)
      return
    }

    if (event.key !== 'Tab' || !panelRef.current) {
      return
    }

    const focusableElements = getFocusableElements(panelRef.current)

    if (focusableElements.length === 0) {
      event.preventDefault()
      panelRef.current.focus()
      return
    }

    const firstElement = focusableElements[0]
    const lastElement = focusableElements.at(-1)

    if (event.shiftKey && document.activeElement === firstElement) {
      event.preventDefault()
      lastElement?.focus()
    }

    if (!event.shiftKey && document.activeElement === lastElement) {
      event.preventDefault()
      firstElement.focus()
    }
  }

  if (!isOpen) {
    return null
  }

  return (
    <div
      className={cx('als-overlay', className)}
      style={mergeStyles(
        {
          inset: 0,
          position: 'fixed',
          zIndex: 1000,
        },
        style,
      )}
    >
      <div
        aria-hidden="true"
        className="als-overlay__backdrop"
        onClick={() => {
          if (!preventBackdropClose) {
            onOpenChange(false)
          }
        }}
        style={{
          background: 'rgba(15, 23, 42, 0.44)',
          inset: 0,
          position: 'absolute',
        }}
      />

      <div
        ref={panelRef}
        aria-describedby={descriptionId}
        aria-labelledby={titleId}
        aria-modal="true"
        className={cx('als-overlay__panel', panelClassName)}
        onKeyDown={handleKeyDown}
        role="dialog"
        style={mergeStyles(
          surfaceStyle,
          {
            boxShadow: '0 24px 70px rgba(15, 23, 42, 0.24)',
            display: 'grid',
            gap: 18,
            maxHeight: 'calc(100vh - 32px)',
            overflow: 'auto',
            padding: 22,
            position: 'absolute',
          },
          panelStyle,
        )}
        tabIndex={-1}
      >
        <div
          className="als-overlay__header"
          style={{
            alignItems: 'flex-start',
            display: 'flex',
            gap: 12,
            justifyContent: 'space-between',
          }}
        >
          <div className="als-overlay__copy" style={{ display: 'grid', gap: 6 }}>
            <h2
              id={titleId}
              className="als-overlay__title"
              style={{
                color: 'var(--als-color-text, #1f2937)',
                fontSize: '1.18rem',
                lineHeight: 1.25,
                margin: 0,
              }}
            >
              {title}
            </h2>

            {description ? (
              <p
                id={descriptionId}
                className="als-overlay__description"
                style={mergeStyles(subtleTextStyle, { lineHeight: 1.5, margin: 0 })}
              >
                {description}
              </p>
            ) : null}
          </div>

          <button
            aria-label={closeLabel}
            className="als-overlay__close"
            onClick={() => onOpenChange(false)}
            style={{
              alignItems: 'center',
              background: 'transparent',
              border: '1px solid transparent',
              borderRadius: 'var(--als-radius-sm, 6px)',
              color: 'var(--als-color-text-muted, #64748b)',
              display: 'inline-flex',
              justifyContent: 'center',
              minHeight: 36,
              minWidth: 36,
              padding: 0,
            }}
            type="button"
          >
            <X aria-hidden="true" size={18} />
          </button>
        </div>

        <div className="als-overlay__body">{children}</div>

        {footer ? (
          <footer
            className="als-overlay__footer"
            style={{
              alignItems: 'center',
              display: 'flex',
              flexWrap: 'wrap',
              gap: 10,
              justifyContent: 'flex-end',
            }}
          >
            {footer}
          </footer>
        ) : null}
      </div>
    </div>
  )
}

export type DialogProps = Omit<OverlayPanelProps, 'panelStyle' | 'titlePrefix'> & {
  panelStyle?: CSSProperties
  size?: 'sm' | 'md' | 'lg'
}

const dialogSizeStyles: Record<NonNullable<DialogProps['size']>, CSSProperties> = {
  lg: {
    width: 'min(760px, calc(100vw - 32px))',
  },
  md: {
    width: 'min(560px, calc(100vw - 32px))',
  },
  sm: {
    width: 'min(420px, calc(100vw - 32px))',
  },
}

export function Dialog({ panelClassName, panelStyle, size = 'md', ...props }: DialogProps) {
  return (
    <OverlayPanel
      panelClassName={cx('als-dialog', panelClassName)}
      panelStyle={mergeStyles(
        {
          left: '50%',
          top: '50%',
          transform: 'translate(-50%, -50%)',
        },
        dialogSizeStyles[size],
        panelStyle,
      )}
      titlePrefix="als-dialog"
      {...props}
    />
  )
}

export type DrawerProps = Omit<OverlayPanelProps, 'panelStyle' | 'titlePrefix'> & {
  panelStyle?: CSSProperties
  side?: 'left' | 'right'
  width?: CSSProperties['width']
}

export function Drawer({
  panelClassName,
  panelStyle,
  side = 'right',
  width = 460,
  ...props
}: DrawerProps) {
  return (
    <OverlayPanel
      panelClassName={cx('als-drawer', `als-drawer--${side}`, panelClassName)}
      panelStyle={mergeStyles(
        {
          bottom: 0,
          maxHeight: '100vh',
          top: 0,
          width: `min(${typeof width === 'number' ? `${width}px` : width}, 100vw)`,
          ...(side === 'left' ? { left: 0 } : { right: 0 }),
        },
        panelStyle,
      )}
      titlePrefix="als-drawer"
      {...props}
    />
  )
}
