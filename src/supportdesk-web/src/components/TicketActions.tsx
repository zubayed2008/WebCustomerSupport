import { useState } from 'react'
import { apiRequest } from '../api/client'
import type { Assignee, TicketDetail, TicketPriority, TicketStatus } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { formatLabel } from '../utils/format'

export const allowedTransitions: Record<TicketStatus, TicketStatus[]> = {
  New: ['Open', 'PendingInternal', 'Resolved'],
  Open: ['PendingCustomer', 'PendingInternal', 'Resolved', 'Closed'],
  PendingCustomer: ['Open', 'PendingInternal', 'Resolved', 'Closed'],
  PendingInternal: ['Open', 'PendingCustomer', 'Resolved', 'Closed'],
  Resolved: ['Open', 'Closed'],
  Closed: ['Open'],
}

const priorities: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent']

interface Props {
  ticket: TicketDetail
  assignees: Assignee[]
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

export default function TicketActions({ ticket, assignees, onChanged, onError }: Props) {
  const { user, can } = useAuth()
  const [status, setStatus] = useState<TicketStatus>(ticket.status)
  const [priority, setPriority] = useState<TicketPriority>(ticket.priority)
  const [assigneeId, setAssigneeId] = useState(ticket.assigneeId ?? '')
  const [busy, setBusy] = useState(false)

  const run = async (action: () => Promise<unknown>) => {
    setBusy(true)
    try {
      await action()
      await onChanged()
    } catch (exception) {
      onError(exception instanceof Error ? exception.message : 'The update failed.')
    } finally {
      setBusy(false)
    }
  }

  const allowed = allowedTransitions[ticket.status] ?? []
  const canChangeStatus = can('tickets.update-status')
  const canReopen = !canChangeStatus && can('tickets.reply') && (ticket.status === 'Resolved' || ticket.status === 'Closed')
  const canChangePriority = can('tickets.update-priority')
  const canAssign = can('tickets.assign')

  if (!canChangeStatus && !canReopen && !canChangePriority && !canAssign) {
    return null
  }

  return (
    <section className="card">
      <h2>Workflow</h2>

      {canChangeStatus ? (
        <div className="form-row">
          <label>
            Status
            <select value={status} onChange={(event) => setStatus(event.target.value as TicketStatus)}>
              <option value={ticket.status}>{formatLabel(ticket.status)} (current)</option>
              {allowed.map((value) => <option key={value} value={value}>{formatLabel(value)}</option>)}
            </select>
          </label>
          <button
            type="button"
            className="button primary"
            disabled={busy || status === ticket.status}
            onClick={() => run(() => apiRequest(`/tickets/${ticket.id}/transitions`, { method: 'POST', body: { status } }))}
          >
            Apply status
          </button>
        </div>
      ) : canReopen ? (
        <button
          type="button"
          className="button"
          disabled={busy}
          onClick={() => run(() => apiRequest(`/tickets/${ticket.id}/transitions`, { method: 'POST', body: { status: 'Open' } }))}
        >
          Reopen ticket
        </button>
      ) : null}

      {canChangePriority ? (
        <div className="form-row">
          <label>
            Priority
            <select value={priority} onChange={(event) => setPriority(event.target.value as TicketPriority)}>
              {priorities.map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </label>
          <button
            type="button"
            className="button"
            disabled={busy || priority === ticket.priority}
            onClick={() => run(() => apiRequest(`/tickets/${ticket.id}`, { method: 'PUT', body: { priority } }))}
          >
            Update priority
          </button>
        </div>
      ) : null}

      {canAssign ? (
        <div className="form-row">
          <label>
            Assignee
            <select value={assigneeId} onChange={(event) => setAssigneeId(event.target.value)}>
              <option value="">Unassigned</option>
              {assignees.map((assignee) => (
                <option key={assignee.id} value={assignee.id}>{assignee.displayName}</option>
              ))}
            </select>
          </label>
          <button
            type="button"
            className="button"
            disabled={busy}
            onClick={() => run(() => apiRequest(`/tickets/${ticket.id}/assignment`, { method: 'POST', body: { assigneeId: assigneeId || null } }))}
          >
            Assign
          </button>
          {user && ticket.assigneeId !== user.id ? (
            <button
              type="button"
              className="button"
              disabled={busy}
              onClick={() => run(() => apiRequest(`/tickets/${ticket.id}/assignment`, { method: 'POST', body: { assigneeId: user.id } }))}
            >
              Claim
            </button>
          ) : null}
        </div>
      ) : null}
    </section>
  )
}
