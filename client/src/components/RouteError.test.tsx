import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { RouteError } from './RouteError'

function BrokenPage(): never {
  throw new Error('render failed')
}

describe('RouteError', () => {
  it('replaces a page that fails to render', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {}) // React logs the caught error
    const router = createMemoryRouter([{ path: '/', element: <BrokenPage />, errorElement: <RouteError /> }])

    render(<RouterProvider router={router} />)

    expect(screen.getByRole('alert')).toHaveTextContent('Something went wrong while showing this page.')
    expect(screen.getByRole('button', { name: 'Reload page' })).toBeInTheDocument()
  })
})
