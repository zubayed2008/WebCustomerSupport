import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiRequest } from '../api/client'
import ActivityTimeline from '../components/ActivityTimeline'
import AiPanel from '../components/AiPanel'
import CommentSection from '../components/CommentSection'
import TicketActions from '../components/TicketActions'
import TicketAttachments from '../components/TicketAttachments'
import TicketMetadataActions from '../components/TicketMetadataActions'
import type { Assignee, Category, KbArticleListItem, PagedResult, TicketDetail } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { formatDateTime, formatLabel } from '../utils/format'

export default function TicketDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { can } = useAuth()
  const [ticket, setTicket] = useState<TicketDetail | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [assignees, setAssignees] = useState<Assignee[]>([])
  const [articles, setArticles] = useState<KbArticleListItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    if (!id) {
      return
    }

    try {
      const result = await apiRequest<TicketDetail>(`/tickets/${id}`)
      setTicket(result)
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load the ticket.')
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    apiRequest<Category[]>('/lookups/categories').then(setCategories).catch(() => setCategories([]))
    if (can('tickets.assign')) {
      apiRequest<Assignee[]>('/lookups/assignees').then(setAssignees).catch(() => setAssignees([]))
    }
    if (can('knowledge.view.published')) {
      apiRequest<PagedResult<KbArticleListItem>>('/knowledge?pageSize=100')
        .then((result) => setArticles(result.items))
        .catch(() => setArticles([]))
    }
  }, [can])

  if (loading) {
    return <p className="loading">Loading ticket…</p>
  }

  if (!ticket) {
    return (
      <div>
        <div className="alert error" role="alert">{error ?? 'Ticket not found.'}</div>
        <Link to="/tickets">Back to tickets</Link>
      </div>
    )
  }

  return (
    <div className="ticket-detail">
      <div className="page-header">
        <div>
          <Link to="/tickets" className="breadcrumb muted">← Back to tickets</Link>
          <div className="ticket-number">{ticket.number}</div>
          <h1>{ticket.subject}</h1>
          <div className="header-badges">
            <span className={`pill status-${ticket.status.toLowerCase()}`}>{formatLabel(ticket.status)}</span>
            <span className={`pill priority-${ticket.priority.toLowerCase()}`}>{ticket.priority} priority</span>
            {ticket.categoryName ? <span className="pill neutral">{ticket.categoryName}</span> : null}
            {ticket.isOverdue ? <span className="pill danger">Overdue</span> : null}
            {!ticket.isOverdue && ticket.isDueSoon ? <span className="pill warning">Due soon</span> : null}
            {ticket.responseBreached ? <span className="pill danger">Response breached</span> : null}
            {ticket.resolutionBreached ? <span className="pill danger">Resolution breached</span> : null}
          </div>
        </div>
      </div>

      {error ? <div className="alert error" role="alert">{error}</div> : null}

      <div className="ticket-layout">
        <div className="ticket-main">
          <section className="card">
            <h2>Request</h2>
            <p className="request-body preserve-lines">{ticket.description}</p>
            {ticket.tags.length > 0 ? (
              <div className="tag-list">
                {ticket.tags.map((tag) => <span key={tag} className="pill neutral">{tag}</span>)}
              </div>
            ) : null}
          </section>

          <CommentSection ticket={ticket} onChanged={load} onError={setError} />
          {can('tickets.attach') ? <TicketAttachments ticket={ticket} onChanged={load} onError={setError} /> : null}
          <AiPanel ticketId={ticket.id} />
          <ActivityTimeline activities={ticket.activities} />
        </div>

        <aside className="ticket-aside" aria-label="Ticket properties">
          <section className="card">
            <h2>Details</h2>
            <dl className="meta-grid">
              <div><dt>Requester</dt><dd>{ticket.requesterName}</dd></div>
              <div><dt>Assignee</dt><dd>{ticket.assigneeName ?? 'Unassigned'}</dd></div>
              <div><dt>Created</dt><dd>{formatDateTime(ticket.createdAt)}</dd></div>
              <div><dt>Last updated</dt><dd>{formatDateTime(ticket.updatedAt)}</dd></div>
              <div><dt>First response due</dt><dd>{formatDateTime(ticket.firstResponseDueAt)}</dd></div>
              <div><dt>Resolution due</dt><dd>{formatDateTime(ticket.resolutionDueAt)}</dd></div>
              <div><dt>Paused for customer</dt><dd>{ticket.pausedMinutes} business minutes</dd></div>
            </dl>
          </section>
          <TicketActions key={`workflow-${ticket.updatedAt}`} ticket={ticket} assignees={assignees} onChanged={load} onError={setError} />
          <TicketMetadataActions key={`metadata-${ticket.updatedAt}`} ticket={ticket} categories={categories} articles={articles} onChanged={load} onError={setError} />
        </aside>
      </div>
    </div>
  )
}
