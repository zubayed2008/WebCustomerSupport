import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiRequest } from '../api/client'
import type { Permission, Role } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import Modal from './Modal'

export default function AdminRoles() {
  const { can } = useAuth()
  const [roles, setRoles] = useState<Role[]>([])
  const [permissions, setPermissions] = useState<Permission[]>([])
  const [selected, setSelected] = useState<Role | null>(null)
  const [draftPermissions, setDraftPermissions] = useState<string[]>([])
  const [newRoleName, setNewRoleName] = useState('')
  const [renameValue, setRenameValue] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const [roleResult, permissionResult] = await Promise.all([
        apiRequest<Role[]>('/admin/roles'),
        apiRequest<Permission[]>('/admin/permissions'),
      ])
      setRoles(roleResult)
      setPermissions(permissionResult)
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load roles.')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const select = (role: Role) => {
    setSelected(role)
    setDraftPermissions(role.permissions)
    setRenameValue(role.name)
    setNotice(null)
  }

  const createRole = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await apiRequest('/admin/roles', { method: 'POST', body: { name: newRoleName } })
      setNotice(`Role ${newRoleName} created.`)
      setNewRoleName('')
      setError(null)
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to create the role.')
    }
  }

  const savePermissions = async () => {
    if (!selected) return
    try {
      const updated = await apiRequest<Role>(`/admin/roles/${selected.id}/permissions`, {
        method: 'PUT',
        body: { permissionNames: draftPermissions },
      })
      setSelected(updated)
      setDraftPermissions(updated.permissions)
      setNotice(`Permissions saved for ${updated.name}.`)
      setError(null)
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to save permissions.')
    }
  }

  const renameRole = async (event: FormEvent) => {
    event.preventDefault()
    if (!selected || !renameValue.trim()) return
    try {
      await apiRequest(`/admin/roles/${selected.id}`, { method: 'PUT', body: { name: renameValue.trim() } })
      setNotice(`Role renamed to ${renameValue.trim()}.`)
      setSelected(null)
      setError(null)
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to rename the role.')
    }
  }

  const deleteRole = async () => {
    if (!selected) return
    try {
      await apiRequest(`/admin/roles/${selected.id}`, { method: 'DELETE' })
      setNotice(`Role ${selected.name} deleted.`)
      setSelected(null)
      setConfirmDelete(false)
      setError(null)
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to delete the role.')
      setConfirmDelete(false)
    }
  }

  const groups = [...new Set(permissions.map((permission) => permission.group))]

  return (
    <>
      {error ? <div className="alert error" role="alert">{error}</div> : null}
      {notice ? <div className="alert success" role="status">{notice}</div> : null}
      <div className="role-layout">
        <section className="card">
          <h2>Roles</h2>
          <ul className="list">
            {roles.map((role) => (
              <li key={role.id} className={selected?.id === role.id ? 'list-item selected' : 'list-item'}>
                <div>
                  <button type="button" className="link-button" onClick={() => select(role)}>{role.name}</button>
                  <div className="muted small">
                    {role.isSystem ? 'System role' : 'Custom role'} · {role.permissions.length} permissions
                  </div>
                </div>
              </li>
            ))}
          </ul>
          {can('roles.manage') ? (
            <form className="inline-form" onSubmit={createRole} style={{ marginTop: '1rem' }}>
              <input value={newRoleName} required maxLength={100} placeholder="New role name" aria-label="New role name" onChange={(event) => setNewRoleName(event.target.value)} />
              <button type="submit" className="button">Create role</button>
            </form>
          ) : null}
        </section>
        {selected ? (
          <section className="card permission-editor" style={{ marginTop: 0 }}>
            <div className="card-header">
              <div>
                <h2>{selected.name}</h2>
                <p className="card-description">{selected.isSystem ? 'System role' : 'Custom role'} · {draftPermissions.length} of {permissions.length} permissions granted</p>
              </div>
              {can('roles.manage') && !selected.isSystem ? (
                <button type="button" className="button danger small" onClick={() => setConfirmDelete(true)}>Delete role</button>
              ) : null}
            </div>

            {groups.map((group) => (
              <fieldset key={group}>
                <legend>{group}</legend>
                {permissions.filter((permission) => permission.group === group).map((permission) => (
                  <label key={permission.id} className="checkbox inline" title={permission.description}>
                    <input
                      type="checkbox"
                      disabled={!can('permissions.manage')}
                      checked={draftPermissions.includes(permission.name)}
                      onChange={(event) => setDraftPermissions((current) => event.target.checked
                        ? [...current, permission.name]
                        : current.filter((value) => value !== permission.name))}
                    />
                    {permission.name}
                  </label>
                ))}
              </fieldset>
            ))}

            {can('permissions.manage') ? (
              <div className="form-actions">
                <button type="button" className="button primary" onClick={() => void savePermissions()}>Save permissions</button>
              </div>
            ) : null}

            {can('roles.manage') && !selected.isSystem ? (
              <form className="inline-form" onSubmit={renameRole} style={{ marginTop: '1rem' }}>
                <input value={renameValue} required maxLength={100} aria-label="Role name" onChange={(event) => setRenameValue(event.target.value)} />
                <button type="submit" className="button">Rename role</button>
              </form>
            ) : null}

            {confirmDelete ? (
              <Modal title={`Delete ${selected.name}?`} description="This action cannot be undone. Users assigned to this role will lose its permissions." onClose={() => setConfirmDelete(false)}>
                <div className="form-actions">
                  <button type="button" className="button danger" onClick={() => void deleteRole()}>Delete role</button>
                  <button type="button" className="button" onClick={() => setConfirmDelete(false)}>Cancel</button>
                </div>
              </Modal>
            ) : null}
          </section>
        ) : (
          <section className="card">
            <h2>Role permissions</h2>
            <p className="empty-state">Select a role to review and edit its permissions.</p>
          </section>
        )}
      </div>
    </>
  )
}
