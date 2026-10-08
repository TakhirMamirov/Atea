interface StatusMessageProps {
  error?: Error
  isLoading: boolean
  isEmpty: boolean
  emptyText: string
}

/**
 * Renders the error, first-load or empty state; renders nothing when content is available.
 * `<output>` is announced politely by screen readers (implicit role "status"); errors use role "alert".
 */
export function StatusMessage({ error, isLoading, isEmpty, emptyText }: StatusMessageProps) {
  if (error) {
    return (
      <p className="notice notice-error" role="alert">
        Could not load data: {error.message}
      </p>
    )
  }
  if (isEmpty && isLoading) return <output className="notice">Loading…</output>
  if (isEmpty) return <output className="notice">{emptyText}</output>
  return null
}
