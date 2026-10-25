import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiRequest } from '../api/client'
import type { PagedResult, Role, UserSummary } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import Modal from './Modal'
import { formatDate } from '../utils/format'

export default function AdminUsers() {
  const { can } = useAuth()
  const [users, setUsers] = useState<UserSummary[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [search, setSearch] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [newRoles, setNewRoles] = useState<string[]>(['Customer'])
  const [roleTarget, setRoleTarget] = useState<UserSummary | null>(null)
  const [roleDraft, setRoleDraft] = useState<string[]>([])
  const [passwordTarget, setPasswordTarget] = useState<UserSummary | null>(null)
  const [nextPassword, setNextPassword] = useState('')
  const [busy, setBusy] = useState(false)

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
    setBusy(true)
    try {
      await apiRequest('/admin/users', { method: 'POST', body: { email, displayName, password, roles: newRoles } })
      setEmail('')
      setDisplayName('')
      setPassword('')
      setNotice(`User ${email} created.`)
      setError(null)
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to create the user.')
    } finally {
      setBusy(false)
    }
  }

  const save = async (user: UserSummary, changes: { isActive?: boolean; roles?: string[] }) => {
    try {
      await apiRequest(`/admin/users/${user.id}`, {
        method: 'PUT',
        body: { displayName: user.displayName, isActive: changes.isActive ?? user.isActive, roles: changes.roles ?? user.roles },
      })
      setError(null)
      await load()
      return true
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to update the user.')
      return false
    }
  }

  const toggleActive = async (user: UserSummary) => {
    setNotice(null)
    if (await save(user, { isActive: !user.isActive })) {
      setNotice(user.isActive
        ? `${user.email} deactivated.`
        : `${user.email} reactivated. Any sign-in lockout and failed-attempt counters were cleared.`)
    }
  }

  const submitRoles = async (event: FormEvent) => {
    event.preventDefault()
    if (!roleTarget) return
    setBusy(true)
    const saved = await save(roleTarget, { roles: roleDraft })
    setBusy(false)
    if (saved) {
      setNotice(`Roles updated for ${roleTarget.email}.`)
      setRoleTarget(null)
    }
  }

  const submitPassword = async (event: FormEvent) => {
    event.preventDefault()
    if (!passwordTarget) return
    setBusy(true)
    try {
      await apiRequest(`/admin/users/${passwordTarget.id}/password`, { method: 'POST', body: { newPassword: nextPassword } })
      setNotice(`Password reset for ${passwordTarget.email}. The account is unlocked and can sign in with the new password.`)
      setError(null)
      setPasswordTarget(null)
      setNextPassword('')
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to reset the password.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
    <section className="card">
      <div className="card-header">
        <div>
          <h2>Users</h2>
          <p className="card-description">Manage access, roles, and account recovery.</p>
        </div>
        <form className="inline-form" onSubmit={(event) => { event.preventDefault(); void load() }}>
          <input value={search} maxLength={100} placeholder="Search users" aria-label="Search users" onChange={(event) => setSearch(event.target.value)} />
          <button type="submit" className="button">Search</button>
        </form>
      </div>

      {error ? <div className="alert error" role="alert">{error}</div> : null}
      {notice ? <div className="alert success" role="status">{notice}</div> : null}

      {can('users.manage') ? (
        <form className="form" onSubmit={create}>
          <h3>Create user</h3>
          <div className="form-grid">
            <label>Email<input type="email" value={email} required maxLength={256} onChange={(event) => setEmail(event.target.value)} /></label>
            <label>Display name<input value={displayName} required maxLength={150} onChange={(event) => setDisplayName(event.target.value)} /></label>
            <label>Temporary password<input type="password" value={password} required minLength={12} maxLength={200} autoComplete="new-password" onChange={(event) => setPassword(event.target.value)} /></label>
          </div>
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
          <div className="form-actions">
            <button type="submit" className="button primary" disabled={busy}>Create user</button>
          </div>
        </form>
      ) : null}
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>Email</th><th>Name</th><th>Status</th><th>Roles</th><th>Created</th><th><span className="muted">Actions</span></th></tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td className="cell-title">{user.email}</td>
                <td>{user.displayName}</td>
                <td><span className={`pill ${user.isActive ? 'success' : 'neutral'}`}>{user.isActive ? 'Active' : 'Inactive'}</span></td>
                <td>
                  <div className="row-actions wrap">
                    {user.roles.map((role) => <span key={role} className="pill neutral">{role}</span>)}
                  </div>
                </td>
                <td>{formatDate(user.createdAt)}</td>
                <td className="cell-actions">
                  {can('users.manage') ? (
                    <div className="row-actions">
                      <button type="button" className="button small" onClick={() => void toggleActive(user)}>
                        {user.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                      <button type="button" className="button small" onClick={() => { setRoleTarget(user); setRoleDraft(user.roles); setNotice(null) }}>Set roles</button>
                      <button type="button" className="button small" onClick={() => { setPasswordTarget(user); setNextPassword(''); setNotice(null) }}>Reset password</button>
                    </div>
                  ) : null}
                </td>
              </tr>
            ))}
            {users.length === 0 ? <tr><td colSpan={6}><div className="empty-state">No users found.</div></td></tr> : null}
          </tbody>
        </table>
      </div>
      <p className="muted small">Resetting a password or reactivating an account also clears sign-in lockouts.</p>
    </section>

    {roleTarget ? (
      <Modal title={`Roles for ${roleTarget.displayName}`} description={roleTarget.email} onClose={() => setRoleTarget(null)}>
        <form className="form" onSubmit={submitRoles}>
          <fieldset>
            <legend>Assigned roles</legend>
            {roles.map((role) => (
              <label key={role.id} className="checkbox">
                <input
                  type="checkbox"
                  checked={roleDraft.includes(role.name)}
                  onChange={(event) => setRoleDraft((current) => event.target.checked
                    ? [...current, role.name]
                    : current.filter((value) => value !== role.name))}
                />
                {role.name}
              </label>
            ))}
          </fieldset>
          <div className="form-actions">
            <button type="submit" className="button primary" disabled={busy}>Save roles</button>
            <button type="button" className="button" onClick={() => setRoleTarget(null)}>Cancel</button>
          </div>
        </form>
      </Modal>
    ) : null}

    {passwordTarget ? (
      <Modal title="Reset password" description={`${passwordTarget.email} · unlocks the account and clears failed sign-in attempts.`} onClose={() => setPasswordTarget(null)}>
        <form className="form" onSubmit={submitPassword}>
          <label>
            New password
            <input type="password" value={nextPassword} required minLength={12} maxLength={200} autoComplete="new-password" onChange={(event) => setNextPassword(event.target.value)} />
          </label>
          <p className="muted small">Minimum 12 characters. Share the temporary password through a secure channel.</p>
          <div className="form-actions">
            <button type="submit" className="button primary" disabled={busy}>{busy ? 'Resetting…' : 'Reset password'}</button>
            <button type="button" className="button" onClick={() => setPasswordTarget(null)}>Cancel</button>
          </div>
        </form>
      </Modal>
    ) : null}
    </>
  )
}
