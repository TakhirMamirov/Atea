/**
 * Shown instead of a page when it fails to load or render, e.g. when a new version was deployed while the app was
 * open and the old page code no longer exists. Reloading fetches the current version.
 */
export function RouteError() {
  return (
    <div className="notice notice-error" role="alert">
      <p>Something went wrong while showing this page.</p>
      <button type="button" className="button" onClick={() => window.location.reload()}>
        Reload page
      </button>
    </div>
  )
}
