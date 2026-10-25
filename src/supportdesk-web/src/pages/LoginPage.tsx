import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export default function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (user) {
    return <Navigate to="/" replace />
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(email, password)
      navigate('/', { replace: true })
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Sign in failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-page">
      <form className="card login-card" onSubmit={submit}>
        <div className="login-brand">
          <span className="brand-mark">SD</span>
          <h1>Support Desk</h1>
        </div>
        <p className="muted">Sign in with the account created by your administrator.</p>
        {error ? <div className="alert error" role="alert">{error}</div> : null}
        <label>
          Email
          <input
            type="email"
            value={email}
            autoComplete="username"
            required
            maxLength={256}
            placeholder="you@example.com"
            onChange={(event) => setEmail(event.target.value)}
          />
        </label>
        <label>
          Password
          <input
            type="password"
            value={password}
            autoComplete="current-password"
            required
            maxLength={200}
            placeholder="Your password"
            onChange={(event) => setPassword(event.target.value)}
          />
        </label>
        <button type="submit" className="button primary" disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>
        <p className="muted small">
          Self-registration is disabled. If your account is locked, an administrator can reset your password to restore access.
        </p>
      </form>
    </div>
  )
}
