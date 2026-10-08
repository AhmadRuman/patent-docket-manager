import { NavLink, Outlet } from 'react-router-dom'

export function Layout() {
  return (
    <div className="shell">
      <div className="demo-banner" role="note">
        Demo application. All matters, clients, people and application numbers are fictional.
      </div>
      <header className="topbar">
        <NavLink to="/" className="brand">
          <span className="brand-mark" aria-hidden>§</span>
          <span className="brand-text">Patent Docket</span>
        </NavLink>
        <nav>
          <NavLink to="/" end>
            Dashboard
          </NavLink>
          <NavLink to="/docket">Docket</NavLink>
          <NavLink to="/matters">Matters</NavLink>
        </nav>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
