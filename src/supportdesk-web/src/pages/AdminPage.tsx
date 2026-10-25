import { useState } from 'react'
import AdminAudit from '../components/AdminAudit'
import AdminPermissions from '../components/AdminPermissions'
import AdminRoles from '../components/AdminRoles'
import AdminUsers from '../components/AdminUsers'
import { useAuth } from '../auth/AuthContext'

type Tab = 'users' | 'roles' | 'permissions' | 'audit'

export default function AdminPage() {
  const { can } = useAuth()
  const [tab, setTab] = useState<Tab>('users')

  const tabs: { id: Tab; label: string; permission?: string }[] = [
    { id: 'users', label: 'Users', permission: 'users.view' },
    { id: 'roles', label: 'Roles', permission: 'roles.view' },
    { id: 'permissions', label: 'Permissions', permission: 'roles.view' },
    { id: 'audit', label: 'Audit', permission: 'audit.view' },
  ]

  const visible = tabs.filter((item) => !item.permission || can(item.permission))

  return (
    <div>
      <h1>Administration</h1>
      <div className="tabs" role="tablist">
        {visible.map((item) => (
          <button
            key={item.id}
            type="button"
            role="tab"
            aria-selected={tab === item.id}
            className={tab === item.id ? 'tab active' : 'tab'}
            onClick={() => setTab(item.id)}
          >
            {item.label}
          </button>
        ))}
      </div>
      {tab === 'users' && can('users.view') ? <AdminUsers /> : null}
      {tab === 'roles' && can('roles.view') ? <AdminRoles /> : null}
      {tab === 'permissions' && can('roles.view') ? <AdminPermissions /> : null}
      {tab === 'audit' && can('audit.view') ? <AdminAudit /> : null}
    </div>
  )
}
