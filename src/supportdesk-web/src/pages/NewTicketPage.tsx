import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiRequest } from '../api/client'
import type { Category, TicketDetail, TicketPriority } from '../api/types'

const priorities: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent']

export default function NewTicketPage() {
  const navigate = useNavigate()
  const [categories, setCategories] = useState<Category[]>([])
  const [subject, setSubject] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<TicketPriority>('Normal')
  const [categoryId, setCategoryId] = useState('')
  const [tags, setTags] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    apiRequest<Category[]>('/lookups/categories')
      .then(setCategories)
      .catch(() => setCategories([]))
  }, [])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const ticket = await apiRequest<TicketDetail>('/tickets', {
        method: 'POST',
        body: {
          subject,
          description,
          priority,
          categoryId: categoryId || null,
          tags: tags.split(',').map((value) => value.trim()).filter(Boolean),
        },
      })
      navigate(`/tickets/${ticket.id}`)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to create the ticket.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Create a ticket</h1>
          <p className="page-subtitle">Describe the issue so the support team can respond quickly.</p>
        </div>
      </div>
      {error ? <div className="alert error" role="alert">{error}</div> : null}
      <form className="card form narrow" onSubmit={submit}>
        <label>
          Subject
          <input value={subject} required maxLength={200} onChange={(event) => setSubject(event.target.value)} />
        </label>
        <label>
          Description
          <textarea value={description} required maxLength={20000} rows={8} onChange={(event) => setDescription(event.target.value)} />
        </label>
        <div className="form-row">
          <label>
            Priority
            <select value={priority} onChange={(event) => setPriority(event.target.value as TicketPriority)}>
              {priorities.map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </label>
          <label>
            Category
            <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
              <option value="">No category</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>{category.name}</option>
              ))}
            </select>
          </label>
        </div>
        <label>
          Tags (comma separated)
          <input value={tags} maxLength={200} onChange={(event) => setTags(event.target.value)} placeholder="login, urgent" />
        </label>
        <div className="form-actions">
          <button type="submit" className="button primary" disabled={busy}>{busy ? 'Creating…' : 'Create ticket'}</button>
          <button type="button" className="button" onClick={() => navigate(-1)}>Cancel</button>
        </div>
      </form>
    </div>
  )
}
