import type { CSSProperties, ReactNode } from 'react'
import {
  AlertTriangle,
  Archive,
  Inbox,
  LoaderCircle,
  RefreshCw,
  RotateCw,
  SearchX,
  ShieldAlert,
} from 'lucide-react'
import { ActionButton } from './actions'
import { cx, mergeStyles, subtleTextStyle, surfaceStyle } from './utils'

export type FeedbackStateAction = {
  icon?: ReactNode
  label: ReactNode
  onClick: () => void
}

export type FeedbackStateProps = {
  action?: FeedbackStateAction
  className?: string
  description?: ReactNode
  icon?: ReactNode
  role?: 'alert' | 'status'
  style?: CSSProperties
  title: ReactNode
  tone?: 'neutral' | 'danger' | 'warning' | 'info'
}

const stateToneStyles: Record<NonNullable<FeedbackStateProps['tone']>, CSSProperties> = {
  danger: {
    background: 'var(--als-color-danger-soft, #fef3f2)',
    color: 'var(--als-color-danger, #b42318)',
  },
  info: {
    background: 'var(--als-color-info-soft, #e8f2ff)',
    color: 'var(--als-color-info, #175cd3)',
  },
  neutral: {
    background: 'var(--als-color-surface-muted, #f8fafc)',
    color: 'var(--als-color-text-muted, #64748b)',
  },
  warning: {
    background: 'var(--als-color-warning-soft, #fffaeb)',
    color: 'var(--als-color-warning, #b54708)',
  },
}

export function FeedbackState({
  action,
  className,
  description,
  icon,
  role = 'status',
  style,
  title,
  tone = 'neutral',
}: FeedbackStateProps) {
  return (
    <section
      className={cx('als-feedback-state', `als-feedback-state--${tone}`, className)}
      role={role}
      style={mergeStyles(
        {
          alignItems: 'center',
          display: 'grid',
          gap: 12,
          justifyItems: 'center',
          minHeight: 220,
          padding: 24,
          textAlign: 'center',
        },
        style,
      )}
    >
      {icon ? (
        <div
          aria-hidden="true"
          className="als-feedback-state__icon"
          style={mergeStyles(stateToneStyles[tone], {
            alignItems: 'center',
            borderRadius: 'var(--als-radius-md, 8px)',
            display: 'inline-flex',
            height: 48,
            justifyContent: 'center',
            width: 48,
          })}
        >
          {icon}
        </div>
      ) : null}

      <div
        className="als-feedback-state__copy"
        style={{ display: 'grid', gap: 6, justifyItems: 'center' }}
      >
        <h2
          className="als-feedback-state__title"
          style={{
            color: 'var(--als-color-text, #1f2937)',
            fontSize: '1.15rem',
            lineHeight: 1.25,
            margin: 0,
          }}
        >
          {title}
        </h2>

        {description ? (
          <p
            className="als-feedback-state__description"
            style={mergeStyles(subtleTextStyle, { lineHeight: 1.55, margin: 0, maxWidth: 540 })}
          >
            {description}
          </p>
        ) : null}
      </div>

      {action ? (
        <ActionButton icon={action.icon} onClick={action.onClick}>
          {action.label}
        </ActionButton>
      ) : null}
    </section>
  )
}

export type LoadingStateProps = {
  className?: string
  description?: ReactNode
  style?: CSSProperties
  title?: ReactNode
}

export function LoadingState({
  className,
  description,
  style,
  title = 'Carregando',
}: LoadingStateProps) {
  return (
    <FeedbackState
      className={cx('als-loading-state', className)}
      description={description}
      icon={<LoaderCircle className="als-loading-state__spinner" size={24} />}
      style={style}
      title={title}
    />
  )
}

export type EmptyStateProps = Omit<FeedbackStateProps, 'icon' | 'role' | 'title'> & {
  title?: ReactNode
}

