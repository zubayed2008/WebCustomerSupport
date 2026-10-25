import { useEffect, useState, type FormEvent } from 'react'
import { apiRequest } from '../api/client'
import type { Permission } from '../api/types'
import { useAuth } from '../auth/AuthContext'

export default function AdminPermissions() {
  const { can } = useAuth()
  const [permissions, setPermissions] = useState<Permission[]>([])
  const [name, setName] = useState('')
  const [group, setGroup] = useState('Custom')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    try {
      setPermissions(await apiRequest<Permission[]>('/admin/permissions'))
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load permissions.')
    }
  }

  useEffect(() => {
    void load()
  }, [])

  const create = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await apiRequest('/admin/permissions', { method: 'POST', body: { name, group, description } })
      setName('')
      setDescription('')
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to create the permission.')
    }
  }

  const groups = [...new Set(permissions.map((permission) => permission.group))]

  return (
    <section className="card">
      <h2>Permissions</h2>
      <p className="muted small">System permissions are enforced by endpoints. Custom permissions can be granted to roles immediately.</p>
      {error ? <div className="alert error">{error}</div> : null}
      {groups.map((groupName) => (
        <div key={groupName}>
          <h3>{groupName}</h3>
          <ul className="list">
            {permissions.filter((permission) => permission.group === groupName).map((permission) => (
              <li key={permission.id} className="list-item">
                <div>
                  <code>{permission.name}</code>
                  <div className="muted small">{permission.description}</div>
                </div>
                <span className={`pill ${permission.isSystem ? 'neutral' : 'success'}`}>{permission.isSystem ? 'System' : 'Custom'}</span>
              </li>
            ))}
          </ul>
        </div>
      ))}

      {can('permissions.manage') ? (
        <form className="form" onSubmit={create}>
          <h3>Create a custom permission</h3>
          <p className="muted small">
            Custom permissions can be granted to roles immediately. Endpoint enforcement still requires application code to check the new name.
          </p>
          <div className="form-grid">
            <label>Name<input value={name} required maxLength={100} placeholder="reports.export" onChange={(event) => setName(event.target.value)} /></label>
            <label>Group<input value={group} required maxLength={100} onChange={(event) => setGroup(event.target.value)} /></label>
          </div>
          <label>Description<input value={description} required maxLength={500} onChange={(event) => setDescription(event.target.value)} /></label>
          <div className="form-actions">
            <button type="submit" className="button primary">Create permission</button>
          </div>
        </form>
      ) : null}
    </section>
  )
}
