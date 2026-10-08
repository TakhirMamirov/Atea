export type QueryValue = string | number | boolean | Date | null | undefined
export type QueryParams = Record<string, QueryValue>

/** Minimal HTTP abstraction so API modules don't depend on `fetch` directly. */
export interface HttpClient {
  getJson<T>(path: string, query?: QueryParams, signal?: AbortSignal): Promise<T>
}

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function buildQueryString(query: QueryParams = {}): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '') continue
    params.append(key, value instanceof Date ? value.toISOString() : String(value))
  }
  const result = params.toString()
  return result ? `?${result}` : ''
}

async function toApiError(response: Response): Promise<ApiError> {
  let message = `Request failed with status ${response.status}`
  try {
    const problem = (await response.json()) as ProblemDetails
    const validationMessages = Object.values(problem.errors ?? {}).flat()
    message = validationMessages[0] ?? problem.detail ?? problem.title ?? message
  } catch {
    // Body is not JSON; keep the generic message.
  }
  return new ApiError(message, response.status)
}

export const REQUEST_TIMEOUT_MS = 15_000

export function createFetchHttpClient(
  baseUrl = '/api',
  fetchFn: typeof fetch = fetch.bind(globalThis),
  timeoutMs = REQUEST_TIMEOUT_MS,
): HttpClient {
  return {
    async getJson<T>(path: string, query?: QueryParams, signal?: AbortSignal): Promise<T> {
      // The request is aborted either by the caller (e.g. the page changed) or when it takes too long.
      const timeout = AbortSignal.timeout(timeoutMs)
      try {
        const response = await fetchFn(`${baseUrl}${path}${buildQueryString(query)}`, {
          headers: { Accept: 'application/json' },
          signal: signal ? AbortSignal.any([signal, timeout]) : timeout,
        })
        if (!response.ok) throw await toApiError(response)
        return (await response.json()) as T
      } catch (error) {
        if (timeout.aborted) throw new Error('The server did not respond in time.', { cause: error })
        throw error
      }
    },
  }
}
