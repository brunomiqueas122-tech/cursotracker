import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { formatHours, goalLabels, levelLabels } from '../format'
import type { CourseSuggestion, Profile, Recommendations } from '../types'

interface Props {
  profile: Profile
  search: (refinement: string | null) => Promise<Recommendations>
  onAdd: (suggestions: CourseSuggestion[]) => Promise<void>
  onEditProfile: () => void
  onBack: () => void
  /** Busca assim que abre (primeira vez, logo depois do perfil) */
  searchOnOpen?: boolean
}

export function DiscoverPanel({ profile, search, onAdd, onEditProfile, onBack, searchOnOpen = false }: Props) {
  const [results, setResults] = useState<Recommendations | null>(null)
  const [remaining, setRemaining] = useState(profile.remainingSearchesToday)
  const [refinement, setRefinement] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [added, setAdded] = useState<Set<string>>(new Set())
  const [adding, setAdding] = useState(false)

  const run = useCallback(
    async (text: string | null) => {
      setLoading(true)
      setError(null)
      try {
        const found = await search(text)
        setResults(found)
        setRemaining(found.remainingSearchesToday)
        setAdded(new Set())
      } catch (e) {
        setError(e instanceof Error ? e.message : 'A busca falhou.')
      } finally {
        setLoading(false)
      }
    },
    [search],
  )

  // StrictMode roda efeitos duas vezes em dev; a ref garante uma única busca (que custa dinheiro)
  const started = useRef(false)
  useEffect(() => {
    if (!searchOnOpen || started.current) return
    started.current = true
    void run(null)
  }, [searchOnOpen, run])

  function submit(e: FormEvent) {
    e.preventDefault()
    void run(refinement.trim() || null)
  }

  async function add(list: CourseSuggestion[]) {
    setAdding(true)
    setError(null)
    try {
      await onAdd(list)
      setAdded((s) => new Set([...s, ...list.map((c) => c.url)]))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Não foi possível adicionar.')
    } finally {
      setAdding(false)
    }
  }

  const pending = results?.courses.filter((c) => !added.has(c.url)) ?? []

  return (
    <main className="page">
      <header className="top">
        <button type="button" className="secondary logout" onClick={onBack}>
          Meu plano
        </button>
        <h1>Descobrir cursos</h1>
        <p>A IA pesquisa na web cursos gratuitos com certificado gratuito para o seu perfil.</p>
      </header>

      <section className="profile-summary" aria-label="Seu perfil">
        <p>
          <strong>{profile.lookingFor}</strong>
          <br />
          {goalLabels[profile.goal]} · {levelLabels[profile.level]} · {profile.hoursPerWeek} h/semana
        </p>
        <button type="button" className="link" onClick={onEditProfile}>
          Editar perfil
        </button>
      </section>

      <form className="refine" onSubmit={submit}>
        <label htmlFor="refinement">
          Quer ajustar a busca? (opcional)
          <input
            id="refinement"
            maxLength={300}
            placeholder="Ex.: só cursos curtos, em português, com foco em AWS"
            value={refinement}
            onChange={(e) => setRefinement(e.target.value)}
          />
        </label>
        <button type="submit" disabled={loading || remaining === 0}>
          {loading ? 'Pesquisando…' : results ? 'Buscar de novo' : 'Buscar cursos'}
        </button>
        <small className="hint">
          {remaining === 0 ? 'Você já usou todas as buscas de hoje. Volte amanhã!' : `${remaining} busca(s) restante(s) hoje.`}
        </small>
      </form>

      {loading && (
        <p className="searching" role="status">
          Pesquisando cursos na web e conferindo se o certificado é mesmo gratuito… isso pode levar até 1 minuto.
        </p>
      )}
      {error && <p className="error" role="alert">{error}</p>}

      {results && !loading && (
        <section className="suggestions" aria-label="Sugestões">
          {results.note && <p className="note">{results.note}</p>}
          {results.courses.length === 0 && <p className="empty">Nenhum curso desta vez. Tente ajustar a busca.</p>}
          {pending.length > 1 && (
            <button type="button" onClick={() => add(pending)} disabled={adding}>
              Adicionar todos ao plano ({pending.length})
            </button>
          )}
          {results.courses.map((c) => (
            <article key={c.url} className="course suggestion">
              <span className="course-meta">
                {c.platform} · {c.area} · {formatHours(c.estimatedHours)} · {c.language}
              </span>
              <h3>
                <a href={c.url} target="_blank" rel="noreferrer">
                  {c.name}
                </a>
              </h3>
              <p>{c.reason}</p>
              <p className="cert-note">🎓 {c.certificate}</p>
              <div className="actions">
                {added.has(c.url) ? (
                  <span className="ok">✔ No seu plano</span>
                ) : (
                  <button type="button" className="secondary" onClick={() => add([c])} disabled={adding}>
                    Adicionar ao plano
                  </button>
                )}
              </div>
            </article>
          ))}
          <p className="hint">
            A IA confere as páginas oficiais, mas pode errar: confirme no site do curso antes de começar.
          </p>
        </section>
      )}
    </main>
  )
}
