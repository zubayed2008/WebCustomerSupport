import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiRequest } from '../api/client'
import type { PagedResult, Role, UserSummary } from '../api/types'
import { useAuth } from '../auth/AuthContext'

export default function AdminUsers() {
  const { can } = useAuth()
  const [users, setUsers] = useState<UserSummary[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [search, setSearch] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [newRoles, setNewRoles] = useState<string[]>(['Customer'])

  const load = useCallback(async () => {
    try {
      const params = new URLSearchParams({ pageSize: '100' })
      if (search.trim()) params.set('search', search.trim())
      const [userResult, roleResult] = await Promise.all([
        apiRequest<PagedResult<UserSummary>>(`/admin/users?${params.toString()}`),
        apiRequest<Role[]>('/admin/roles'),
      ])
      setUsers(userResult.items)
      setRoles(roleResult)
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load users.')
    }
  }, [search])

  useEffect(() => {
    void load()
  }, [load])

  const create = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await apiRequest('/admin/users', { method: 'POST', body: { email, displayName, password, roles: newRoles } })
      setEmail('')
      setDisplayName('')
      setPassword('')
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to create the user.')
    }
  }

  const save = async (user: UserSummary, changes: { displayName?: string; isActive?: boolean; roles?: string[] }) => {
    try {
      await apiRequest(`/admin/users/${user.id}`, {
        method: 'PUT',
        body: {
          displayName: changes.displayName ?? user.displayName,
          isActive: changes.isActive ?? user.isActive,
          roles: changes.roles ?? user.roles,
        },
      })
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to update the user.')
    }
  }

  const editRoles = async (user: UserSummary) => {
    const available = roles.map((role) => role.name).join(', ')
    const value = window.prompt(`Roles for ${user.email} (comma separated). Available: ${available}`, user.roles.join(', '))
    if (value === null) {
      return
    }

    await save(user, { roles: value.split(',').map((item) => item.trim()).filter(Boolean) })
  }

  const resetPassword = async (user: UserSummary) => {
    const next = window.prompt(`New password for ${user.email} (minimum 12 characters):`)
    if (!next) {
      return
    }

    try {
      await apiRequest(`/admin/users/${user.id}/password`, { method: 'POST', body: { newPassword: next } })
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to reset the password.')
    }
  }

  return (
    <section className="card">
      <div className="page-header">
        <h2>Users</h2>
        <form className="inline-form" onSubmit={(event) => { event.preventDefault(); void load() }}>
          <input value={search} maxLength={100} placeholder="Search users" onChange={(event) => setSearch(event.target.value)} />
          <button type="submit" className="button">Search</button>
        </form>
      </div>

      {error ? <div className="alert error">{error}</div> : null}

      {can('users.manage') ? (
        <form className="form" onSubmit={create}>
          <h3>Create user</h3>
          <div className="form-row">
            <label>Email<input type="email" value={email} required maxLength={256} onChange={(event) => setEmail(event.target.value)} /></label>
            <label>Display name<input value={displayName} required maxLength={150} onChange={(event) => setDisplayName(event.target.value)} /></label>
          </div>
          <label>Temporary password<input type="password" value={password} required minLength={12} maxLength={200} onChange={(event) => setPassword(event.target.value)} /></label>
          <fieldset>
            <legend>Roles</legend>
            {roles.map((role) => (
              <label key={role.id} className="checkbox inline">
                <input
                  type="checkbox"
                  checked={newRoles.includes(role.name)}
                  onChange={(event) => setNewRoles((current) => event.target.checked
                    ? [...current, role.name]
                    : current.filter((value) => value !== role.name))}
                />
                {role.name}
              </label>
            ))}
          </fieldset>
          <button type="submit" className="button primary">Create user</button>
        </form>
      ) : null}

      <table className="table">
        <thead>
          <tr><th>Email</th><th>Name</th><th>Active</th><th>Roles</th><th>Created</th><th /></tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.id}>
              <td>{user.email}</td>
              <td>{user.displayName}</td>
              <td>{user.isActive ? 'Yes' : 'No'}</td>
              <td>{user.roles.join(', ')}</td>
              <td>{new Date(user.createdAt).toLocaleDateString()}</td>
              <td className="row-actions">
                {can('users.manage') ? (
                  <>
                    <button type="button" className="button small" onClick={() => save(user, { isActive: !user.isActive })}>
                      {user.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                    <button type="button" className="button small" onClick={() => editRoles(user)}>Set roles</button>
                    <button type="button" className="button small" onClick={() => resetPassword(user)}>Reset password</button>
                  </>
                ) : null}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <p className="muted small">Role changes are applied per user through the API; use the role editor to change granted permissions.</p>
    </section>
  )
}
