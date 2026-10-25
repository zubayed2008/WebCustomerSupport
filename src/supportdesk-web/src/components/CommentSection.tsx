import { useState, type FormEvent } from 'react'
import { apiRequest, downloadAttachment } from '../api/client'
import type { TicketDetail } from '../api/types'
import { useAuth } from '../auth/AuthContext'

interface Props {
  ticket: TicketDetail
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

export default function CommentSection({ ticket, onChanged, onError }: Props) {
  const { can } = useAuth()
  const [body, setBody] = useState('')
  const [isInternal, setIsInternal] = useState(false)
  const [busy, setBusy] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    try {
      await apiRequest(`/tickets/${ticket.id}/comments`, { method: 'POST', body: { body, isInternal } })
      setBody('')
      setIsInternal(false)
      await onChanged()
    } catch (exception) {
      onError(exception instanceof Error ? exception.message : 'The comment could not be added.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="card">
      <h2>Conversation</h2>
      <ul className="list">
        {ticket.comments.map((comment) => (
          <li key={comment.id} className={comment.visibility === 'Internal' ? 'list-item column internal' : 'list-item column'}>
            <div className="row-actions wrap">
              <strong>{comment.authorName}</strong>
              <span className={`pill ${comment.visibility === 'Internal' ? 'warning' : 'status-open'}`}>
                {comment.visibility === 'Internal' ? 'Internal note' : 'Public reply'}
              </span>
              {comment.isFirstResponse ? <span className="pill">First response</span> : null}
              <span className="muted small">{new Date(comment.createdAt).toLocaleString()}</span>
            </div>
            <p className="preserve-lines">{comment.body}</p>
            {comment.attachments.length > 0 ? (
              <ul>
                {comment.attachments.map((attachment) => (
                  <li key={attachment.id}>
                    <button
                      type="button"
                      className="link-button"
                      onClick={() => downloadAttachment(ticket.id, attachment.id, attachment.fileName).catch((error: Error) => onError(error.message))}
                    >
                      {attachment.fileName}
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}
          </li>
        ))}
        {ticket.comments.length === 0 ? <li className="muted">No comments yet.</li> : null}
      </ul>

      {can('tickets.reply') ? (
        <form className="form" onSubmit={submit}>
          <label>
            Add a comment
            <textarea value={body} required maxLength={20000} rows={5} onChange={(event) => setBody(event.target.value)} />
          </label>
          {can('tickets.note') ? (
            <label className="checkbox">
              <input type="checkbox" checked={isInternal} onChange={(event) => setIsInternal(event.target.checked)} />
              Internal note (hidden from the customer)
            </label>
          ) : null}
          <button type="submit" className="button primary" disabled={busy}>
            {busy ? 'Sending…' : isInternal ? 'Add internal note' : 'Send reply'}
          </button>
        </form>
      ) : null}
    </section>
  )
}
