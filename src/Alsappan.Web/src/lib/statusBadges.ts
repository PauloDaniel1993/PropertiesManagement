import type { StatusBadgeTone } from '../components'

export const statusBadgeTones = [
  'archived',
  'danger',
  'info',
  'neutral',
  'success',
  'warning',
] as const satisfies readonly StatusBadgeTone[]

export function coerceStatusBadgeTone(
  tone: string | undefined,
  fallback: StatusBadgeTone = 'neutral',
): StatusBadgeTone {
  return statusBadgeTones.includes(tone as StatusBadgeTone) ? (tone as StatusBadgeTone) : fallback
}
