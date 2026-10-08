import { NavLink, Outlet } from 'react-router'

export function Layout() {
  return (
    <div className="app">
      <header className="app-header">
        <div className="container header-inner">
          <span className="brand">Cloud Reports</span>
          <nav className="nav" aria-label="Main">
            <NavLink to="/" end className="nav-link">
              Weather
            </NavLink>
            <NavLink to="/logs" className="nav-link">
              Fetch logs
            </NavLink>
          </nav>
        </div>
      </header>
      <main className="container main">
        <Outlet />
      </main>
    </div>
  )
}
