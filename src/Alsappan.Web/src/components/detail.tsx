import { useState, type CSSProperties, type KeyboardEvent, type ReactNode } from 'react'
import { Link2 } from 'lucide-react'
import { cx, mergeStyles, subtleTextStyle, surfaceStyle, useComponentId } from './utils'

export type DetailSectionProps = {
  actions?: ReactNode
  children: ReactNode
  className?: string
  description?: ReactNode
  style?: CSSProperties
  title: ReactNode
}

export function DetailSection({
  actions,
  children,
  className,
  description,
  style,
  title,
}: DetailSectionProps) {
  return (
    <section
      className={cx('als-detail-section', className)}
      style={mergeStyles(surfaceStyle, { display: 'grid', gap: 18, padding: 18 }, style)}
    >
      <div
        className="als-detail-section__header"
        style={{
          alignItems: 'flex-start',
          display: 'flex',
          gap: 12,
          justifyContent: 'space-between',
        }}
      >
        <div className="als-detail-section__copy" style={{ display: 'grid', gap: 5 }}>
          <h2
            className="als-detail-section__title"
            style={{
              color: 'var(--als-color-text, #1f2937)',
              fontSize: '1.05rem',
              lineHeight: 1.3,
              margin: 0,
            }}
          >
            {title}
          </h2>

          {description ? (
            <p
              className="als-detail-section__description"
              style={mergeStyles(subtleTextStyle, {
                fontSize: '0.9rem',
                lineHeight: 1.45,
                margin: 0,
              })}
            >
              {description}
            </p>
          ) : null}
        </div>

        {actions ? (
          <div
            className="als-detail-section__actions"
            style={{ alignItems: 'center', display: 'flex', flexWrap: 'wrap', gap: 8 }}
          >
            {actions}
          </div>
        ) : null}
      </div>

      <div className="als-detail-section__body">{children}</div>
    </section>
  )
}

export type DetailListItem = {
  description?: ReactNode
  label: ReactNode
  value: ReactNode
}

export type DetailListProps = {
  className?: string
  columns?: 1 | 2 | 3
  items: DetailListItem[]
  style?: CSSProperties
}

