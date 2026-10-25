import { useState, type FormEvent } from 'react'

interface Props {
  onLogin: (password: string) => Promise<void>
}

export function LoginScreen({ onLogin }: Props) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await onLogin(password)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível entrar.')
      setBusy(false)
    }
  }

  return (
    <main className="login">
      <form onSubmit={submit} className="login-card">
        <h1>CursoTracker</h1>
        <p>Digite sua senha para ver seus cursos.</p>
        <label htmlFor="password">
          Senha
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            autoFocus
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        {error && <p className="error" role="alert">{error}</p>}
        <button type="submit" disabled={!password || busy}>
          {busy ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
    </main>
  )
}
