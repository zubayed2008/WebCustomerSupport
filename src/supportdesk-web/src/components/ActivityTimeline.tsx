import type { TicketActivity } from '../api/types'

export default function ActivityTimeline({ activities }: { activities: TicketActivity[] }) {
  return (
    <section className="card">
      <h2>Activity</h2>
      <ol className="timeline">
        {activities.map((activity) => (
          <li key={activity.id}>
            <strong>{activity.type}</strong>
            {activity.field ? (
              <span> — {activity.field}: {activity.oldValue ?? 'none'} → {activity.newValue ?? 'none'}</span>
            ) : null}
            <div className="muted small">
              {activity.actorName ?? 'System'} · {new Date(activity.createdAt).toLocaleString()}
            </div>
          </li>
        ))}
        {activities.length === 0 ? <li className="muted">No activity recorded yet.</li> : null}
      </ol>
    </section>
  )
}
