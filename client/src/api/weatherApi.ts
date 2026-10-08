import type { HttpClient } from './httpClient'
import type { DateRange, WeatherReport } from './types'

export interface WeatherApi {
  getReport(range: DateRange, signal?: AbortSignal): Promise<WeatherReport>
}

export function createWeatherApi(http: HttpClient): WeatherApi {
  return {
    getReport: (range, signal) => http.getJson<WeatherReport>('/weather', { from: range.from, to: range.to }, signal),
  }
}
