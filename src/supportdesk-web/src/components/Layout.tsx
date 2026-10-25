import { useCallback, useEffect, useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { notificationsChangedEvent } from '../utils/events'
import { initials } from '../utils/format'

interface NavItem {
  to: string
  label: string
  icon: 'dashboard' | 'tickets' | 'knowledge' | 'notifications' | 'sla' | 'admin'
  permissions?: string[]
}

const navItems: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: 'dashboard', permissions: ['reports.view.all'] },
  { to: '/tickets', label: 'Tickets', icon: 'tickets', permissions: ['tickets.view.all', 'tickets.view.own'] },
  { to: '/knowledge', label: 'Knowledge', icon: 'knowledge', permissions: ['knowledge.view.published'] },
  { to: '/notifications', label: 'Notifications', icon: 'notifications' },
  { to: '/sla', label: 'Service levels', icon: 'sla', permissions: ['sla.view'] },
  { to: '/admin', label: 'Administration', icon: 'admin', permissions: ['roles.view', 'users.view'] },
]

function NavIcon({ name }: { name: NavItem['icon'] }) {
  const paths: Record<NavItem['icon'], string[]> = {
    dashboard: ['M3 3h7v9H3z', 'M14 3h7v5h-7z', 'M14 12h7v9h-7z', 'M3 16h7v5H3z'],
    tickets: ['M4 4h16v5a3 3 0 0 0 0 6v5H4v-5a3 3 0 0 0 0-6z', 'M12 7v2', 'M12 15v2'],
    knowledge: ['M5 4a2 2 0 0 1 2-2h12v20H7a2 2 0 0 1-2-2z', 'M5 18a2 2 0 0 1 2-2h12'],
    notifications: ['M18 9a6 6 0 1 0-12 0c0 5-2 6-2 6h16s-2-1-2-6', 'M10 20a2 2 0 0 0 4 0'],
    sla: ['M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18z', 'M12 7v5l3 2'],
    admin: ['M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2', 'M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z', 'M17 11l2 2 4-4'],
  }

  return (
    <svg className="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.7} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {paths[name].map((path) => <path key={path} d={path} />)}
    </svg>
  )
}

export default function Layout() {
  const { user, logout, can } = useAuth()
  const navigate = useNavigate()
  const [unread, setUnread] = useState(0)

  const loadUnread = useCallback(async () => {
    try {
      const count = await apiRequest<number>('/notifications/unread-count')
      setUnread(count)
    } catch {
      setUnread(0)
    }
  }, [])

  useEffect(() => {
    void loadUnread()
    const timer = window.setInterval(() => void loadUnread(), 30000)
    const handleChange = () => void loadUnread()
    window.addEventListener(notificationsChangedEvent, handleChange)

    return () => {
      window.clearInterval(timer)
      window.removeEventListener(notificationsChangedEvent, handleChange)
    }
  }, [loadUnread])

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  const visibleItems = navItems.filter((item) => !item.permissions || item.permissions.some((permission) => can(permission)))

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">SD</span>
          <span>
            Support Desk
            <span className="brand-subtitle">Customer operations</span>
          </span>
        </div>
        <div className="sidebar-section">Workspace</div>
        <nav aria-label="Main navigation">
          {visibleItems.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.to === '/'}>
              <NavIcon name={item.icon} />
              <span className="nav-label">{item.label}</span>
              {item.to === '/notifications' && unread > 0 ? <span className="badge">{unread}</span> : null}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          Signed in as
          <br />
          {user?.email}
        </div>
      </aside>
      <div className="main-column">
        <header className="topbar">
          <div className="topbar-user">
            <span className="avatar">{initials(user?.displayName)}</span>
            <div>
              <div className="topbar-user-name">{user?.displayName}</div>
              <div className="topbar-user-roles">{user?.roles.join(' · ')}</div>
            </div>
          </div>
          <div className="topbar-actions">
            <Link to="/tickets/new" className="button primary">
              New ticket
            </Link>
            <button type="button" className="button" onClick={handleLogout}>
              Sign out
            </button>
          </div>
        </header>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
