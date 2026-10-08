import { describe, expect, it } from 'vitest'
import { buildChartData } from './chartData'

describe('buildChartData', () => {
  it('merges city series into one row per timestamp', () => {
    const { rows, keys } = buildChartData([
      {
        city: 'London',
        country: 'GB',
        points: [
          { timestampUtc: '2026-10-07T10:01:00Z', temperature: 11, temperatureMin: 10, temperatureMax: 12 },
          { timestampUtc: '2026-10-07T10:00:00Z', temperature: 10, temperatureMin: 9, temperatureMax: 11 },
        ],
      },
      {
        city: 'Rome',
        country: 'IT',
        points: [{ timestampUtc: '2026-10-07T10:00:00Z', temperature: 20, temperatureMin: 19, temperatureMax: 21 }],
      },
    ])

    expect(keys.map((k) => k.label)).toEqual(['London, GB', 'Rome, IT'])
    expect(rows).toEqual([
      { timestamp: Date.parse('2026-10-07T10:00:00Z'), t0: 10, r0: [9, 11], t1: 20, r1: [19, 21] },
      { timestamp: Date.parse('2026-10-07T10:01:00Z'), t0: 11, r0: [10, 12] },
    ])
  })

  it('handles empty input', () => {
    expect(buildChartData([])).toEqual({ rows: [], keys: [] })
  })
})
