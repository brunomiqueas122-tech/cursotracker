import { useState, type ChangeEvent } from 'react'
import type { Backup } from '../store'

interface Props {
  backup: Backup
  onImported: () => void
  onExport: () => void
}

export function BackupPanel({ backup, onImported, onExport }: Props) {
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null)

  function exportFile() {
    onExport()
    setMessage({ text: 'Backup salvo nos downloads.', error: false })
  }

  async function importFile(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    if (!window.confirm('Isso substitui o progresso salvo neste aparelho pelo do arquivo. Continuar?')) return
    try {
      backup.importJson(await file.text())
      onImported()
      setMessage({ text: 'Backup restaurado.', error: false })
    } catch (err) {
      setMessage({ text: err instanceof Error && err.message !== '' ? err.message : 'Arquivo de backup inválido.', error: true })
    }
  }

  return (
    <section className="backup" aria-label="Backup">
      <h2>Backup</h2>
      <p>Seu progresso fica salvo só neste aparelho. Faça um backup de vez em quando para não perder nada.</p>
      <div className="backup-actions">
        <button type="button" onClick={exportFile}>
          Baixar backup
        </button>
        <label className="file-button" htmlFor="backup-file">
          Restaurar backup
          <input id="backup-file" type="file" accept="application/json,.json" onChange={importFile} />
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
