import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import Layout from './components/Layout'
import AdminPage from './pages/AdminPage'
import DashboardPage from './pages/DashboardPage'
import KnowledgePage from './pages/KnowledgePage'
import LoginPage from './pages/LoginPage'
import NewTicketPage from './pages/NewTicketPage'
import NotificationsPage from './pages/NotificationsPage'
import SlaPage from './pages/SlaPage'
import TicketDetailPage from './pages/TicketDetailPage'
import TicketsPage from './pages/TicketsPage'

function ProtectedRoute() {
  const { user, loading } = useAuth()

  if (loading) {
    return <div className="centered">Loading…</div>
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  return <Layout />
}

function PermissionRoute({ permissions }: { permissions: string[] }) {
  const { can } = useAuth()
  return permissions.some((permission) => can(permission)) ? <Outlet /> : <Navigate to="/tickets" replace />
}

function HomeRoute() {
  const { can } = useAuth()
  return can('reports.view.all') ? <DashboardPage /> : <TicketsPage />
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<HomeRoute />} />
            <Route path="/tickets" element={<TicketsPage />} />
            <Route path="/tickets/new" element={<NewTicketPage />} />
            <Route path="/tickets/:id" element={<TicketDetailPage />} />
            <Route path="/knowledge" element={<KnowledgePage />} />
            <Route path="/notifications" element={<NotificationsPage />} />
            <Route element={<PermissionRoute permissions={['sla.view']} />}>
              <Route path="/sla" element={<SlaPage />} />
            </Route>
            <Route element={<PermissionRoute permissions={['roles.view', 'users.view']} />}>
              <Route path="/admin" element={<AdminPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

