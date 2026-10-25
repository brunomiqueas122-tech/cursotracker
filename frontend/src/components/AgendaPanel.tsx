import { useState } from 'react'
import { formatDate, formatHours } from '../format'
import type { Course } from '../types'

interface Props {
  today: Course[]
  overdue: Course[]
  onMoveToToday: (course: Course) => Promise<void>
}

export function AgendaPanel({ today, overdue, onMoveToToday }: Props) {
  const [error, setError] = useState<string | null>(null)

  if (today.length === 0 && overdue.length === 0) return null

  async function move(course: Course) {
    setError(null)
    try {
      await onMoveToToday(course)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Não foi possível reagendar.')
    }
  }

  return (
    <section className="agenda" aria-label="Agenda">
      {today.length > 0 && (
        <div>
          <h2>Para hoje</h2>
          <ul>
            {today.map((c) => (
              <li key={c.id}>
                <a href={`#course-${c.id}`}>{c.name}</a>
                <span>{formatHours(c.estimatedHours)}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
      {overdue.length > 0 && (
        <div>
          <h2 className="overdue-title">Atrasados</h2>
          <ul>
            {overdue.map((c) => (
              <li key={c.id}>
                <a href={`#course-${c.id}`}>{c.name}</a>
                <span>{formatDate(c.date)}</span>
                <button type="button" className="secondary small" onClick={() => move(c)}>
                  Mover para hoje
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
      {error && <p className="error" role="alert">{error}</p>}
    </section>
  )
}
