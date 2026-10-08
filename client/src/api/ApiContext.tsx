import { createContext, use, type ReactNode } from 'react'
import { createFetchLogsApi, type FetchLogsApi } from './fetchLogsApi'
import { createFetchHttpClient } from './httpClient'
import { createWeatherApi, type WeatherApi } from './weatherApi'

export interface Api {
  weather: WeatherApi
  fetchLogs: FetchLogsApi
}

export function createApi(): Api {
  const http = createFetchHttpClient()
  return { weather: createWeatherApi(http), fetchLogs: createFetchLogsApi(http) }
}

const ApiContext = createContext<Api | null>(null)

/** Supplies API implementations to the component tree; tests inject fakes here. */
export function ApiProvider({ api, children }: { api: Api; children: ReactNode }) {
  return <ApiContext value={api}>{children}</ApiContext>
}

export function useApi(): Api {
  const api = use(ApiContext)
  if (!api) throw new Error('useApi must be used within an ApiProvider')
  return api
}
