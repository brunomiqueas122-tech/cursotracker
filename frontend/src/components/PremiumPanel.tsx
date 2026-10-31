import { useEffect, useState, type FormEvent } from 'react'
import { formatDateTime } from '../format'
import type { PremiumCode, PremiumStatus } from '../types'

interface Props {
  status: () => Promise<PremiumStatus>
  redeem: (code: string) => Promise<PremiumStatus>
  /** Só para administradores: gerar e listar códigos */
  admin?: {
    list: () => Promise<PremiumCode[]>
    create: (days: number | null) => Promise<{ code: string; days: number | null }>
  }
  onActivated: () => void
  onBack: () => void
}

const premiumBenefits = [
  'Todos os cursos encontrados pela IA, sem limite de 3 por mês',
  'Assuntos ilimitados no seu perfil',
  'Cronograma: cursos distribuídos por dia, "Para hoje", atrasados e reagendar',
  'Lembretes no calendário do celular',
  'Adicionar o certificado ao LinkedIn na hora e post pronto',
  'Exportar certificados para o portfólio',
]

const durations: { label: string; days: number | null }[] = [
  { label: '30 dias', days: 30 },
  { label: '1 ano', days: 365 },
  { label: 'Para sempre', days: null },
]

const durationLabel = (days: number | null) => durations.find((d) => d.days === days)?.label ?? `${days} dias`

function formatUntil(s: PremiumStatus): string {
  if (s.forever) return 'Premium para sempre.'
  return s.premiumUntil ? `Premium ativo até ${new Date(s.premiumUntil).toLocaleDateString('pt-BR')}.` : ''
}

export function PremiumPanel({ status, redeem, admin, onActivated, onBack }: Props) {
  const [current, setCurrent] = useState<PremiumStatus | null>(null)
  const [code, setCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [activated, setActivated] = useState(false)

  useEffect(() => {
    status()
      .then(setCurrent)
      .catch(() => undefined) // sem o status a tela ainda permite ativar
  }, [status])

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      setCurrent(await redeem(code))
      setCode('')
      setActivated(true)
      onActivated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível ativar.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="page">
      <header className="top">
        <button type="button" className="secondary logout" onClick={onBack}>
          Voltar
        </button>
        <h1>CursoTracker Premium</h1>
        <p>{current?.isPremium ? formatUntil(current) : 'Tudo o que o app oferece, sem limites.'}</p>
      </header>

      {activated && (
        <p className="premium-welcome" role="status">
          Premium ativado! Todos os recursos já estão liberados.
        </p>
      )}

      <section className="premium-card" aria-label="O que o Premium libera">
        <h2>O que você ganha</h2>
        <ul>
          {premiumBenefits.map((b) => (
            <li key={b}>{b}</li>
          ))}
        </ul>
        <p className="hint">No plano grátis: 3 cursos liberados por mês e até 2 assuntos no perfil.</p>
      </section>

      <section className="premium-card" aria-label="Como assinar">
        <h2>{current?.isPremium ? 'Renovar ou estender' : 'Como assinar'}</h2>
        <p>
          {current?.contact
            ? <>Fale comigo para assinar: <strong>{current.contact}</strong>. Você recebe um código de ativação.</>
            : 'Fale com quem te convidou para o app e peça um código de ativação.'}
        </p>
        <form className="redeem" onSubmit={submit}>
          <label htmlFor="premium-code">
            Código de ativação
            <input
              id="premium-code"
              required
              autoComplete="off"
              placeholder="XXXX-XXXX-XXXX"
              value={code}
              onChange={(e) => setCode(e.target.value)}
            />
          </label>
          <button type="submit" disabled={busy || code.trim() === ''}>
            {busy ? 'Ativando…' : 'Ativar Premium'}
          </button>
        </form>
        {error && <p className="error" role="alert">{error}</p>}
      </section>

      {admin && <AdminCodes {...admin} />}
    </main>
  )
}

function AdminCodes({ list, create }: NonNullable<Props['admin']>) {
  const [codes, setCodes] = useState<PremiumCode[]>([])
  const [days, setDays] = useState<number | null>(30)
  const [created, setCreated] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    list()
      .then(setCodes)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : 'Não foi possível carregar os códigos.'))
  }, [list])

  async function generate() {
    setError(null)
    try {
      const result = await create(days)
      setCreated(result.code)
      setCodes(await list())
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Não foi possível gerar o código.')
    }
  }

  return (
    <section className="premium-card" aria-label="Códigos premium">
      <h2>Gerar código (administrador)</h2>
      <div className="redeem">
        <label htmlFor="premium-days">
          Duração
          <select id="premium-days" value={days ?? ''} onChange={(e) => setDays(e.target.value ? Number(e.target.value) : null)}>
            {durations.map((d) => (
              <option key={d.label} value={d.days ?? ''}>
                {d.label}
              </option>
            ))}
          </select>
        </label>
        <button type="button" onClick={generate}>
          Gerar código
        </button>
      </div>
      {created && (
        <p className="premium-welcome">
          Código gerado: <strong className="premium-code">{created}</strong>
          <br />
          Copie agora: ele não aparece de novo. Vale para uma pessoa só.
        </p>
      )}
      {error && <p className="error" role="alert">{error}</p>}
      {codes.length > 0 && (
        <ul className="premium-codes">
          {codes.map((c) => (
            <li key={c.id}>
              <code>{c.hint}-…</code> · {durationLabel(c.days)} ·{' '}
              {c.redeemedByEmail && c.redeemedAt ? `usado por ${c.redeemedByEmail} em ${formatDateTime(c.redeemedAt)}` : 'não usado'}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
