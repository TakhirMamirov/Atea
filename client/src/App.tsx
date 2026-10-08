import { createBrowserRouter, RouterProvider } from 'react-router'
import { ApiProvider, type Api } from './api/ApiContext'
import { Layout } from './components/Layout'
import { RouteError } from './components/RouteError'
import { NotFoundPage } from './pages/NotFoundPage'

// Pages are code-split so the charting library only loads on the weather page.
const router = createBrowserRouter([
  {
    element: <Layout />,
    errorElement: <RouteError />,
    hydrateFallbackElement: <p className="notice">Loading…</p>,
    children: [
      {
        // Page errors render inside the layout, so the header and navigation stay usable.
        errorElement: <RouteError />,
        children: [
          { index: true, lazy: async () => ({ Component: (await import('./pages/WeatherPage')).WeatherPage }) },
          { path: 'logs', lazy: async () => ({ Component: (await import('./pages/LogsPage')).LogsPage }) },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
])

export function App({ api }: { api: Api }) {
  return (
    <ApiProvider api={api}>
      <RouterProvider router={router} />
    </ApiProvider>
  )
}
