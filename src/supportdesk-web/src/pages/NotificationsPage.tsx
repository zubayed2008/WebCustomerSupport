import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiRequest } from '../api/client'
import type { Notification, PagedResult } from '../api/types'
import { notifyNotificationsChanged } from '../utils/events'
import { formatDateTime } from '../utils/format'

export default function NotificationsPage() {
  const [items, setItems] = useState<Notification[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const load = async () => {
    setLoading(true)
    try {
      const result = await apiRequest<PagedResult<Notification>>('/notifications?pageSize=50')
      setItems(result.items)
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load notifications.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  const run = async (action: () => Promise<unknown>) => {
    try {
      await action()
      await load()
      notifyNotificationsChanged()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'The notification could not be updated.')
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Notifications</h1>
          <p className="page-subtitle">Assignments, replies, and service-level alerts for your account.</p>
        </div>
        <button type="button" className="button" onClick={() => void run(() => apiRequest('/notifications/read-all', { method: 'POST' }))}>
          Mark all read
        </button>
      </div>
      {error ? <div className="alert error" role="alert">{error}</div> : null}
      {loading ? <p className="loading">Loading notifications…</p> : null}
      <ul className="list">
        {items.map((item) => (
          <li key={item.id} className={item.isRead ? 'list-item' : 'list-item unread'}>
            <div>
              <strong>{item.title}</strong>
              {item.body ? <p className="muted">{item.body}</p> : null}
              <span className="muted small">{formatDateTime(item.createdAt)}</span>
            </div>
            <div className="row-actions">
              {item.link ? <Link className="button small" to={item.link}>Open</Link> : null}
              {!item.isRead ? (
                <button type="button" className="button small" onClick={() => void run(() => apiRequest(`/notifications/${item.id}/read`, { method: 'POST' }))}>
                  Mark read
                </button>
              ) : null}
            </div>
          </li>
        ))}
        {!loading && items.length === 0 ? <li className="empty-state">No notifications yet.</li> : null}
      </ul>
    </div>
  )
}