export function EmptyState({ title = 'Nenhum registro encontrado', ...props }: EmptyStateProps) {
  return <FeedbackState icon={<Inbox size={24} />} title={title} {...props} />
}

export type ErrorStateProps = Omit<
  FeedbackStateProps,
  'action' | 'icon' | 'role' | 'title' | 'tone'
> & {
  onRetry?: () => void
  retryLabel?: ReactNode
  title?: ReactNode
}

export function ErrorState({
  onRetry,
  retryLabel = 'Tentar novamente',
  title = 'Não foi possível carregar os dados',
  ...props
}: ErrorStateProps) {
  return (
    <FeedbackState
      action={
        onRetry
          ? { icon: <RotateCw aria-hidden="true" size={16} />, label: retryLabel, onClick: onRetry }
          : undefined
      }
      icon={<AlertTriangle size={24} />}
      role="alert"
      title={title}
      tone="danger"
      {...props}
    />
  )
}

export type ForbiddenStateProps = Omit<FeedbackStateProps, 'icon' | 'role' | 'title' | 'tone'> & {
  title?: ReactNode
}

export function ForbiddenState({ title = 'Acesso não autorizado', ...props }: ForbiddenStateProps) {
  return (
    <FeedbackState
      icon={<ShieldAlert size={24} />}
      role="alert"
      title={title}
      tone="warning"
      {...props}
    />
  )
}

export type NotFoundStateProps = Omit<FeedbackStateProps, 'icon' | 'role' | 'title' | 'tone'> & {
  title?: ReactNode
}

export function NotFoundState({ title = 'Registro não encontrado', ...props }: NotFoundStateProps) {
  return <FeedbackState icon={<SearchX size={24} />} title={title} tone="info" {...props} />
}

export type ArchivedStateProps = Omit<FeedbackStateProps, 'icon' | 'role' | 'title' | 'tone'> & {
  title?: ReactNode
}

export function ArchivedState({ title = 'Registro arquivado', ...props }: ArchivedStateProps) {
  return <FeedbackState icon={<Archive size={24} />} title={title} tone="neutral" {...props} />
}

export type OptimisticRefreshStateProps = {
  className?: string
  description?: ReactNode
  isRefreshing?: boolean
  retryLabel?: ReactNode
  onRetry?: () => void
  style?: CSSProperties
  title?: ReactNode
}

export function OptimisticRefreshState({
  className,
  description,
  isRefreshing = false,
  onRetry,
  retryLabel = 'Atualizar agora',
  style,
  title = 'Atualizando dados em segundo plano',
}: OptimisticRefreshStateProps) {
  return (
    <aside
      className={cx(
        'als-optimistic-refresh-state',
        isRefreshing && 'als-optimistic-refresh-state--active',
        className,
      )}
      role="status"
      style={mergeStyles(
        surfaceStyle,
        {
          alignItems: 'center',
          display: 'flex',
          flexWrap: 'wrap',
          gap: 12,
          justifyContent: 'space-between',
          padding: 12,
        },
        style,
      )}
    >
      <div
        className="als-optimistic-refresh-state__copy"
        style={{ alignItems: 'center', display: 'flex', gap: 10, minWidth: 0 }}
      >
        <RefreshCw
          aria-hidden="true"
          className="als-optimistic-refresh-state__icon"
          color="var(--als-color-primary, #1877f2)"
          size={18}
        />
        <div style={{ display: 'grid', gap: 2, minWidth: 0 }}>
          <strong className="als-optimistic-refresh-state__title">{title}</strong>
          {description ? (
            <span
              className="als-optimistic-refresh-state__description"
              style={mergeStyles(subtleTextStyle, { fontSize: '0.86rem', lineHeight: 1.4 })}
            >
              {description}
            </span>
          ) : null}
        </div>
      </div>

      {onRetry ? (
        <ActionButton disabled={isRefreshing} onClick={onRetry} size="sm">
          {retryLabel}
        </ActionButton>
      ) : null}
    </aside>
  )
}
