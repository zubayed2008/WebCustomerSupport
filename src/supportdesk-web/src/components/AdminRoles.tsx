import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiRequest } from '../api/client'
import type { Permission, Role } from '../api/types'
import { useAuth } from '../auth/AuthContext'

export default function AdminRoles() {
  const { can } = useAuth()
  const [roles, setRoles] = useState<Role[]>([])
  const [permissions, setPermissions] = useState<Permission[]>([])
  const [selected, setSelected] = useState<Role | null>(null)
  const [draftPermissions, setDraftPermissions] = useState<string[]>([])
  const [newRoleName, setNewRoleName] = useState('')
  const [error, setError] = useState<string | null>(null)

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
  }

  const createRole = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await apiRequest('/admin/roles', { method: 'POST', body: { name: newRoleName } })
      setNewRoleName('')
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to create the role.')
    }
  }

  const savePermissions = async () => {
    if (!selected) {
      return
    }

    try {
      const updated = await apiRequest<Role>(`/admin/roles/${selected.id}/permissions`, {
        method: 'PUT',
        body: { permissionNames: draftPermissions },
      })
      setSelected(updated)
      setDraftPermissions(updated.permissions)
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to save permissions.')
    }
  }

  const renameRole = async () => {
    if (!selected) {
      return
    }

    const name = window.prompt('New role name', selected.name)
    if (!name) {
      return
    }

    try {
      await apiRequest(`/admin/roles/${selected.id}`, { method: 'PUT', body: { name } })
      setSelected(null)
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to rename the role.')
    }
  }

  const deleteRole = async () => {
    if (!selected || !window.confirm(`Delete role ${selected.name}?`)) {
      return
    }

    try {
      await apiRequest(`/admin/roles/${selected.id}`, { method: 'DELETE' })
      setSelected(null)
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to delete the role.')
    }
  }

  const groups = [...new Set(permissions.map((permission) => permission.group))]

  return (
    <section className="card">
      <h2>Roles and permissions</h2>
      {error ? <div className="alert error">{error}</div> : null}

      <ul className="list">
        {roles.map((role) => (
          <li key={role.id} className="list-item">
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
        <form className="inline-form" onSubmit={createRole}>
          <input value={newRoleName} required maxLength={100} placeholder="New role name" onChange={(event) => setNewRoleName(event.target.value)} />
          <button type="submit" className="button">Create role</button>
        </form>
      ) : null}

      {selected ? (
        <div className="permission-editor">
          <h3>{selected.name} permissions</h3>
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
            <button type="button" className="button primary" onClick={savePermissions}>Save permissions</button>
          ) : null}
          {can('roles.manage') && !selected.isSystem ? (
            <div className="row-actions">
              <button type="button" className="button" onClick={renameRole}>Rename</button>
              <button type="button" className="button danger" onClick={deleteRole}>Delete</button>
            </div>
          ) : null}
        </div>
      ) : null}
    </section>
  )
}
