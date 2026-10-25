import { useCallback, useEffect, useState } from 'react'
import { api } from './api'
import { LoginScreen } from './components/LoginScreen'
import { createLocalStore } from './localStore'
import { httpStore } from './store'
import { Tracker } from './Tracker'

type Session = 'checking' | 'loggedOut' | 'loggedIn' | 'offline'

// Build "pages": dados no aparelho, sem servidor e sem login
const localStore = import.meta.env.VITE_STORAGE === 'local' ? createLocalStore() : null

export default function App() {
  return localStore ? <Tracker store={localStore} /> : <ServerApp />
}

function ServerApp() {
  const [session, setSession] = useState<Session>('checking')

  useEffect(() => {
    const controller = new AbortController()
    api
      .isLoggedIn(controller.signal)
      .then((ok) => setSession(ok ? 'loggedIn' : 'loggedOut'))
      .catch(() => {
        if (!controller.signal.aborted) setSession('offline')
      })
    return () => controller.abort()
  }, [])

  const login = useCallback(async (password: string) => {
    await api.login(password)
    setSession('loggedIn')
  }, [])

  const logout = useCallback(async () => {
    await api.logout().catch(() => undefined)
    setSession('loggedOut')
  }, [])

  const expire = useCallback(() => setSession('loggedOut'), [])

  switch (session) {
    case 'checking':
      return <p className="empty">Carregando…</p>
    case 'offline':
      return <p className="error page">Não foi possível falar com o servidor. Confira sua internet e recarregue a página.</p>
    case 'loggedOut':
      return <LoginScreen onLogin={login} />
    case 'loggedIn':
      return <Tracker store={httpStore} onUnauthorized={expire} onLogout={logout} />
  }
}
