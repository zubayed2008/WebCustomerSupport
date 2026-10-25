import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiRequest } from '../api/client'
import type { Notification, PagedResult } from '../api/types'

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

  const markRead = async (id: string) => {
    await apiRequest(`/notifications/${id}/read`, { method: 'POST' })
    await load()
  }

  const markAll = async () => {
    await apiRequest('/notifications/read-all', { method: 'POST' })
    await load()
  }

  return (
    <div>
      <div className="page-header">
        <h1>Notifications</h1>
        <button type="button" className="button" onClick={markAll}>
          Mark all read
        </button>
      </div>
      {error ? <div className="alert error">{error}</div> : null}
      {loading ? <p>Loading…</p> : null}
      <ul className="list">
        {items.map((item) => (
          <li key={item.id} className={item.isRead ? 'list-item' : 'list-item unread'}>
            <div>
              <strong>{item.title}</strong>
              {item.body ? <p className="muted">{item.body}</p> : null}
              <span className="muted small">{new Date(item.createdAt).toLocaleString()}</span>
            </div>
            <div className="row-actions">
              {item.link ? <Link to={item.link}>Open</Link> : null}
              {!item.isRead ? (
                <button type="button" className="button small" onClick={() => markRead(item.id)}>
                  Mark read
                </button>
              ) : null}
            </div>
          </li>
        ))}
        {!loading && items.length === 0 ? <li className="muted">No notifications yet.</li> : null}
      </ul>
    </div>
  )
}
