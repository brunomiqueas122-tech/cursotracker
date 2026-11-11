import { useState } from 'react'

interface Props {
  count: number
  onImport: () => Promise<void>
  onDismiss: () => void
}

/** Pergunta, já logado, se traz os cursos que vieram do app antigo do celular. */
export function LegacyImportBanner({ count, onImport, onDismiss }: Props) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function importNow() {
    setBusy(true)
    setError(null)
    try {
      await onImport()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Não foi possível importar.')
      setBusy(false)
    }
  }

  return (
    <section className="premium-card" aria-label="Cursos do app antigo">
      <h2>Cursos do app antigo</h2>
      <p>
        Encontramos {count} curso(s) da versão antiga do app no seu celular. Quer trazer para a sua conta, com o
        progresso de cada um? Cursos que já estão no seu plano não são duplicados.
      </p>
      {error && <p className="error" role="alert">{error}</p>}
      <div className="backup-actions">
        <button type="button" onClick={importNow} disabled={busy}>
          {busy ? 'Importando…' : `Importar ${count} curso(s)`}
        </button>
        <button type="button" className="secondary" onClick={onDismiss} disabled={busy}>
          Agora não
        </button>
      </div>
    </section>
  )
}
