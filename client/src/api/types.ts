/** DTOs returned by the ASP.NET Core API. Timestamps are ISO-8601 UTC strings. */

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface FetchLog {
  id: number
  city: string
  attemptedAtUtc: string
  isSuccess: boolean
  httpStatusCode: number | null
  durationMs: number
  errorMessage: string | null
}

export interface FetchLogQuery {
  page: number
  pageSize: number
  city?: string
  isSuccess?: boolean
}

export interface TemperatureSample {
  timestampUtc: string
  temperature: number
  temperatureMin: number
  temperatureMax: number
}

export interface CitySeries {
  city: string
  country: string
  points: TemperatureSample[]
}

export interface CitySummary {
  city: string
  country: string
  currentTemperature: number
  minTemperature: number
  maxTemperature: number
  lastObservedAtUtc: string
  lastFetchedAtUtc: string
  sampleCount: number
}

export interface WeatherReport {
  fromUtc: string
  toUtc: string
  series: CitySeries[]
  cities: CitySummary[]
}

export interface DateRange {
  from: Date
  to: Date
}
