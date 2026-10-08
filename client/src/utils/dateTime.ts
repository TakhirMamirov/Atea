import type { DateRange } from '../api/types'

export const MAX_RANGE_DAYS = 31
const HOUR_MS = 3_600_000

const dateTimeFormat = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
})

const shortFormat = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
})

/** Formats an ISO string or date in the viewer's local time zone, e.g. "07 Oct 2026, 14:05:00". */
export function formatDateTime(value: string | Date | number): string {
  return dateTimeFormat.format(new Date(value))
}

/** Compact label for chart axes, e.g. "07 Oct, 14:05". */
export function formatShortDateTime(value: string | Date | number): string {
  return shortFormat.format(new Date(value))
}

export function formatTemperature(value: number): string {
  return `${value.toFixed(1)} °C`
}

const pad = (n: number) => String(n).padStart(2, '0')

/** Converts a date to the value format of `<input type="datetime-local">` in local time. */
export function toDateTimeLocalValue(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/** Parses a `datetime-local` value (interpreted as local time). Returns null when invalid. */
export function parseDateTimeLocalValue(value: string): Date | null {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

export function validateRange(range: DateRange): string | null {
  if (range.from >= range.to) return 'Start must be before end.'
  if (range.to.getTime() - range.from.getTime() > MAX_RANGE_DAYS * 24 * HOUR_MS) {
    return `Range must not exceed ${MAX_RANGE_DAYS} days.`
  }
  return null
}

export interface RangePreset {
  id: string
  label: string
  hours: number
}

export const RANGE_PRESETS: readonly RangePreset[] = [
  { id: '1h', label: 'Last hour', hours: 1 },
  { id: '6h', label: '6 hours', hours: 6 },
  { id: '24h', label: '24 hours', hours: 24 },
  { id: '7d', label: '7 days', hours: 24 * 7 },
  { id: '30d', label: '30 days', hours: 24 * 30 },
]

export const DEFAULT_PRESET_ID = '24h'

/** A range is either relative to "now" (preset, re-evaluated on refresh) or fixed. */
export type RangeSelection = { kind: 'preset'; presetId: string } | { kind: 'custom'; from: Date; to: Date }

export function resolveRange(selection: RangeSelection, now: Date = new Date()): DateRange {
  if (selection.kind === 'custom') return { from: selection.from, to: selection.to }
  const preset = RANGE_PRESETS.find((p) => p.id === selection.presetId) ?? RANGE_PRESETS[2]
  return { from: new Date(now.getTime() - preset.hours * HOUR_MS), to: now }
}

/** Reads the selection from URL search params (`range=24h` or `from=...&to=...`). */
export function selectionFromSearchParams(params: URLSearchParams): RangeSelection {
  const from = params.get('from')
  const to = params.get('to')
  if (from && to) {
    const range = { from: new Date(from), to: new Date(to) }
    if (!Number.isNaN(range.from.getTime()) && !Number.isNaN(range.to.getTime()) && !validateRange(range)) {
      return { kind: 'custom', ...range }
    }
  }
  const presetId = params.get('range')
  const known = RANGE_PRESETS.some((p) => p.id === presetId)
  return { kind: 'preset', presetId: known && presetId ? presetId : DEFAULT_PRESET_ID }
}

export function selectionToSearchParams(selection: RangeSelection): URLSearchParams {
  return selection.kind === 'preset'
    ? new URLSearchParams({ range: selection.presetId })
    : new URLSearchParams({ from: selection.from.toISOString(), to: selection.to.toISOString() })
}
