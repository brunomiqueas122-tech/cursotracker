import { useState } from 'react'
import { formatHours, statusLabels } from '../format'
import { linkedInCertificationUrl, linkedInShareUrl } from '../linkedin'
import { courseStatuses, type Course, type CourseDetails, type CourseStatus, type UpdateProgressRequest } from '../types'
import { CourseForm } from './CourseForm'

interface Props {
  course: Course
  onSave: (id: number, progress: UpdateProgressRequest) => Promise<void>
  onEdit?: (id: number, details: CourseDetails) => Promise<void>
  onDelete?: (id: number) => Promise<void>
  areas?: string[]
  overdue?: boolean
}

function toDraft(course: Course): UpdateProgressRequest {
  return {
    hoursDone: course.hoursDone,
    status: course.status,
    certificateReceived: course.certificateReceived,
    certificateUrl: course.certificateUrl,
    onLinkedIn: course.onLinkedIn,
    notes: course.notes,
  }
}

export function CourseCard({ course, onSave, onEdit, onDelete, areas = [], overdue = false }: Props) {
  const [draft, setDraft] = useState(() => toDraft(course))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [openedLinkedIn, setOpenedLinkedIn] = useState(false)
  const [editing, setEditing] = useState(false)

  // O servidor pode ajustar o que foi enviado (ex.: status), então o rascunho segue a versão salva
  const [synced, setSynced] = useState(course)
  if (course !== synced) {
    setSynced(course)
    setDraft(toDraft(course))
  }

  const original = toDraft(course)
  const dirty = (Object.keys(draft) as (keyof UpdateProgressRequest)[]).some((k) => draft[k] !== original[k])
  const id = `course-${course.id}`

  const update = (patch: Partial<UpdateProgressRequest>) => setDraft((d) => ({ ...d, ...patch }))

  async function save(next: UpdateProgressRequest) {
    setSaving(true)
    setError(null)
    try {
      await onSave(course.id, next)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao salvar.')
    } finally {
      setSaving(false)
    }
  }

  const complete = () =>
    save({ ...draft, status: 'Completed', hoursDone: Math.max(draft.hoursDone, course.estimatedHours) })

  async function remove() {
    if (!onDelete || !window.confirm(`Excluir "${course.name}"? O progresso dele será perdido.`)) return
    setSaving(true)
    setError(null)
    try {
      await onDelete(course.id)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao excluir.')
      setSaving(false)
    }
  }

  if (editing && onEdit) {
    return (
      <article id={id} className="course editing">
        <h3>Editar curso</h3>
        <CourseForm
          initial={{
            date: course.date,
            platform: course.platform,
            name: course.name,
            area: course.area,
            url: course.url,
            estimatedHours: course.estimatedHours,
          }}
          areas={areas}
          submitLabel="Salvar alterações"
          onSubmit={async (details) => {
            await onEdit(course.id, details)
            setEditing(false)
          }}
          onCancel={() => setEditing(false)}
        />
      </article>
    )
  }

  return (
    <article id={id} className={`course status-${course.status}${overdue ? ' overdue' : ''}`}>
      <header className="course-head">
        <div>
          <span className="course-meta">
            {course.platform} · {course.area} · {formatHours(course.estimatedHours)}
          </span>
          <h3>
            <a href={course.url} target="_blank" rel="noreferrer">
              {course.name}
            </a>
          </h3>
        </div>
        <div className="pills">
          {overdue && <span className="pill pill-overdue">Atrasado</span>}
          <span className={`pill pill-${course.status}`}>{statusLabels[course.status]}</span>
        </div>
      </header>

      <div className="fields">
        <label htmlFor={`${id}-status`}>
          Status
          <select
            id={`${id}-status`}
            value={draft.status}
            onChange={(e) => update({ status: e.target.value as CourseStatus })}
          >
            {courseStatuses.map((s) => (
              <option key={s} value={s}>
                {statusLabels[s]}
              </option>
            ))}
          </select>
        </label>

        <label htmlFor={`${id}-hours`}>
          Horas feitas
          <input
            id={`${id}-hours`}
            type="number"
            min={0}
            step={0.5}
            value={draft.hoursDone}
            onChange={(e) => update({ hoursDone: e.target.valueAsNumber || 0 })}
          />
        </label>

        <label className="check" htmlFor={`${id}-cert`}>
          <input
            id={`${id}-cert`}
            type="checkbox"
            checked={draft.certificateReceived}
            onChange={(e) => update({ certificateReceived: e.target.checked })}
          />
          Certificado recebido
        </label>

        <label className="check" htmlFor={`${id}-linkedin`}>
          <input
            id={`${id}-linkedin`}
            type="checkbox"
            checked={draft.onLinkedIn}
            onChange={(e) => update({ onLinkedIn: e.target.checked })}
          />
          No LinkedIn
        </label>

        {draft.certificateReceived && (
          <label className="wide" htmlFor={`${id}-certurl`}>
            Link do certificado
            <input
              id={`${id}-certurl`}
              type="url"
              placeholder="https://"
              value={draft.certificateUrl ?? ''}
              onChange={(e) => update({ certificateUrl: e.target.value || null })}
            />
          </label>
        )}

        <label className="wide" htmlFor={`${id}-notes`}>
          Anotações
          <textarea
            id={`${id}-notes`}
            rows={2}
            value={draft.notes ?? ''}
            onChange={(e) => update({ notes: e.target.value || null })}
          />
        </label>
      </div>

      {error && <p className="error" role="alert">{error}</p>}

      {course.status === 'Completed' && !course.onLinkedIn && (
        <div className="linkedin">
          <p>
            Curso concluído! Adicione o certificado ao seu perfil.
            {!course.certificateUrl && ' Dica: salve antes o link do certificado para ele ir junto.'}
          </p>
          <div className="linkedin-actions">
            <a
              className="linkedin-button"
              href={linkedInCertificationUrl(course)}
              target="_blank"
              rel="noreferrer"
              onClick={() => setOpenedLinkedIn(true)}
            >
              Adicionar ao LinkedIn
            </a>
            {openedLinkedIn && (
              <button type="button" className="secondary" onClick={() => save({ ...draft, onLinkedIn: true })} disabled={saving}>
                Já adicionei
              </button>
            )}
          </div>
        </div>
      )}

      <footer className="actions">
        {(onEdit || onDelete) && (
          <div className="manage">
            {onEdit && (
              <button type="button" className="link" onClick={() => setEditing(true)} disabled={saving}>
                Editar
              </button>
            )}
            {onDelete && (
              <button type="button" className="link danger" onClick={remove} disabled={saving}>
                Excluir
              </button>
            )}
          </div>
        )}
        {course.certificateUrl && (
          <a href={course.certificateUrl} target="_blank" rel="noreferrer">
            Ver certificado
          </a>
        )}
        {course.status === 'Completed' && (
          <a href={linkedInShareUrl(course)} target="_blank" rel="noreferrer">
            Compartilhar no LinkedIn
          </a>
        )}
        {course.status !== 'Completed' && (
          <button type="button" className="secondary" onClick={complete} disabled={saving}>
            Marcar como concluído
          </button>
        )}
        <button type="button" onClick={() => save(draft)} disabled={!dirty || saving}>
          {saving ? 'Salvando…' : 'Salvar'}
        </button>
      </footer>
    </article>
  )
}
