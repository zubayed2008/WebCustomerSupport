import { useEffect, useState } from 'react'
import { apiRequest } from '../api/client'
import { formatDateTime } from '../utils/format'

interface AuditLog {
  id: string
  actorId: string | null
  action: string
  entityType: string
  entityId: string
  detailsJson: string | null
  ipAddress: string | null
  createdAt: string
}

export default function AdminAudit() {
  const [items, setItems] = useState<AuditLog[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    apiRequest<AuditLog[]>('/admin/audit?pageSize=100')
      .then(setItems)
      .catch((exception: Error) => setError(exception.message))
  }, [])

  return (
    <section className="card">
      <h2>Administrative audit</h2>
      {error ? <div className="alert error" role="alert">{error}</div> : null}
      <div className="table-wrap">
      <table className="table">
        <thead>
          <tr><th>When</th><th>Action</th><th>Entity</th><th>Actor</th><th>Details</th></tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>{formatDateTime(item.createdAt)}</td>
              <td>{item.action}</td>
              <td>{item.entityType}:{item.entityId.slice(0, 8)}</td>
              <td>{item.actorId?.slice(0, 8) ?? 'system'}</td>
              <td className="muted small">{item.detailsJson ?? '—'}</td>
            </tr>
          ))}
          {items.length === 0 ? <tr><td colSpan={5}><div className="empty-state">No audit records.</div></td></tr> : null}
        </tbody>
      </table>
      </div>
    </section>
  )
}
