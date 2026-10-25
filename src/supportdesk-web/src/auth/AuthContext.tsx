import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { apiRequest, clearCsrfToken } from '../api/client'
import type { CurrentUser } from '../api/types'

interface AuthContextValue {
  user: CurrentUser | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  refresh: () => Promise<void>
  can: (permission: string) => boolean
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [loading, setLoading] = useState(true)

  const refresh = useCallback(async () => {
    try {
      const current = await apiRequest<CurrentUser>('/auth/me')
      setUser(current)
    } catch {
      setUser(null)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  useEffect(() => {
    const handler = () => setUser(null)
    window.addEventListener('supportdesk:unauthenticated', handler)
    return () => window.removeEventListener('supportdesk:unauthenticated', handler)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const current = await apiRequest<CurrentUser>('/auth/login', { method: 'POST', body: { email, password } })
    clearCsrfToken()
    setUser(current)
  }, [])

  const logout = useCallback(async () => {
    try {
      await apiRequest('/auth/logout', { method: 'POST' })
    } finally {
      clearCsrfToken()
      setUser(null)
    }
  }, [])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    loading,
    login,
    logout,
    refresh,
    can: (permission: string) => user?.permissions.includes(permission) ?? false,
  }), [user, loading, login, logout, refresh])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider')
  }
  return context
}
