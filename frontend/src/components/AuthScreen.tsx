import { useState, type FormEvent } from 'react'

interface Props {
  onLogin: (email: string, password: string) => Promise<void>
  onRegister: (email: string, password: string, inviteCode: string) => Promise<void>
}

export function AuthScreen({ onLogin, onRegister }: Props) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [inviteCode, setInviteCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const registering = mode === 'register'

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      if (registering) await onRegister(email, password, inviteCode)
      else await onLogin(email, password)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível entrar.')
      setBusy(false)
    }
  }

  function switchMode() {
    setMode(registering ? 'login' : 'register')
    setError(null)
  }

  return (
    <main className="login">
      <form onSubmit={submit} className="login-card">
        <h1>CursoTracker</h1>
        <p>
          {registering
            ? 'Crie sua conta. A IA vai buscar cursos gratuitos com certificado para o seu perfil.'
            : 'Entre para ver seu plano de cursos.'}
        </p>
        <label htmlFor="email">
          E-mail
          <input id="email" type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label htmlFor="password">
          Senha
          <input
            id="password"
            type="password"
            autoComplete={registering ? 'new-password' : 'current-password'}
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            aria-describedby={registering ? 'password-hint' : undefined}
          />
        </label>
        {registering && (
          <small id="password-hint" className="hint">
            Mínimo de 8 caracteres, com letra minúscula e número.
          </small>
        )}
        {registering && (
          <label htmlFor="invite">
            Código de convite
            <input id="invite" required value={inviteCode} onChange={(e) => setInviteCode(e.target.value)} />
          </label>
        )}
        {error && <p className="error" role="alert">{error}</p>}
        <button type="submit" disabled={busy}>
          {busy ? 'Aguarde…' : registering ? 'Criar conta' : 'Entrar'}
        </button>
        <button type="button" className="link" onClick={switchMode}>
          {registering ? 'Já tenho conta' : 'Criar uma conta'}
        </button>
      </form>
    </main>
  )
}
