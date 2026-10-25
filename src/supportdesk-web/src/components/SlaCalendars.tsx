import { useState, type FormEvent } from 'react'
import { apiRequest } from '../api/client'
import type { BusinessCalendar } from '../api/types'
import { useAuth } from '../auth/AuthContext'

const weekDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']

interface Props {
  calendars: BusinessCalendar[]
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

export default function SlaCalendars({ calendars, onChanged, onError }: Props) {
  const { can } = useAuth()
  const [name, setName] = useState('')
  const [timeZoneId, setTimeZoneId] = useState('UTC')
  const [start, setStart] = useState('09:00')
  const [end, setEnd] = useState('17:00')
  const [selectedDays, setSelectedDays] = useState<string[]>(['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'])

  const save = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await apiRequest('/sla/calendars', {
        method: 'POST',
        body: {
          name,
          timeZoneId,
          isActive: true,
          workingDays: selectedDays.map((day) => ({ day, intervals: [{ start, end }] })),
          holidays: [],
        },
      })
      setName('')
      await onChanged()
    } catch (exception) {
      onError(exception instanceof Error ? exception.message : 'Unable to save the calendar.')
    }
  }

  return (
    <section className="card">
      <h2>Business calendars</h2>
      <ul className="list">
        {calendars.map((calendar) => (
          <li key={calendar.id} className="list-item column">
            <strong>{calendar.name}</strong>
            <span className="muted small">{calendar.timeZoneId} · {calendar.workingDays.map((day) => day.day.slice(0, 3)).join(', ')}</span>
            <span className="muted small">Holidays: {calendar.holidays.length}</span>
          </li>
        ))}
        {calendars.length === 0 ? <li className="empty-state">No business calendars configured.</li> : null}
      </ul>

      {can('sla.manage') ? (
        <form className="form" onSubmit={save}>
          <h3>Add calendar</h3>
          <div className="form-row">
            <label>Name<input value={name} required maxLength={150} onChange={(event) => setName(event.target.value)} /></label>
            <label>Time zone<input value={timeZoneId} required maxLength={100} onChange={(event) => setTimeZoneId(event.target.value)} placeholder="UTC or America/New_York" /></label>
          </div>
          <div className="form-row">
            <label>Start<input value={start} required pattern="\d{2}:\d{2}" onChange={(event) => setStart(event.target.value)} /></label>
            <label>End<input value={end} required pattern="\d{2}:\d{2}" onChange={(event) => setEnd(event.target.value)} /></label>
          </div>
          <fieldset>
            <legend>Working days</legend>
            {weekDays.map((day) => (
              <label key={day} className="checkbox inline">
                <input
                  type="checkbox"
                  checked={selectedDays.includes(day)}
                  onChange={(event) => setSelectedDays((current) => event.target.checked
                    ? [...current, day]
                    : current.filter((value) => value !== day))}
                />
                {day}
              </label>
            ))}
          </fieldset>
          <button type="submit" className="button primary">Save calendar</button>
        </form>
      ) : null}
    </section>
  )
}
