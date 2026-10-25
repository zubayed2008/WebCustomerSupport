import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiRequest } from '../api/client'
import type { PagedResult, TicketListItem, TicketPriority } from '../api/types'
import { useAuth } from '../auth/AuthContext'

const priorities: (TicketPriority | '')[] = ['', 'Urgent', 'High', 'Normal', 'Low']
const statuses = ['', 'New', 'Open', 'PendingCustomer', 'PendingInternal', 'Resolved', 'Closed']

export default function TicketsPage() {
  const { can } = useAuth()
  const [items, setItems] = useState<TicketListItem[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [queue, setQueue] = useState(can('tickets.view.all') ? 'active' : '')
  const [status, setStatus] = useState('')
  const [priority, setPriority] = useState('')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const pageSize = 25

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
      if (queue) params.set('queue', queue)
      if (status) params.set('status', status)
      if (priority) params.set('priority', priority)
      if (search.trim()) params.set('search', search.trim())
      const result = await apiRequest<PagedResult<TicketListItem>>(`/tickets?${params.toString()}`)
      setItems(result.items)
      setTotal(result.totalCount)
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load tickets.')
    } finally {
      setLoading(false)
    }
  }, [page, queue, status, priority, search])

  useEffect(() => {
    void load()
  }, [load])

  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  return (
    <div>
      <div className="page-header">
        <h1>{can('tickets.view.all') ? 'Ticket queue' : 'My tickets'}</h1>
        <Link className="button primary" to="/tickets/new">
          New ticket
        </Link>
      </div>

      <form
        className="filters"
        onSubmit={(event) => {
          event.preventDefault()
          setPage(1)
          void load()
        }}
      >
        <label>
          Search
          <input value={search} maxLength={100} onChange={(event) => setSearch(event.target.value)} placeholder="Number, subject, or description" />
        </label>
        {can('tickets.view.all') ? (
          <label>
            Queue
            <select value={queue} onChange={(event) => { setQueue(event.target.value); setPage(1) }}>
              <option value="">All tickets</option>
              <option value="active">Active</option>
              <option value="unassigned">Unassigned</option>
              <option value="mine">Assigned to me</option>
              <option value="overdue">Overdue</option>
            </select>
          </label>
        ) : null}
        <label>
          Status
          <select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
            {statuses.map((value) => (
              <option key={value} value={value}>{value || 'Any status'}</option>
            ))}
          </select>
        </label>
        <label>
          Priority
          <select value={priority} onChange={(event) => { setPriority(event.target.value); setPage(1) }}>
            {priorities.map((value) => (
              <option key={value} value={value}>{value || 'Any priority'}</option>
            ))}
          </select>
        </label>
        <button type="submit" className="button">Apply</button>
      </form>

      {error ? <div className="alert error">{error}</div> : null}
      {loading ? <p>Loading…</p> : null}

      <table className="table">
        <thead>
          <tr>
            <th>Number</th>
            <th>Subject</th>
            <th>Status</th>
            <th>Priority</th>
            <th>Requester</th>
            <th>Assignee</th>
            <th>SLA</th>
            <th>Updated</th>
          </tr>
        </thead>
        <tbody>
          {items.map((ticket) => (
            <tr key={ticket.id}>
              <td><Link to={`/tickets/${ticket.id}`}>{ticket.number}</Link></td>
              <td>{ticket.subject}</td>
              <td><span className={`pill status-${ticket.status.toLowerCase()}`}>{ticket.status}</span></td>
              <td>{ticket.priority}</td>
              <td>{ticket.requesterName}</td>
              <td>{ticket.assigneeName ?? 'Unassigned'}</td>
              <td>
                {ticket.isOverdue ? <span className="pill danger">Overdue</span> : null}
                {!ticket.isOverdue && ticket.isDueSoon ? <span className="pill warning">Due soon</span> : null}
                {!ticket.isOverdue && !ticket.isDueSoon ? <span className="muted">On track</span> : null}
              </td>
              <td>{new Date(ticket.updatedAt).toLocaleString()}</td>
            </tr>
          ))}
          {!loading && items.length === 0 ? (
            <tr>
              <td colSpan={8} className="muted">No tickets match the current filters.</td>
            </tr>
          ) : null}
        </tbody>
      </table>

      <div className="pagination">
        <button type="button" className="button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>
          Previous
        </button>
        <span>Page {page} of {totalPages} ({total} tickets)</span>
        <button type="button" className="button" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)}>
          Next
        </button>
      </div>
    </div>
  )
}
