import { useId, type CSSProperties } from 'react'

export function cx(...classes: Array<string | false | null | undefined>) {
  return classes.filter(Boolean).join(' ')
}

export function mergeStyles(...styles: Array<CSSProperties | undefined>) {
  return Object.assign({}, ...styles.filter(Boolean)) as CSSProperties
}

export function composeIds(...ids: Array<string | false | null | undefined>) {
  const composed = ids.filter(Boolean).join(' ')

  return composed.length > 0 ? composed : undefined
}

export function useComponentId(prefix: string, providedId?: string) {
  const generatedId = useId().replace(/:/g, '')

  return providedId ?? `${prefix}-${generatedId}`
}

export const visuallyHiddenStyle: CSSProperties = {
  border: 0,
  clip: 'rect(0 0 0 0)',
  height: 1,
  margin: -1,
  overflow: 'hidden',
  padding: 0,
  position: 'absolute',
  whiteSpace: 'nowrap',
  width: 1,
}

export const surfaceStyle: CSSProperties = {
  background: 'var(--als-color-surface, #ffffff)',
  border: '1px solid var(--als-color-border, #d7deea)',
  borderRadius: 'var(--als-radius-md, 8px)',
  color: 'var(--als-color-text, #1f2937)',
}

export const subtleTextStyle: CSSProperties = {
  color: 'var(--als-color-text-muted, #64748b)',
}

export const focusRingStyle: CSSProperties = {
  outlineColor: 'var(--als-color-focus, #93c5fd)',
  outlineOffset: 2,
}