export function DetailList({ className, columns = 2, items, style }: DetailListProps) {
  return (
    <dl
      className={cx('als-detail-list', className)}
      style={mergeStyles(
        {
          display: 'grid',
          gap: 14,
          gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))`,
          margin: 0,
        },
        style,
      )}
    >
      {items.map((item, index) => (
        <div
          className="als-detail-list__item"
          key={index}
          style={{ display: 'grid', gap: 4, minWidth: 0 }}
        >
          <dt
            className="als-detail-list__label"
            style={mergeStyles(subtleTextStyle, {
              fontSize: '0.78rem',
              fontWeight: 800,
              textTransform: 'uppercase',
            })}
          >
            {item.label}
          </dt>
          <dd className="als-detail-list__value" style={{ margin: 0, minWidth: 0 }}>
            {item.value}
          </dd>
          {item.description ? (
            <dd
              className="als-detail-list__description"
              style={mergeStyles(subtleTextStyle, {
                fontSize: '0.86rem',
                lineHeight: 1.45,
                margin: 0,
              })}
            >
              {item.description}
            </dd>
          ) : null}
        </div>
      ))}
    </dl>
  )
}

export type TabItem = {
  content: ReactNode
  disabled?: boolean
  icon?: ReactNode
  id: string
  label: ReactNode
}

export type TabsProps = {
  ariaLabel?: string
  className?: string
  defaultSelectedId?: string
  onSelectedIdChange?: (id: string) => void
  selectedId?: string
  style?: CSSProperties
  tabs: TabItem[]
}

export function Tabs({
  ariaLabel = 'Seções',
  className,
  defaultSelectedId,
  onSelectedIdChange,
  selectedId,
  style,
  tabs,
}: TabsProps) {
  const tabsId = useComponentId('als-tabs')
  const firstEnabledTab = tabs.find((tab) => !tab.disabled)
  const [internalSelectedId, setInternalSelectedId] = useState(
    defaultSelectedId ?? firstEnabledTab?.id,
  )
  const activeId = selectedId ?? internalSelectedId
  const activeTab = tabs.find((tab) => tab.id === activeId && !tab.disabled) ?? firstEnabledTab

  function selectTab(id: string) {
    setInternalSelectedId(id)
    onSelectedIdChange?.(id)
  }

  function handleTabKeyDown(event: KeyboardEvent<HTMLButtonElement>, tabIndex: number) {
    const enabledTabs = tabs.filter((tab) => !tab.disabled)

    if (enabledTabs.length === 0) {
      return
    }

    const currentEnabledIndex = enabledTabs.findIndex((tab) => tab.id === tabs[tabIndex]?.id)
    const lastEnabledIndex = enabledTabs.length - 1
    let nextTab: TabItem | undefined

    if (event.key === 'ArrowRight') {
      nextTab = enabledTabs[currentEnabledIndex === lastEnabledIndex ? 0 : currentEnabledIndex + 1]
    }

    if (event.key === 'ArrowLeft') {
      nextTab = enabledTabs[currentEnabledIndex <= 0 ? lastEnabledIndex : currentEnabledIndex - 1]
    }

    if (event.key === 'Home') {
      nextTab = enabledTabs[0]
    }

    if (event.key === 'End') {
      nextTab = enabledTabs[lastEnabledIndex]
    }

    if (nextTab) {
      event.preventDefault()
      selectTab(nextTab.id)
      document.getElementById(`${tabsId}-${nextTab.id}-tab`)?.focus()
    }
  }

  if (!activeTab) {
    return null
  }

  return (
    <div className={cx('als-tabs', className)} style={style}>
      <div
        aria-label={ariaLabel}
        className="als-tabs__list"
        role="tablist"
        style={{
          alignItems: 'center',
          borderBottom: '1px solid var(--als-color-border, #d7deea)',
          display: 'flex',
          gap: 4,
          overflowX: 'auto',
        }}
      >
        {tabs.map((tab, tabIndex) => {
          const isSelected = tab.id === activeTab.id

          return (
            <button
              key={tab.id}
              id={`${tabsId}-${tab.id}-tab`}
              aria-controls={`${tabsId}-${tab.id}-panel`}
              aria-selected={isSelected}
              className={cx('als-tabs__tab', isSelected && 'als-tabs__tab--selected')}
              disabled={tab.disabled}
              onClick={() => selectTab(tab.id)}
              onKeyDown={(event) => handleTabKeyDown(event, tabIndex)}
              role="tab"
              style={{
                alignItems: 'center',
                background: 'transparent',
                border: 0,
                borderBottom: isSelected
                  ? '3px solid var(--als-color-primary, #1877f2)'
                  : '3px solid transparent',
                color: isSelected
                  ? 'var(--als-color-text, #1f2937)'
                  : 'var(--als-color-text-muted, #64748b)',
                display: 'inline-flex',
                font: 'inherit',
                fontWeight: 800,
                gap: 8,
                minHeight: 44,
                padding: '0 14px',
                whiteSpace: 'nowrap',
              }}
              tabIndex={isSelected ? 0 : -1}
              type="button"
            >
              {tab.icon}
              {tab.label}
            </button>
          )
        })}
      </div>

      <div
        id={`${tabsId}-${activeTab.id}-panel`}
        aria-labelledby={`${tabsId}-${activeTab.id}-tab`}
        className={cx('als-tabs__panel', 'als-tabs__panel--selected')}
        role="tabpanel"
        style={{ paddingBlock: 16 }}
        tabIndex={0}
      >
        {activeTab.content}
      </div>
    </div>
  )
}

export type RelationshipPanelItem = {
  action?: ReactNode
  description?: ReactNode
  href?: string
  id: string
  meta?: ReactNode
  status?: ReactNode
  title: ReactNode
}

export type RelationshipPanelProps = {
  action?: ReactNode
  className?: string
  emptyState?: ReactNode
  items: RelationshipPanelItem[]
  style?: CSSProperties
  title: ReactNode
}

export function RelationshipPanel({
  action,
  className,
  emptyState = 'Nenhum vínculo cadastrado.',
  items,
  style,
  title,
}: RelationshipPanelProps) {
  return (
    <section
      className={cx('als-relationship-panel', className)}
      style={mergeStyles(surfaceStyle, { display: 'grid', gap: 12, padding: 14 }, style)}
    >
      <header
        className="als-relationship-panel__header"
        style={{ alignItems: 'center', display: 'flex', gap: 10, justifyContent: 'space-between' }}
      >
        <h3
          className="als-relationship-panel__title"
          style={{
            alignItems: 'center',
            color: 'var(--als-color-text, #1f2937)',
            display: 'inline-flex',
            fontSize: '0.98rem',
            gap: 8,
            lineHeight: 1.35,
            margin: 0,
          }}
        >
          <Link2 aria-hidden="true" size={17} />
          {title}
        </h3>
        {action}
      </header>

      {items.length === 0 ? (
        <div
          className="als-relationship-panel__empty"
          style={mergeStyles(subtleTextStyle, {
            border: '1px dashed var(--als-color-border, #d7deea)',
            borderRadius: 'var(--als-radius-md, 8px)',
            padding: 14,
          })}
        >
          {emptyState}
        </div>
      ) : (
        <ul
          className="als-relationship-panel__list"
          style={{ display: 'grid', gap: 8, listStyle: 'none', margin: 0, padding: 0 }}
        >
          {items.map((item) => (
            <li
              key={item.id}
              className="als-relationship-panel__item"
              style={mergeStyles(surfaceStyle, {
                alignItems: 'center',
                display: 'flex',
                gap: 12,
                justifyContent: 'space-between',
                padding: 12,
              })}
            >
              <div
                className="als-relationship-panel__item-copy"
                style={{ display: 'grid', gap: 4, minWidth: 0 }}
              >
                {item.href ? (
                  <a
                    className="als-relationship-panel__link"
                    href={item.href}
                    style={{ color: 'var(--als-color-primary, #1877f2)', fontWeight: 800 }}
                  >
                    {item.title}
                  </a>
                ) : (
                  <strong className="als-relationship-panel__item-title">{item.title}</strong>
                )}

                {item.description ? (
                  <span
                    className="als-relationship-panel__description"
                    style={mergeStyles(subtleTextStyle, { fontSize: '0.88rem', lineHeight: 1.45 })}
                  >
                    {item.description}
                  </span>
                ) : null}

                {item.meta ? (
                  <span
                    className="als-relationship-panel__meta"
                    style={mergeStyles(subtleTextStyle, { fontSize: '0.8rem', lineHeight: 1.35 })}
                  >
                    {item.meta}
                  </span>
                ) : null}
              </div>

              <div
                className="als-relationship-panel__item-actions"
                style={{ alignItems: 'center', display: 'flex', flexShrink: 0, gap: 8 }}
              >
                {item.status}
                {item.action}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
