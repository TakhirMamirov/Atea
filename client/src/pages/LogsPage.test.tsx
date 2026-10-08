import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { createFakeApi, renderPage } from '../test/renderWithProviders'
import { LogsPage } from './LogsPage'

const logs = {
  items: [
    { id: 2, city: 'Rome', attemptedAtUtc: '2026-10-07T10:01:00Z', isSuccess: false, httpStatusCode: 401, durationMs: 120, errorMessage: 'Invalid API key' },
    { id: 1, city: 'London', attemptedAtUtc: '2026-10-07T10:00:00Z', isSuccess: true, httpStatusCode: 200, durationMs: 95, errorMessage: null },
  ],
  totalCount: 120,
  page: 1,
  pageSize: 20,
  totalPages: 6,
}

describe('LogsPage', () => {
  it('renders log entries with status and pagination', async () => {
    const api = createFakeApi({ logs })
    renderPage(<LogsPage />, api, '/logs')

    const table = await screen.findByRole('table')
    const rows = within(table).getAllByRole('row')
    expect(rows).toHaveLength(3)
    expect(within(rows[1]).getByText('Failure')).toBeInTheDocument()
    expect(within(rows[1]).getByText('Invalid API key')).toBeInTheDocument()
    expect(within(rows[2]).getByText('Success')).toBeInTheDocument()
    expect(screen.getByText(/120 entries · page 1 of 6/)).toBeInTheDocument()
    expect(api.fetchLogs.getLogs).toHaveBeenCalledWith({ page: 1, pageSize: 20, city: undefined, isSuccess: undefined }, expect.any(AbortSignal))
  })

  it('reloads with filters and resets paging when a filter changes', async () => {
    const api = createFakeApi({ logs })
    const { router } = renderPage(<LogsPage />, api, '/logs?page=2')
    await screen.findByRole('table')

    await userEvent.selectOptions(screen.getByLabelText('City'), 'Riga')
    await userEvent.selectOptions(screen.getByLabelText('Result'), 'failure')

    expect(api.fetchLogs.getLogs).toHaveBeenLastCalledWith({ page: 1, pageSize: 20, city: 'Riga', isSuccess: false }, expect.any(AbortSignal))
    expect(router.state.location.search).toBe('?city=Riga&status=failure')
  })

  it('shows an error message when loading fails', async () => {
    const api = createFakeApi()
    api.fetchLogs.getLogs.mockRejectedValue(new Error('Network down'))
    renderPage(<LogsPage />, api, '/logs')

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })
})
