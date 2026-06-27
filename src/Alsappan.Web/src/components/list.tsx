import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type InputHTMLAttributes,
  type ReactNode,
} from 'react'
import {
  ChevronLeft,
  ChevronRight,
  MoreHorizontal,
  Search,
  SlidersHorizontal,
  X,
  type LucideIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { ActionButton } from './actions'
import {
  cx,
  mergeStyles,
  subtleTextStyle,
  surfaceStyle,
  useComponentId,
  visuallyHiddenStyle,
} from './utils'

export type SearchInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> & {
  clearLabel?: string
  hideLabel?: boolean
  inputClassName?: string
  inputStyle?: CSSProperties
  label: string
  onClear?: () => void
  onValueChange?: (value: string) => void
}

const inputShellStyle: CSSProperties = {
  alignItems: 'center',
  display: 'flex',
  gap: 8,
  minHeight: 44,
  minWidth: 260,
  paddingInline: 12,
  width: '100%',
}

const inputStyle: CSSProperties = {
  background: 'transparent',
  border: 0,
  color: 'var(--als-color-text, #1f2937)',
  flex: 1,
  font: 'inherit',
  minWidth: 0,
  outline: 0,
}

export function SearchInput({
  className,
  clearLabel,
  hideLabel = true,
  id,
  inputClassName,
  inputStyle: inputStyleOverride,
  label,
  onChange,
  onClear,
  onValueChange,
  placeholder,
  style,
  value,
  ...props
}: SearchInputProps) {
  const { t } = useTranslation()
  const inputId = useComponentId('als-search-input', id)
  const hasValue = typeof value === 'string' && value.length > 0
  const resolvedClearLabel = clearLabel ?? t('components.search.clear')

  return (
    <div className={cx('als-search-input', className)} style={style}>
      <label
        className="als-search-input__label"
        htmlFor={inputId}
        style={
          hideLabel ? visuallyHiddenStyle : { display: 'block', fontWeight: 700, marginBottom: 6 }
        }
      >
        {label}
      </label>

      <div className="als-search-input__control" style={mergeStyles(surfaceStyle, inputShellStyle)}>
        <Search aria-hidden="true" color="var(--als-color-text-muted, #64748b)" size={18} />
        <input
          id={inputId}
          className={cx('als-search-input__input', inputClassName)}
          onChange={(event) => {
            onChange?.(event)
            onValueChange?.(event.currentTarget.value)
          }}
          placeholder={placeholder}
          style={mergeStyles(inputStyle, inputStyleOverride)}
          type="search"
          value={value}
          {...props}
        />

        {hasValue && onClear ? (
          <button
            aria-label={resolvedClearLabel}
            className="als-search-input__clear"
            onClick={onClear}
            style={{
              alignItems: 'center',
              background: 'transparent',
              border: 0,
              color: 'var(--als-color-text-muted, #64748b)',
              display: 'inline-flex',
              justifyContent: 'center',
              minHeight: 32,
              minWidth: 32,
              padding: 0,
            }}
            type="button"
          >
            <X aria-hidden="true" size={16} />
          </button>
        ) : null}
      </div>
    </div>
  )
}

export type FilterBarProps = {
  actions?: ReactNode
  activeCount?: number
  children: ReactNode
  className?: string
  clearLabel?: string
  label?: ReactNode
  onClear?: () => void
  style?: CSSProperties
  summary?: ReactNode
}

export function FilterBar({
  actions,
  activeCount,
  children,
  className,
  clearLabel,
  label,
  onClear,
  style,
  summary,
}: FilterBarProps) {
  const { t } = useTranslation()
  const resolvedClearLabel = clearLabel ?? t('components.filters.clear')
  const resolvedLabel = label ?? t('components.filters.label')

  return (
    <section
      aria-label={typeof resolvedLabel === 'string' ? resolvedLabel : undefined}
      className={cx('als-filter-bar', className)}
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
        className="als-filter-bar__content"
        style={{
          alignItems: 'center',
          display: 'flex',
          flex: '1 1 320px',
          flexWrap: 'wrap',
          gap: 10,
        }}
      >
        <span
          className="als-filter-bar__label"
          style={{ alignItems: 'center', display: 'inline-flex', fontWeight: 800, gap: 8 }}
        >
          <SlidersHorizontal aria-hidden="true" size={18} />
          {resolvedLabel}
          {typeof activeCount === 'number' && activeCount > 0 ? (
            <span
              aria-label={t('components.filters.activeCount', { count: activeCount })}
              className="als-filter-bar__count"
              style={{
                background: 'var(--als-color-primary-soft, #e8f2ff)',
                borderRadius: 999,
                color: 'var(--als-color-primary, #1877f2)',
                fontSize: '0.78rem',
                lineHeight: 1,
                padding: '5px 8px',
              }}
            >
              {activeCount}
            </span>
          ) : null}
        </span>

        {children}
      </div>

      <div
        className="als-filter-bar__actions"
        style={{ alignItems: 'center', display: 'flex', flexWrap: 'wrap', gap: 8 }}
      >
        {summary ? (
          <span
            className="als-filter-bar__summary"
            style={mergeStyles(subtleTextStyle, { fontSize: '0.9rem' })}
          >
            {summary}
          </span>
        ) : null}
        {onClear ? (
          <ActionButton onClick={onClear} size="sm" tone="ghost">
            {resolvedClearLabel}
          </ActionButton>
        ) : null}
        {actions}
      </div>
    </section>
  )
}

export type DataTableSortDirection = 'ascending' | 'descending' | 'none'

export type DataTableColumn<TData> = {
  align?: 'left' | 'center' | 'right'
  cell: (row: TData) => ReactNode
  className?: string
  header: ReactNode
  sortLabel?: string
  id: string
  onSort?: () => void
  sortDirection?: DataTableSortDirection
  width?: CSSProperties['width']
}

export type DataTableProps<TData> = {
  caption?: ReactNode
  className?: string
  columns: Array<DataTableColumn<TData>>
  emptyState?: ReactNode
  errorState?: ReactNode
  getRowKey: (row: TData, index: number) => string
  isLoading?: boolean
  loadingLabel?: ReactNode
  rowActions?: (row: TData) => ReactNode
  rows: TData[]
  style?: CSSProperties
}

const tableCellStyle: CSSProperties = {
  borderBottom: '1px solid var(--als-color-border, #d7deea)',
  padding: '14px 16px',
  verticalAlign: 'middle',
}

export function DataTable<TData>({
  caption,
  className,
  columns,
  emptyState,
  errorState,
  getRowKey,
  isLoading = false,
  loadingLabel,
  rowActions,
  rows,
  style,
}: DataTableProps<TData>) {
  const { t } = useTranslation()
  const colspan = columns.length + (rowActions ? 1 : 0)
  const hasRows = rows.length > 0
  const resolvedEmptyState = emptyState ?? t('components.table.empty')
  const resolvedLoadingLabel = loadingLabel ?? t('components.table.loading')
  const getColumnText = (header: ReactNode) => (typeof header === 'string' ? header : undefined)

  return (
    <div
      className={cx('als-data-table', className)}
      style={mergeStyles(
        surfaceStyle,
        {
          overflowX: 'auto',
          width: '100%',
        },
        style,
      )}
    >
      <table
        className="als-data-table__table"
        style={{
          borderCollapse: 'collapse',
          minWidth: 720,
          tableLayout: 'fixed',
          width: '100%',
        }}
      >
        {caption ? <caption className="als-data-table__caption">{caption}</caption> : null}
        <thead className="als-data-table__head">
          <tr className="als-data-table__header-row">
            {columns.map((column) => (
              <th
                key={column.id}
                aria-sort={column.sortDirection}
                className={cx('als-data-table__header-cell', column.className)}
                scope="col"
                style={mergeStyles(tableCellStyle, {
                  background: 'var(--als-color-surface-muted, #f8fafc)',
                  color: 'var(--als-color-text-muted, #64748b)',
                  fontSize: '0.78rem',
                  fontWeight: 800,
                  textAlign: column.align ?? 'left',
                  textTransform: 'uppercase',
                  width: column.width,
                })}
              >
                {column.onSort ? (
                  <button
                    aria-label={
                      column.sortLabel ??
                      t('components.table.sortBy', {
                        column: getColumnText(column.header) ?? column.id,
                      })
                    }
                    className="als-data-table__sort"
                    onClick={column.onSort}
                    style={{
                      alignItems: 'center',
                      background: 'transparent',
                      border: 0,
                      color: 'inherit',
                      display: 'inline-flex',
                      font: 'inherit',
                      justifyContent:
                        column.align === 'right'
                          ? 'flex-end'
                          : column.align === 'center'
                            ? 'center'
                            : 'flex-start',
                      minHeight: 32,
                      padding: 0,
                      width: '100%',
                    }}
                    type="button"
                  >
                    {column.header}
                  </button>
                ) : (
                  column.header
                )}
              </th>
            ))}

            {rowActions ? (
              <th
                className="als-data-table__header-cell als-data-table__header-cell--actions"
                scope="col"
                style={mergeStyles(tableCellStyle, {
                  background: 'var(--als-color-surface-muted, #f8fafc)',
                  color: 'var(--als-color-text-muted, #64748b)',
                  fontSize: '0.78rem',
                  fontWeight: 800,
                  textAlign: 'right',
                  textTransform: 'uppercase',
                  width: 92,
                })}
              >
                {t('components.table.actions')}
              </th>
            ) : null}
          </tr>
        </thead>

        <tbody className="als-data-table__body">
          {isLoading || errorState || !hasRows ? (
            <tr className="als-data-table__state-row">
              <td
                className="als-data-table__state-cell"
                colSpan={colspan}
                style={mergeStyles(tableCellStyle, {
                  padding: 24,
                  textAlign: 'center',
                })}
              >
                {isLoading ? (
                  <span className="als-data-table__loading" role="status">
                    {resolvedLoadingLabel}
                  </span>
                ) : (
                  (errorState ?? resolvedEmptyState)
                )}
              </td>
            </tr>
          ) : (
            rows.map((row, rowIndex) => (
              <tr className="als-data-table__row" key={getRowKey(row, rowIndex)}>
                {columns.map((column) => (
                  <td
                    key={column.id}
                    className={cx('als-data-table__cell', column.className)}
                    style={mergeStyles(tableCellStyle, {
                      color: 'var(--als-color-text, #1f2937)',
                      textAlign: column.align ?? 'left',
                    })}
                  >
                    {column.cell(row)}
                  </td>
                ))}

                {rowActions ? (
                  <td
                    className="als-data-table__cell als-data-table__cell--actions"
                    style={mergeStyles(tableCellStyle, { textAlign: 'right' })}
                  >
                    {rowActions(row)}
                  </td>
                ) : null}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}

export type PaginationLabels = {
  next: string
  page: (page: number, totalPages: number) => string
  previous: string
  range: (first: number, last: number, total: number) => string
}

export type PaginationProps = {
  className?: string
  labels?: PaginationLabels
  onPageChange: (page: number) => void
  page: number
  pageSize: number
  style?: CSSProperties
  totalItems: number
}

export function Pagination({
  className,
  labels,
  onPageChange,
  page,
  pageSize,
  style,
  totalItems,
}: PaginationProps) {
  const { t } = useTranslation()
  const normalizedPageSize = Math.max(1, pageSize)
  const pageCount = Math.max(1, Math.ceil(totalItems / normalizedPageSize))
  const currentPage = Math.min(Math.max(1, page), pageCount)
  const firstItem = totalItems === 0 ? 0 : (currentPage - 1) * normalizedPageSize + 1
  const lastItem = Math.min(totalItems, currentPage * normalizedPageSize)
  const resolvedLabels: PaginationLabels = labels ?? {
    next: t('components.pagination.next'),
    page: (pageNumber, totalPages) =>
      t('components.pagination.page', { page: pageNumber, totalPages }),
    previous: t('components.pagination.previous'),
    range: (first, last, total) => t('components.pagination.range', { first, last, total }),
  }

  return (
    <nav
      aria-label={t('components.pagination.label')}
      className={cx('als-pagination', className)}
      style={mergeStyles(
        {
          alignItems: 'center',
          display: 'flex',
          flexWrap: 'wrap',
          gap: 12,
          justifyContent: 'space-between',
        },
        style,
      )}
    >
      <p className="als-pagination__range" style={mergeStyles(subtleTextStyle, { margin: 0 })}>
        {resolvedLabels.range(firstItem, lastItem, totalItems)}
      </p>

      <div
        className="als-pagination__controls"
        style={{ alignItems: 'center', display: 'flex', flexWrap: 'wrap', gap: 8 }}
      >
        <ActionButton
          aria-label={resolvedLabels.previous}
          disabled={currentPage <= 1}
          icon={<ChevronLeft aria-hidden="true" size={16} />}
          onClick={() => onPageChange(currentPage - 1)}
          size="sm"
        >
          {resolvedLabels.previous}
        </ActionButton>

        <span className="als-pagination__page" style={{ fontSize: '0.92rem', fontWeight: 800 }}>
          {resolvedLabels.page(currentPage, pageCount)}
        </span>

        <ActionButton
          aria-label={resolvedLabels.next}
          disabled={currentPage >= pageCount}
          icon={<ChevronRight aria-hidden="true" size={16} />}
          onClick={() => onPageChange(currentPage + 1)}
          size="sm"
        >
          {resolvedLabels.next}
        </ActionButton>
      </div>
    </nav>
  )
}

export type StatusBadgeTone = 'neutral' | 'success' | 'warning' | 'danger' | 'info' | 'archived'

export type StatusBadgeProps = {
  className?: string
  icon?: ReactNode
  label: ReactNode
  style?: CSSProperties
  tone?: StatusBadgeTone
}

const badgeToneStyles: Record<StatusBadgeTone, CSSProperties> = {
  archived: {
    background: 'var(--als-color-archived-soft, #eef2f7)',
    color: 'var(--als-color-archived, #475467)',
  },
  danger: {
    background: 'var(--als-color-danger-soft, #fef3f2)',
    color: 'var(--als-color-danger, #b42318)',
  },
  info: {
    background: 'var(--als-color-info-soft, #e8f2ff)',
    color: 'var(--als-color-info, #175cd3)',
  },
  neutral: {
    background: 'var(--als-color-neutral-soft, #f8fafc)',
    color: 'var(--als-color-neutral, #344054)',
  },
  success: {
    background: 'var(--als-color-success-soft, #ecfdf3)',
    color: 'var(--als-color-success, #027a48)',
  },
  warning: {
    background: 'var(--als-color-warning-soft, #fffaeb)',
    color: 'var(--als-color-warning, #b54708)',
  },
}

export function StatusBadge({ className, icon, label, style, tone = 'neutral' }: StatusBadgeProps) {
  return (
    <span
      className={cx('als-status-badge', `als-status-badge--${tone}`, className)}
      style={mergeStyles(
        {
          alignItems: 'center',
          borderRadius: 999,
          display: 'inline-flex',
          fontSize: '0.78rem',
          fontWeight: 800,
          gap: 6,
          lineHeight: 1,
          minHeight: 26,
          padding: '6px 10px',
        },
        badgeToneStyles[tone],
        style,
      )}
    >
      {icon}
      {label}
    </span>
  )
}

export type RowAction = {
  disabled?: boolean
  icon?: ReactNode
  id: string
  isDestructive?: boolean
  label: ReactNode
  onSelect: () => void
}

export type RowActionsLabels = {
  menu: string
}

export type RowActionsProps = {
  actions: RowAction[]
  className?: string
  labels?: RowActionsLabels
  menuIcon?: LucideIcon
}

export function RowActions({
  actions,
  className,
  labels,
  menuIcon: MenuIcon = MoreHorizontal,
}: RowActionsProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)
  const menuId = useComponentId('als-row-actions')
  const resolvedLabels = labels ?? { menu: t('components.rowActions.menu') }

  function focusMenuItem(index: number) {
    const items = menuRef.current?.querySelectorAll<HTMLButtonElement>('[role="menuitem"]')
    items?.item(index)?.focus()
  }

  useEffect(() => {
    if (!isOpen) {
      return undefined
    }

    function handlePointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    document.addEventListener('pointerdown', handlePointerDown)

    return () => document.removeEventListener('pointerdown', handlePointerDown)
  }, [isOpen])

  useEffect(() => {
    if (isOpen) {
      window.setTimeout(() => focusMenuItem(0), 0)
    }
  }, [isOpen])

  return (
    <div
      ref={rootRef}
      className={cx('als-row-actions', className)}
      onKeyDown={(event) => {
        if (event.key === 'Escape') {
          event.preventDefault()
          setIsOpen(false)
          triggerRef.current?.focus()
          return
        }

        if (!isOpen && (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ')) {
          event.preventDefault()
          setIsOpen(true)
          return
        }

        if (!isOpen || (event.key !== 'ArrowDown' && event.key !== 'ArrowUp')) {
          return
        }

        event.preventDefault()
        const items = Array.from(
          menuRef.current?.querySelectorAll<HTMLButtonElement>('[role="menuitem"]') ?? [],
        )
        const currentIndex = items.findIndex((item) => item === document.activeElement)
        const lastIndex = items.length - 1

        if (event.key === 'ArrowDown') {
          items[currentIndex >= lastIndex ? 0 : currentIndex + 1]?.focus()
        }

        if (event.key === 'ArrowUp') {
          items[currentIndex <= 0 ? lastIndex : currentIndex - 1]?.focus()
        }
      }}
      style={{ display: 'inline-flex', position: 'relative' }}
    >
      <button
        ref={triggerRef}
        aria-controls={isOpen ? menuId : undefined}
        aria-expanded={isOpen}
        aria-haspopup="menu"
        aria-label={resolvedLabels.menu}
        className="als-row-actions__trigger"
        onClick={() => setIsOpen((current) => !current)}
        style={{
          alignItems: 'center',
          background: 'var(--als-color-surface, #ffffff)',
          border: '1px solid var(--als-color-border, #d7deea)',
          borderRadius: 'var(--als-radius-md, 8px)',
          color: 'var(--als-color-text, #1f2937)',
          display: 'inline-flex',
          justifyContent: 'center',
          minHeight: 36,
          minWidth: 36,
          padding: 0,
        }}
        type="button"
      >
        <MenuIcon aria-hidden="true" size={18} />
      </button>

      {isOpen ? (
        <div
          ref={menuRef}
          id={menuId}
          className="als-row-actions__menu"
          role="menu"
          style={mergeStyles(surfaceStyle, {
            boxShadow: '0 18px 42px rgba(15, 23, 42, 0.16)',
            display: 'grid',
            gap: 4,
            minWidth: 176,
            padding: 6,
            position: 'absolute',
            right: 0,
            top: 'calc(100% + 6px)',
            zIndex: 20,
          })}
        >
          {actions.map((action) => (
            <button
              key={action.id}
              className={cx(
                'als-row-actions__item',
                action.isDestructive && 'als-row-actions__item--destructive',
              )}
              disabled={action.disabled}
              onClick={() => {
                action.onSelect()
                setIsOpen(false)
              }}
              onKeyDown={(event) => {
                if (event.key === 'Home') {
                  event.preventDefault()
                  focusMenuItem(0)
                }

                if (event.key === 'End') {
                  event.preventDefault()
                  const items =
                    menuRef.current?.querySelectorAll<HTMLButtonElement>('[role="menuitem"]')
                  if (items && items.length > 0) {
                    items.item(items.length - 1).focus()
                  }
                }
              }}
              role="menuitem"
              style={{
                alignItems: 'center',
                background: 'transparent',
                border: 0,
                borderRadius: 'var(--als-radius-sm, 6px)',
                color: action.isDestructive
                  ? 'var(--als-color-danger, #b42318)'
                  : 'var(--als-color-text, #1f2937)',
                display: 'flex',
                font: 'inherit',
                gap: 8,
                justifyContent: 'flex-start',
                minHeight: 36,
                padding: '0 10px',
                textAlign: 'left',
                width: '100%',
              }}
              type="button"
            >
              {action.icon}
              {action.label}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  )
}
