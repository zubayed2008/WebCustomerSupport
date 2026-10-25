import { useState } from 'react'
import { downloadAttachment, uploadAttachment } from '../api/client'
import type { TicketDetail } from '../api/types'

interface Props {
  ticket: TicketDetail
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

export default function TicketAttachments({ ticket, onChanged, onError }: Props) {
  const [busy, setBusy] = useState(false)

  const upload = async (file: File) => {
    setBusy(true)
    try {
      await uploadAttachment(ticket.id, file)
      await onChanged()
    } catch (exception) {
      onError(exception instanceof Error ? exception.message : 'The upload failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="card">
      <h2>Attachments</h2>
      <ul>
        {ticket.attachments.map((attachment) => (
          <li key={attachment.id}>
            <button
              type="button"
              className="link-button"
              onClick={() => downloadAttachment(ticket.id, attachment.id, attachment.fileName).catch((exception: Error) => onError(exception.message))}
            >
              {attachment.fileName}
            </button>
            <span className="muted small"> ({Math.round(attachment.sizeBytes / 1024)} KB)</span>
          </li>
        ))}
        {ticket.attachments.length === 0 ? <li className="muted">No attachments.</li> : null}
      </ul>
      <label>
        Upload a file (PDF, PNG, JPEG, WEBP, TXT, or CSV, max 10 MB)
        <input
          type="file"
          accept=".pdf,.png,.jpg,.jpeg,.webp,.txt,.csv"
          disabled={busy}
          onChange={(event) => {
            const file = event.target.files?.[0]
            if (file) {
              void upload(file)
            }
            event.target.value = ''
          }}
        />
      </label>
    </section>
  )
}
