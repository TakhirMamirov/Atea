import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { WeatherReport } from '../api/types'
import { createFakeApi, renderPage } from '../test/renderWithProviders'
import { WeatherPage } from './WeatherPage'

// Recharts needs real layout; the chart is covered by chartData tests.
vi.mock('../components/TemperatureChart', () => ({
  TemperatureChart: ({ series }: { series: unknown[] }) => <div data-testid="chart">{series.length} series</div>,
}))

const report: WeatherReport = {
  fromUtc: '2026-10-06T12:00:00Z',
  toUtc: '2026-10-07T12:00:00Z',
  series: [
    { city: 'London', country: 'GB', points: [{ timestampUtc: '2026-10-07T11:00:00Z', temperature: 12, temperatureMin: 11, temperatureMax: 13 }] },
    { city: 'Riga', country: 'LV', points: [{ timestampUtc: '2026-10-07T11:00:00Z', temperature: 7, temperatureMin: 6, temperatureMax: 8 }] },
  ],
  cities: [
    { city: 'London', country: 'GB', currentTemperature: 12, minTemperature: 9.5, maxTemperature: 14.25, lastObservedAtUtc: '2026-10-07T11:55:00Z', lastFetchedAtUtc: '2026-10-07T11:59:00Z', sampleCount: 1440 },
    { city: 'Riga', country: 'LV', currentTemperature: 7, minTemperature: 3, maxTemperature: 9, lastObservedAtUtc: '2026-10-07T11:50:00Z', lastFetchedAtUtc: '2026-10-07T11:59:00Z', sampleCount: 1438 },
  ],
}

describe('WeatherPage', () => {
  it('renders the chart and a summary row per city', async () => {
    const api = createFakeApi({ report })
    renderPage(<WeatherPage />, api)

    expect(await screen.findByTestId('chart')).toHaveTextContent('2 series')
    const rows = within(screen.getByRole('table')).getAllByRole('row')
    expect(rows).toHaveLength(3)
    expect(within(rows[1]).getAllByRole('cell').map((c) => c.textContent)).toEqual(
      expect.arrayContaining(['GB', 'London', '12.0 °C', '9.5 °C', '14.3 °C', '1440']),
    )
  })

  it('requests the selected preset range', async () => {
    const api = createFakeApi({ report })
    const { router } = renderPage(<WeatherPage />, api)
    await screen.findByTestId('chart')

    await userEvent.click(screen.getByRole('button', { name: 'Last hour' }))

    expect(router.state.location.search).toBe('?range=1h')
    const [range] = api.weather.getReport.mock.lastCall!
    expect(range.to.getTime() - range.from.getTime()).toBe(3_600_000)
  })

  it('shows an empty state when there is no data', async () => {
    renderPage(<WeatherPage />, createFakeApi())

    expect(await screen.findByText('No weather data for the selected period.')).toBeInTheDocument()
  })
})
