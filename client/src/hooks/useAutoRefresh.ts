import { useEffect, useRef } from 'react'

/**
 * Calls `refresh` every `intervalMs` while the page is visible. Refreshes are skipped while the tab is hidden;
 * if any were skipped, one refresh runs as soon as the tab becomes visible again. Pass `null` to disable.
 */
export function useAutoRefresh(refresh: () => void, intervalMs: number | null): void {
  const saved = useRef(refresh)

  useEffect(() => {
    saved.current = refresh
  }, [refresh])

  useEffect(() => {
    if (intervalMs === null) return
    let missed = false

    const id = setInterval(() => {
      if (document.hidden) missed = true
      else saved.current()
    }, intervalMs)

    const onVisibilityChange = () => {
      if (document.hidden || !missed) return
      missed = false
      saved.current()
    }
    document.addEventListener('visibilitychange', onVisibilityChange)

    return () => {
      clearInterval(id)
      document.removeEventListener('visibilitychange', onVisibilityChange)
    }
  }, [intervalMs])
}
