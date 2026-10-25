import { useEffect, useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { apiRequest } from '../api/client'
import type { PagedResult, Notification } from '../api/types'
import { useAuth } from '../auth/AuthContext'

interface NavItem {
  to: string
  label: string
  permissions?: string[]
}

const navItems: NavItem[] = [
  { to: '/', label: 'Dashboard', permissions: ['reports.view.all'] },
  { to: '/tickets', label: 'Tickets', permissions: ['tickets.view.all', 'tickets.view.own'] },
  { to: '/knowledge', label: 'Knowledge', permissions: ['knowledge.view.published'] },
  { to: '/notifications', label: 'Notifications' },
  { to: '/sla', label: 'Service levels', permissions: ['sla.view'] },
  { to: '/admin', label: 'Administration', permissions: ['roles.view', 'users.view'] },
]

export default function Layout() {
  const { user, logout, can } = useAuth()
  const navigate = useNavigate()
  const [unread, setUnread] = useState(0)

  useEffect(() => {
    let active = true
    const load = async () => {
      try {
        const count = await apiRequest<number>('/notifications/unread-count')
        if (active) {
          setUnread(count)
        }
      } catch {
        if (active) {
          setUnread(0)
        }
      }
    }

    void load()
    const timer = window.setInterval(load, 30000)
    return () => {
      active = false
      window.clearInterval(timer)
    }
  }, [])

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
          <span>Support Desk</span>
        </div>
        <nav aria-label="Main navigation">
          {visibleItems.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.to === '/'}>
              {item.label}
              {item.to === '/notifications' && unread > 0 ? <span className="badge">{unread}</span> : null}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="main-column">
        <header className="topbar">
          <div>
            <strong>{user?.displayName}</strong>
            <span className="muted"> {user?.roles.join(', ')}</span>
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

export type { PagedResult, Notification }
