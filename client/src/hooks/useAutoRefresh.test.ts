import { renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useAutoRefresh } from './useAutoRefresh'

let hidden = false

function setHidden(value: boolean) {
  hidden = value
  document.dispatchEvent(new Event('visibilitychange'))
}

describe('useAutoRefresh', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    hidden = false
    vi.spyOn(document, 'hidden', 'get').mockImplementation(() => hidden)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('refreshes on every interval while the page is visible', () => {
    const refresh = vi.fn<() => void>()
    renderHook(() => useAutoRefresh(refresh, 1000))

    vi.advanceTimersByTime(3000)

    expect(refresh).toHaveBeenCalledTimes(3)
  })

  it('skips refreshes while hidden and catches up once when visible again', () => {
    const refresh = vi.fn<() => void>()
    renderHook(() => useAutoRefresh(refresh, 1000))

    setHidden(true)
    vi.advanceTimersByTime(5000)
    expect(refresh).not.toHaveBeenCalled()

    setHidden(false)
    expect(refresh).toHaveBeenCalledTimes(1)
  })

  it('does not refresh on becoming visible when nothing was skipped', () => {
    const refresh = vi.fn<() => void>()
    renderHook(() => useAutoRefresh(refresh, 1000))

    setHidden(true)
    setHidden(false)

    expect(refresh).not.toHaveBeenCalled()
  })

  it('does nothing when disabled', () => {
    const refresh = vi.fn<() => void>()
    renderHook(() => useAutoRefresh(refresh, null))

    vi.advanceTimersByTime(5000)

    expect(refresh).not.toHaveBeenCalled()
  })
})
