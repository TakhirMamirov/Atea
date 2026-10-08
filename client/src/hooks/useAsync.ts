import { useCallback, useEffect, useState } from 'react'

export interface AsyncState<T> {
  data: T | undefined
  error: Error | undefined
  isLoading: boolean
  reload: () => void
}

interface Settled<T, L> {
  load: L
  version: number
  data: T | undefined
  error: Error | undefined
}

/**
 * Runs `load` whenever its identity changes (memoize it with useCallback) or `reload` is called.
 * Previous data is kept while reloading to avoid flicker; stale requests are aborted.
 */
export function useAsync<T>(load: (signal: AbortSignal) => Promise<T>): AsyncState<T> {
  const [version, setVersion] = useState(0)
  const [settled, setSettled] = useState<Settled<T, typeof load>>()

  useEffect(() => {
    const controller = new AbortController()

    load(controller.signal).then(
      (data) => {
        if (!controller.signal.aborted) setSettled({ load, version, data, error: undefined })
      },
      (reason: unknown) => {
        if (controller.signal.aborted) return
        const error = reason instanceof Error ? reason : new Error(String(reason))
        setSettled((previous) => ({ load, version, data: previous?.data, error }))
      },
    )

    return () => controller.abort()
  }, [load, version])

  const reload = useCallback(() => setVersion((v) => v + 1), [])

  // Loading is derived: the latest settled result belongs to an older request (or none yet).
  const isLoading = settled?.load !== load || settled.version !== version

  return {
    data: settled?.data,
    error: isLoading ? undefined : settled?.error,
    isLoading,
    reload,
  }
}
