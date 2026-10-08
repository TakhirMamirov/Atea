import { useState, type FormEvent } from 'react'
import {
  parseDateTimeLocalValue,
  RANGE_PRESETS,
  resolveRange,
  toDateTimeLocalValue,
  validateRange,
  type RangeSelection,
} from '../utils/dateTime'

interface DateRangeFilterProps {
  value: RangeSelection
  onChange: (selection: RangeSelection) => void
}

/**
 * Preset buttons plus a custom from/to picker (local time).
 * Remount with a new `key` when `value` changes externally to reset the inputs.
 */
export function DateRangeFilter({ value, onChange }: DateRangeFilterProps) {
  const initial = resolveRange(value)
  const [fromText, setFromText] = useState(toDateTimeLocalValue(initial.from))
  const [toText, setToText] = useState(toDateTimeLocalValue(initial.to))
  const [error, setError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const from = parseDateTimeLocalValue(fromText)
    const to = parseDateTimeLocalValue(toText)
    if (!from || !to) {
      setError('Enter both start and end.')
      return
    }
    const validationError = validateRange({ from, to })
    setError(validationError)
    if (!validationError) onChange({ kind: 'custom', from, to })
  }

  return (
    <form className="filter-bar" onSubmit={handleSubmit} aria-label="Date and time range">
      <fieldset className="segmented">
        <legend className="visually-hidden">Quick ranges</legend>
        {RANGE_PRESETS.map((preset) => {
          const active = value.kind === 'preset' && value.presetId === preset.id
          return (
            <button
              key={preset.id}
              type="button"
              className={active ? 'segment active' : 'segment'}
              aria-pressed={active}
              onClick={() => onChange({ kind: 'preset', presetId: preset.id })}
            >
              {preset.label}
            </button>
          )
        })}
      </fieldset>

      <div className="field-group">
        <label className="field">
          <span>From</span>
          <input type="datetime-local" value={fromText} onChange={(e) => setFromText(e.target.value)} required />
        </label>
        <label className="field">
          <span>To</span>
          <input type="datetime-local" value={toText} onChange={(e) => setToText(e.target.value)} required />
        </label>
        <button type="submit" className="button">
          Apply
        </button>
      </div>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
    </form>
  )
}
