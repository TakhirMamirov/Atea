import { useCallback, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router'
import { useApi } from '../api/ApiContext'
import { CitySummaryTable } from '../components/CitySummaryTable'
import { DateRangeFilter } from '../components/DateRangeFilter'
import { PageHeader } from '../components/PageHeader'
import { StatusMessage } from '../components/StatusMessage'
import { TemperatureChart } from '../components/TemperatureChart'
import { AUTO_REFRESH_MS } from '../config'
import { useAsync } from '../hooks/useAsync'
import { useAutoRefresh } from '../hooks/useAutoRefresh'
import {
  formatDateTime,
  resolveRange,
  selectionFromSearchParams,
  selectionToSearchParams,
  type RangeSelection,
} from '../utils/dateTime'

export function WeatherPage() {
  const { weather } = useApi()
  const [searchParams, setSearchParams] = useSearchParams()
  const selection = useMemo(() => selectionFromSearchParams(searchParams), [searchParams])
  const [showMinMax, setShowMinMax] = useState(true)

  // Presets are re-resolved against "now" on every load, so refreshes slide the window forward.
  const load = useCallback((signal: AbortSignal) => weather.getReport(resolveRange(selection), signal), [weather, selection])
  const { data, error, isLoading, reload } = useAsync(load)
  useAutoRefresh(reload, selection.kind === 'preset' ? AUTO_REFRESH_MS : null)

  const handleRangeChange = (next: RangeSelection) => setSearchParams(selectionToSearchParams(next))
  // The server echoes the exact range it used; prefer it so the chart axis matches the data.
  const range = useMemo(
    () => (data ? { from: new Date(data.fromUtc), to: new Date(data.toUtc) } : resolveRange(selection)),
    [data, selection],
  )
  const hasData = (data?.series.length ?? 0) > 0

  return (
    <>
      <PageHeader
        title="Weather"
        description="Temperature for all monitored cities. Times are shown in your local time zone."
        actions={
          <button type="button" className="button" onClick={reload} disabled={isLoading}>
            {isLoading ? 'Refreshing…' : 'Refresh'}
          </button>
        }
      />

      <section className="panel">
        <DateRangeFilter key={searchParams.toString()} value={selection} onChange={handleRangeChange} />
      </section>

      <StatusMessage
        error={error}
        isLoading={isLoading}
        isEmpty={!hasData}
        emptyText="No weather data for the selected period."
      />

      {data && hasData && (
        <>
          <section className="panel">
            <div className="panel-header">
              <h2>
                Temperature, {formatDateTime(range.from)} – {formatDateTime(range.to)}
              </h2>
              <label className="checkbox">
                <input type="checkbox" checked={showMinMax} onChange={(e) => setShowMinMax(e.target.checked)} />
                Show min/max band
              </label>
            </div>
            <TemperatureChart series={data.series} range={range} showMinMax={showMinMax} />
          </section>

          <section className="panel">
            <div className="panel-header">
              <h2>Cities</h2>
              <span className="muted">Min and max are the extremes within the selected period</span>
            </div>
            <CitySummaryTable cities={data.cities} />
          </section>
        </>
      )}
    </>
  )
}
