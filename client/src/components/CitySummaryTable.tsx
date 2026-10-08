import type { CitySummary } from '../api/types'
import { formatDateTime, formatTemperature } from '../utils/dateTime'

export function CitySummaryTable({ cities }: { cities: CitySummary[] }) {
  return (
    <table className="table">
      <caption className="visually-hidden">Temperature summary per city for the selected period</caption>
      <thead>
        <tr>
          <th scope="col">Country</th>
          <th scope="col">City</th>
          <th scope="col" className="num">Temperature</th>
          <th scope="col" className="num">Min</th>
          <th scope="col" className="num">Max</th>
          <th scope="col">Last update</th>
          <th scope="col" className="num">Samples</th>
        </tr>
      </thead>
      <tbody>
        {cities.map((c) => (
          <tr key={c.city}>
            <td>{c.country}</td>
            <td>{c.city}</td>
            <td className="num">{formatTemperature(c.currentTemperature)}</td>
            <td className="num">{formatTemperature(c.minTemperature)}</td>
            <td className="num">{formatTemperature(c.maxTemperature)}</td>
            <td title={`Fetched ${formatDateTime(c.lastFetchedAtUtc)}`}>{formatDateTime(c.lastObservedAtUtc)}</td>
            <td className="num">{c.sampleCount}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
