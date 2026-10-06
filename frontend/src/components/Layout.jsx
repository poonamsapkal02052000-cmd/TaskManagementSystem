import { useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import NotificationBell from './NotificationBell';

export default function Layout() {
  const { user, isAdmin, logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);
  const close = () => setMenuOpen(false);

  return (
    <div className="app">
      <a href="#main" className="skip-link">
        Skip to content
      </a>
      <header className="topbar">
        <div className="topbar-inner">
          <NavLink to="/" className="brand" onClick={close}>
            <span className="brand-mark" aria-hidden="true">✓</span> TaskManager
          </NavLink>

          <button
            type="button"
            className="icon-btn menu-toggle"
            aria-label="Toggle navigation"
            aria-expanded={menuOpen}
            aria-controls="main-nav"
            onClick={() => setMenuOpen((o) => !o)}
          >
            ☰
          </button>

          <nav id="main-nav" className={`nav ${menuOpen ? 'open' : ''}`} aria-label="Main">
            <NavLink to="/" end onClick={close}>Dashboard</NavLink>
            <NavLink to="/tasks" onClick={close}>Tasks</NavLink>
            <NavLink to="/teams" onClick={close}>Teams</NavLink>
            {isAdmin && <NavLink to="/users" onClick={close}>Users</NavLink>}
            <button type="button" className="nav-logout" onClick={() => logout()}>
              Log out ({user.fullName})
            </button>
          </nav>

          <div className="topbar-right">
            <NotificationBell />
            <div className="user-chip" title={user.email}>
              <span className="avatar" aria-hidden="true">{user.fullName.charAt(0).toUpperCase()}</span>
              <span className="user-meta">
                <span className="user-name">{user.fullName}</span>
                <span className={`role-tag role-${user.role}`}>{user.role}</span>
              </span>
            </div>
            <button type="button" className="btn btn-ghost topbar-logout" onClick={() => logout()}>
              Log out
            </button>
          </div>
        </div>
      </header>

      <main id="main" className="container">
        <Outlet />
      </main>
    </div>
  );
}
