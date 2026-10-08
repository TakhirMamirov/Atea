import type { CitySeries } from '../api/types'

export interface ChartSeriesKey {
  city: string
  label: string
  temperatureKey: string
  rangeKey: string
}

export type ChartRow = { timestamp: number } & Record<string, number | [number, number] | undefined>

/**
 * Merges per-city series into one row per timestamp. The API aligns every city to the same time buckets,
 * so rows line up and a single tooltip can show all cities at once.
 */
export function buildChartData(series: CitySeries[]): { rows: ChartRow[]; keys: ChartSeriesKey[] } {
  const rowsByTimestamp = new Map<number, ChartRow>()

  const keys = series.map((s, index) => {
    const key: ChartSeriesKey = {
      city: s.city,
      label: s.country ? `${s.city}, ${s.country}` : s.city,
      temperatureKey: `t${index}`,
      rangeKey: `r${index}`,
    }

    for (const point of s.points) {
      const timestamp = Date.parse(point.timestampUtc)
      let row = rowsByTimestamp.get(timestamp)
      if (!row) {
        row = { timestamp }
        rowsByTimestamp.set(timestamp, row)
      }
      row[key.temperatureKey] = point.temperature
      row[key.rangeKey] = [point.temperatureMin, point.temperatureMax]
    }

    return key
  })

  const rows = [...rowsByTimestamp.values()].sort((a, b) => a.timestamp - b.timestamp)
  return { rows, keys }
}
