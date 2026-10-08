import { describe, expect, it, vi } from 'vitest'
import { ApiError, buildQueryString, createFetchHttpClient } from './httpClient'

describe('buildQueryString', () => {
  it('skips empty values and serializes dates as ISO strings', () => {
    const qs = buildQueryString({
      page: 2,
      city: '',
      isSuccess: false,
      missing: undefined,
      from: new Date('2026-10-07T10:00:00Z'),
    })
    expect(qs).toBe('?page=2&isSuccess=false&from=2026-10-07T10%3A00%3A00.000Z')
  })

  it('returns an empty string when there is nothing to send', () => {
    expect(buildQueryString({ a: undefined })).toBe('')
  })
})

describe('createFetchHttpClient', () => {
  it('requests the base URL with query and returns parsed JSON', async () => {
    const fetchFn = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify({ ok: 1 }), { status: 200 }))
    const client = createFetchHttpClient('/api', fetchFn)

    const result = await client.getJson<{ ok: number }>('/weather', { page: 1 })

    expect(result).toEqual({ ok: 1 })
    expect(fetchFn).toHaveBeenCalledWith('/api/weather?page=1', expect.objectContaining({ headers: { Accept: 'application/json' } }))
  })

  it('throws ApiError with the first validation message from problem details', async () => {
    const problem = { title: 'One or more validation errors occurred.', errors: { from: ["'from' must be earlier than 'to'."] } }
    const fetchFn = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify(problem), { status: 400 }))
    const client = createFetchHttpClient('/api', fetchFn)

    const error = await client.getJson('/weather').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, message: "'from' must be earlier than 'to'." })
  })

  it('falls back to a generic message when the error body is not JSON', async () => {
    const fetchFn = vi.fn<typeof fetch>().mockResolvedValue(new Response('Bad gateway', { status: 502 }))
    const client = createFetchHttpClient('/api', fetchFn)

    await expect(client.getJson('/weather')).rejects.toThrow('Request failed with status 502')
  })

  it('explains a rate-limit rejection in plain words', async () => {
    const fetchFn = vi.fn<typeof fetch>().mockResolvedValue(new Response('{"status":429}', { status: 429 }))
    const client = createFetchHttpClient('/api', fetchFn)

    await expect(client.getJson('/weather')).rejects.toMatchObject({
      status: 429,
      message: 'Too many requests. Please wait a minute and try again.',
    })
  })

  it('fails with a timeout message when the server does not respond in time', async () => {
    const client = createFetchHttpClient('/api', neverRespondingFetch(), 20)

    await expect(client.getJson('/weather')).rejects.toThrow('The server did not respond in time.')
  })

  it('passes a cancellation by the caller through unchanged', async () => {
    const client = createFetchHttpClient('/api', neverRespondingFetch(), 10_000)
    const controller = new AbortController()

    const request = client.getJson('/weather', undefined, controller.signal)
    controller.abort()

    await expect(request).rejects.toMatchObject({ name: 'AbortError' })
  })
})

/** A fetch that only settles when its signal is aborted, like a hanging server. */
function neverRespondingFetch() {
  return vi.fn<typeof fetch>(
    (_input, init) =>
      new Promise<Response>((_resolve, reject) => {
        init?.signal?.addEventListener('abort', () => reject(init.signal?.reason))
      }),
  )
}
