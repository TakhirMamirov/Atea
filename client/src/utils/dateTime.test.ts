import { describe, expect, it } from 'vitest'
import {
  formatTemperature,
  parseDateTimeLocalValue,
  resolveRange,
  selectionFromSearchParams,
  selectionToSearchParams,
  toDateTimeLocalValue,
  validateRange,
} from './dateTime'

describe('datetime-local conversion', () => {
  it('round-trips a local date to minute precision', () => {
    const date = new Date(2026, 9, 7, 9, 5)
    const text = toDateTimeLocalValue(date)
    expect(text).toBe('2026-10-07T09:05')
    expect(parseDateTimeLocalValue(text)).toEqual(date)
  })

  it('returns null for invalid input', () => {
    expect(parseDateTimeLocalValue('')).toBeNull()
    expect(parseDateTimeLocalValue('not a date')).toBeNull()
  })
})

describe('validateRange', () => {
  const base = new Date('2026-10-07T12:00:00Z')

  it('accepts a valid range', () => {
    expect(validateRange({ from: new Date(base.getTime() - 3_600_000), to: base })).toBeNull()
  })

  it('rejects start after end', () => {
    expect(validateRange({ from: base, to: new Date(base.getTime() - 1) })).toMatch(/before/)
  })

  it('rejects ranges longer than 31 days', () => {
    expect(validateRange({ from: new Date(base.getTime() - 32 * 86_400_000), to: base })).toMatch(/31 days/)
  })
})

describe('range selection', () => {
  const now = new Date('2026-10-07T12:00:00Z')

  it('defaults to the 24 hour preset', () => {
    const selection = selectionFromSearchParams(new URLSearchParams())
    expect(selection).toEqual({ kind: 'preset', presetId: '24h' })
    expect(resolveRange(selection, now)).toEqual({ from: new Date('2026-10-06T12:00:00Z'), to: now })
  })

  it('round-trips a custom range through search params', () => {
    const selection = { kind: 'custom' as const, from: new Date('2026-10-01T00:00:00Z'), to: new Date('2026-10-02T00:00:00Z') }
    expect(selectionFromSearchParams(selectionToSearchParams(selection))).toEqual(selection)
  })

  it('ignores an invalid custom range', () => {
    const params = new URLSearchParams({ from: '2026-10-02T00:00:00Z', to: '2026-10-01T00:00:00Z' })
    expect(selectionFromSearchParams(params).kind).toBe('preset')
  })
})

it('formats temperatures with one decimal', () => {
  expect(formatTemperature(12)).toBe('12.0 °C')
  expect(formatTemperature(-3.456)).toBe('-3.5 °C')
})
