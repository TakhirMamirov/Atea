import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { RangeSelection } from '../utils/dateTime'
import { DateRangeFilter } from './DateRangeFilter'

describe('DateRangeFilter', () => {
  it('marks the active preset and emits a preset selection on click', async () => {
    const onChange = vi.fn<(selection: RangeSelection) => void>()
    render(<DateRangeFilter value={{ kind: 'preset', presetId: '24h' }} onChange={onChange} />)

    expect(screen.getByRole('button', { name: '24 hours' })).toHaveAttribute('aria-pressed', 'true')

    await userEvent.click(screen.getByRole('button', { name: '7 days' }))

    expect(onChange).toHaveBeenCalledWith({ kind: 'preset', presetId: '7d' })
  })

  it('emits a custom range when applied', async () => {
    const onChange = vi.fn<(selection: RangeSelection) => void>()
    render(<DateRangeFilter value={{ kind: 'preset', presetId: '24h' }} onChange={onChange} />)

    fireEvent.change(screen.getByLabelText('From'), { target: { value: '2026-10-01T08:00' } })
    fireEvent.change(screen.getByLabelText('To'), { target: { value: '2026-10-02T08:00' } })
    await userEvent.click(screen.getByRole('button', { name: 'Apply' }))

    expect(onChange).toHaveBeenCalledWith({
      kind: 'custom',
      from: new Date(2026, 9, 1, 8, 0),
      to: new Date(2026, 9, 2, 8, 0),
    })
  })

  it('shows an error and does not emit when start is after end', async () => {
    const onChange = vi.fn<(selection: RangeSelection) => void>()
    render(<DateRangeFilter value={{ kind: 'preset', presetId: '24h' }} onChange={onChange} />)

    fireEvent.change(screen.getByLabelText('From'), { target: { value: '2026-10-03T08:00' } })
    fireEvent.change(screen.getByLabelText('To'), { target: { value: '2026-10-02T08:00' } })
    await userEvent.click(screen.getByRole('button', { name: 'Apply' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Start must be before end.')
    expect(onChange).not.toHaveBeenCalled()
  })
})
