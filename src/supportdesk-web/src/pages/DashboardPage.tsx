import { useEffect, useState } from 'react'
import { apiRequest } from '../api/client'
import type { ReportOverview } from '../api/types'

export default function DashboardPage() {
  const [overview, setOverview] = useState<ReportOverview | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    apiRequest<ReportOverview>('/reports/overview')
      .then(setOverview)
      .catch((exception: Error) => setError(exception.message))
      .finally(() => setLoading(false))
  }, [])

  if (loading) {
    return <p>Loading dashboard…</p>
  }

  if (!overview) {
    return <div className="alert error">{error ?? 'Unable to load the dashboard.'}</div>
  }

  const maxTrend = Math.max(1, ...overview.volumeTrend.map((point) => Math.max(point.created, point.resolved)))

  return (
    <div>
      <h1>Operational dashboard</h1>
      <div className="metric-grid">
        <div className="metric card"><span>Open tickets</span><strong>{overview.openTickets}</strong></div>
        <div className="metric card"><span>Unassigned</span><strong>{overview.unassignedTickets}</strong></div>
        <div className="metric card warning"><span>Due soon</span><strong>{overview.dueSoonTickets}</strong></div>
        <div className="metric card danger"><span>Overdue</span><strong>{overview.overdueTickets}</strong></div>
        <div className="metric card"><span>Resolved (30 days)</span><strong>{overview.resolvedInPeriod}</strong></div>
        <div className="metric card"><span>Avg first response</span><strong>{overview.averageFirstResponseMinutes} min</strong></div>
        <div className="metric card"><span>Avg resolution</span><strong>{overview.averageResolutionMinutes} min</strong></div>
      </div>

      <div className="two-column">
        <section className="card">
          <h2>By status</h2>
          <table className="table">
            <tbody>
              {overview.byStatus.map((metric) => (
                <tr key={metric.label}><td>{metric.label}</td><td>{metric.count}</td></tr>
              ))}
            </tbody>
          </table>
        </section>
        <section className="card">
          <h2>By priority</h2>
          <table className="table">
            <tbody>
              {overview.byPriority.map((metric) => (
                <tr key={metric.label}><td>{metric.label}</td><td>{metric.count}</td></tr>
              ))}
            </tbody>
          </table>
        </section>
      </div>

      <section className="card">
        <h2>Agent workload</h2>
        <table className="table">
          <thead>
            <tr><th>Agent</th><th>Active</th><th>Due soon</th><th>Overdue</th></tr>
          </thead>
          <tbody>
            {overview.agentWorkload.map((agent) => (
              <tr key={agent.agentId ?? 'unassigned'}>
                <td>{agent.agentName}</td>
                <td>{agent.activeTickets}</td>
                <td>{agent.dueSoon}</td>
                <td>{agent.overdue}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="card">
        <h2>14-day volume</h2>
        <div className="chart">
          {overview.volumeTrend.map((point) => (
            <div key={point.date} className="chart-column">
              <div className="chart-bars">
                <div className="bar created" style={{ height: `${(point.created / maxTrend) * 100}%` }} title={`${point.created} created`} />
                <div className="bar resolved" style={{ height: `${(point.resolved / maxTrend) * 100}%` }} title={`${point.resolved} resolved`} />
              </div>
              <span className="muted small">{point.date.slice(5)}</span>
            </div>
          ))}
        </div>
        <p className="muted small"><span className="legend created" /> created <span className="legend resolved" /> resolved</p>
      </section>
    </div>
  )
}
