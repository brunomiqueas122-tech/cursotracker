import { useId, useState, type FormEvent } from 'react'
import type { CourseDetails } from '../types'

interface Props {
  initial: CourseDetails
  areas: string[]
  /** Plano grátis: sem escolher a data (o cronograma é do Premium) */
  hideDate?: boolean
  submitLabel: string
  onSubmit: (details: CourseDetails) => Promise<void>
  onCancel: () => void
}

export function CourseForm({ initial, areas, hideDate = false, submitLabel, onSubmit, onCancel }: Props) {
  const [form, setForm] = useState(initial)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const id = useId()

  const update = (patch: Partial<CourseDetails>) => setForm((f) => ({ ...f, ...patch }))

  async function submit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      await onSubmit(form)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
      setSaving(false)
    }
  }

  return (
    <form className="course-form" onSubmit={submit}>
      <div className="fields">
        <label className="wide" htmlFor={`${id}-name`}>
          Nome do curso
          <input id={`${id}-name`} required value={form.name} onChange={(e) => update({ name: e.target.value })} />
        </label>
        {!hideDate && (
          <label htmlFor={`${id}-date`}>
            Data
            <input id={`${id}-date`} type="date" required value={form.date} onChange={(e) => update({ date: e.target.value })} />
          </label>
        )}
        <label htmlFor={`${id}-hours`}>
          Horas estimadas
          <input
            id={`${id}-hours`}
            type="number"
            min={0.5}
            step={0.5}
            required
            value={form.estimatedHours}
            onChange={(e) => update({ estimatedHours: e.target.valueAsNumber || 0 })}
          />
        </label>
        <label htmlFor={`${id}-platform`}>
          Plataforma
          <input id={`${id}-platform`} required value={form.platform} onChange={(e) => update({ platform: e.target.value })} />
        </label>
        <label htmlFor={`${id}-area`}>
          Área
          <input
            id={`${id}-area`}
            required
            list={`${id}-areas`}
            value={form.area}
            onChange={(e) => update({ area: e.target.value })}
          />
          <datalist id={`${id}-areas`}>
            {areas.map((a) => (
              <option key={a} value={a} />
            ))}
          </datalist>
        </label>
        <label className="wide" htmlFor={`${id}-url`}>
          Link do curso
          <input
            id={`${id}-url`}
            type="url"
            required
            placeholder="https://"
            value={form.url}
            onChange={(e) => update({ url: e.target.value })}
          />
        </label>
      </div>
      {error && <p className="error" role="alert">{error}</p>}
      <div className="actions">
        <button type="button" className="secondary" onClick={onCancel} disabled={saving}>
          Cancelar
        </button>
        <button type="submit" disabled={saving}>
          {saving ? 'Salvando…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
