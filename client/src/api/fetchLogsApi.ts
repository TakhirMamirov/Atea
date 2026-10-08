import type { HttpClient } from './httpClient'
import type { FetchLog, FetchLogQuery, PagedResult } from './types'

export interface FetchLogsApi {
  getLogs(query: FetchLogQuery, signal?: AbortSignal): Promise<PagedResult<FetchLog>>
}

export function createFetchLogsApi(http: HttpClient): FetchLogsApi {
  return {
    getLogs: (query, signal) =>
      http.getJson<PagedResult<FetchLog>>(
        '/fetch-logs',
        { page: query.page, pageSize: query.pageSize, city: query.city, isSuccess: query.isSuccess },
        signal,
      ),
  }
}
