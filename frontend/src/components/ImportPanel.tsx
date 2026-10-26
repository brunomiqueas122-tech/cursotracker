import { useState, type ChangeEvent } from 'react'

interface Props {
  importCourses: (backupJson: string) => Promise<{ imported: number }>
  onImported: () => void
}

/** Traz o progresso salvo na versão antiga do app (arquivo "Baixar backup"). */
export function ImportPanel({ importCourses, onImported }: Props) {
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null)

  async function importFile(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    try {
      const { imported } = await importCourses(await file.text())
      setMessage({ text: `${imported} curso(s) importado(s) para sua conta.`, error: false })
      onImported()
    } catch (err) {
      setMessage({ text: err instanceof Error ? err.message : 'Arquivo inválido.', error: true })
    }
  }

  return (
    <section className="backup" aria-label="Importar">
      <h2>Trazer progresso da versão antiga</h2>
      <p>Se você usava o app antes das contas, escolha o arquivo do “Baixar backup” para trazer seus cursos.</p>
      <div className="backup-actions">
        <label className="file-button" htmlFor="import-file">
          Escolher arquivo de backup
          <input id="import-file" type="file" accept="application/json,.json" onChange={importFile} />
        </label>
      </div>
      {message && (
        <p className={message.error ? 'error' : 'ok'} role={message.error ? 'alert' : 'status'}>
          {message.text}
        </p>
      )}
    </section>
  )
}
