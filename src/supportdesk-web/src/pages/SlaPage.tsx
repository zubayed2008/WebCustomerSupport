import { useCallback, useEffect, useState } from 'react'
import { apiRequest } from '../api/client'
import SlaCalendars from '../components/SlaCalendars'
import SlaPolicies from '../components/SlaPolicies'
import type { BusinessCalendar, SlaPolicy } from '../api/types'

export default function SlaPage() {
  const [policies, setPolicies] = useState<SlaPolicy[]>([])
  const [calendars, setCalendars] = useState<BusinessCalendar[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const [policyResult, calendarResult] = await Promise.all([
        apiRequest<SlaPolicy[]>('/sla/policies'),
        apiRequest<BusinessCalendar[]>('/sla/calendars'),
      ])
      setPolicies(policyResult)
      setCalendars(calendarResult)
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load service levels.')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <div>
      <h1>Service levels</h1>
      {error ? <div className="alert error">{error}</div> : null}
      <SlaPolicies policies={policies} calendars={calendars} onChanged={load} onError={setError} />
      <SlaCalendars calendars={calendars} onChanged={load} onError={setError} />
    </div>
  )
}
