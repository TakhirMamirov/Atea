import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { vi } from 'vitest'
import { ApiProvider, type Api } from '../api/ApiContext'
import type { FetchLogsApi } from '../api/fetchLogsApi'
import type { FetchLog, PagedResult, WeatherReport } from '../api/types'
import type { WeatherApi } from '../api/weatherApi'

export function createFakeApi(overrides: Partial<{ report: WeatherReport; logs: PagedResult<FetchLog> }> = {}) {
  const api = {
    weather: {
      getReport: vi.fn<WeatherApi['getReport']>().mockResolvedValue(
        overrides.report ?? { fromUtc: '2026-10-06T12:00:00Z', toUtc: '2026-10-07T12:00:00Z', series: [], cities: [] },
      ),
    },
    fetchLogs: {
      getLogs: vi.fn<FetchLogsApi['getLogs']>().mockResolvedValue(
        overrides.logs ?? { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 },
      ),
    },
  } satisfies Api
  return api
}

/** Renders a page inside the API provider and an in-memory router at `path`. */
export function renderPage(element: ReactElement, api: Api, path = '/') {
  const router = createMemoryRouter([{ path: '*', element }], { initialEntries: [path] })
  return {
    router,
    ...render(
      <ApiProvider api={api}>
        <RouterProvider router={router} />
      </ApiProvider>,
    ),
  }
}
