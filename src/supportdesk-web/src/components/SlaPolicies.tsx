import { useEffect, useState, type FormEvent } from 'react'
import { apiRequest } from '../api/client'
import type { BusinessCalendar, SlaPolicy, TicketPriority } from '../api/types'
import { useAuth } from '../auth/AuthContext'

const priorities: TicketPriority[] = ['Urgent', 'High', 'Normal', 'Low']

interface Props {
  policies: SlaPolicy[]
  calendars: BusinessCalendar[]
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

export default function SlaPolicies({ policies, calendars, onChanged, onError }: Props) {
  const { can } = useAuth()
  const [name, setName] = useState('')
  const [priority, setPriority] = useState<TicketPriority>('Normal')
  const [firstResponse, setFirstResponse] = useState(240)
  const [resolution, setResolution] = useState(1440)
  const [dueSoon, setDueSoon] = useState(60)
  const [calendarId, setCalendarId] = useState(calendars[0]?.id ?? '')

  useEffect(() => {
    if (!calendarId && calendars.length > 0) {
      setCalendarId(calendars[0].id)
    }
  }, [calendarId, calendars])

  const save = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await apiRequest('/sla/policies', {
        method: 'POST',
        body: {
          name,
          priority,
          firstResponseMinutes: firstResponse,
          resolutionMinutes: resolution,
          dueSoonMinutes: dueSoon,
          businessCalendarId: calendarId,
          isActive: true,
        },
      })
      setName('')
      await onChanged()
    } catch (exception) {
      onError(exception instanceof Error ? exception.message : 'Unable to save the policy.')
    }
  }

  return (
    <section className="card">
      <h2>SLA policies</h2>
      <div className="table-wrap">
      <table className="table">
        <thead>
          <tr><th>Name</th><th>Priority</th><th>First response</th><th>Resolution</th><th>Due soon</th><th>Calendar</th><th>Active</th></tr>
        </thead>
        <tbody>
          {policies.map((policy) => (
            <tr key={policy.id}>
              <td>{policy.name}</td>
              <td>{policy.priority}</td>
              <td>{policy.firstResponseMinutes} min</td>
              <td>{policy.resolutionMinutes} min</td>
              <td>{policy.dueSoonMinutes} min</td>
              <td>{policy.businessCalendarName}</td>
              <td>{policy.isActive ? 'Yes' : 'No'}</td>
            </tr>
          ))}
        </tbody>
      </table>
      </div>

      {can('sla.manage') ? (
        <form className="form" onSubmit={save}>
          <h3>Add or replace a priority policy</h3>
          <div className="form-row">
            <label>Name<input value={name} required maxLength={150} onChange={(event) => setName(event.target.value)} /></label>
            <label>
              Priority
              <select value={priority} onChange={(event) => setPriority(event.target.value as TicketPriority)}>
                {priorities.map((value) => <option key={value} value={value}>{value}</option>)}
              </select>
            </label>
          </div>
          <div className="form-row">
            <label>First response (minutes)<input type="number" min={1} max={100000} value={firstResponse} onChange={(event) => setFirstResponse(Number(event.target.value))} /></label>
            <label>Resolution (minutes)<input type="number" min={1} max={100000} value={resolution} onChange={(event) => setResolution(Number(event.target.value))} /></label>
            <label>Due soon (minutes)<input type="number" min={1} max={10000} value={dueSoon} onChange={(event) => setDueSoon(Number(event.target.value))} /></label>
          </div>
          <label>
            Business calendar
            <select value={calendarId} required onChange={(event) => setCalendarId(event.target.value)}>
              <option value="">Select a calendar</option>
              {calendars.map((calendar) => <option key={calendar.id} value={calendar.id}>{calendar.name}</option>)}
            </select>
          </label>
          <button type="submit" className="button primary">Save policy</button>
        </form>
      ) : null}
    </section>
  )
}
