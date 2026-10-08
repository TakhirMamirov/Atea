import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router'
import { useApi } from '../api/ApiContext'
import type { FetchLogQuery } from '../api/types'
import { LogsTable } from '../components/LogsTable'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { StatusMessage } from '../components/StatusMessage'
import { AUTO_REFRESH_MS, CITIES } from '../config'
import { useAsync } from '../hooks/useAsync'
import { useAutoRefresh } from '../hooks/useAutoRefresh'

const PAGE_SIZE = 20
type StatusFilter = '' | 'success' | 'failure'

function queryFromSearchParams(params: URLSearchParams): FetchLogQuery {
  const page = Number.parseInt(params.get('page') ?? '1', 10)
  const status = params.get('status')
  return {
    page: Number.isFinite(page) && page > 0 ? page : 1,
    pageSize: PAGE_SIZE,
    city: params.get('city') || undefined,
    isSuccess: status === 'success' ? true : status === 'failure' ? false : undefined,
  }
}

export function LogsPage() {
  const { fetchLogs } = useApi()
  const [searchParams, setSearchParams] = useSearchParams()
  const query = useMemo(() => queryFromSearchParams(searchParams), [searchParams])

  const load = useCallback((signal: AbortSignal) => fetchLogs.getLogs(query, signal), [fetchLogs, query])
  const { data, error, isLoading, reload } = useAsync(load)
  // Only the first page shows new entries, so only it auto-refreshes.
  useAutoRefresh(reload, query.page === 1 ? AUTO_REFRESH_MS : null)

  function updateParams(changes: Record<string, string>) {
    const next = new URLSearchParams(searchParams)
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value)
      else next.delete(key)
    }
    setSearchParams(next)
  }

  const status: StatusFilter = query.isSuccess === undefined ? '' : query.isSuccess ? 'success' : 'failure'
  const items = data?.items ?? []

  return (
    <>
      <PageHeader
        title="Fetch logs"
        description="Every attempt to fetch data from OpenWeatherMap, newest first."
        actions={
          <button type="button" className="button" onClick={reload} disabled={isLoading}>
            {isLoading ? 'Refreshing…' : 'Refresh'}
          </button>
        }
      />

      <section className="panel">
        <div className="filter-bar">
          <label className="field">
            <span>City</span>
            <select value={query.city ?? ''} onChange={(e) => updateParams({ city: e.target.value, page: '' })}>
              <option value="">All cities</option>
              {CITIES.map((city) => (
                <option key={city} value={city}>
                  {city}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Result</span>
            <select value={status} onChange={(e) => updateParams({ status: e.target.value, page: '' })}>
              <option value="">All results</option>
              <option value="success">Success</option>
              <option value="failure">Failure</option>
            </select>
          </label>
        </div>
      </section>

      <StatusMessage error={error} isLoading={isLoading} isEmpty={items.length === 0} emptyText="No log entries found." />

      {data && items.length > 0 && (
        <section className="panel panel-flush">
          <LogsTable logs={items} />
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={(page) => updateParams({ page: page > 1 ? String(page) : '' })}
          />
        </section>
      )}
    </>
  )
}
