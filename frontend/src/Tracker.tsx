import { useCallback, useEffect, useMemo, useState } from 'react'
import { UnauthorizedError } from './api'
import { BackupPanel } from './components/BackupPanel'
import { CourseCard } from './components/CourseCard'
import { DashboardPanel } from './components/DashboardPanel'
import { formatDate, groupByDay, statusLabels } from './format'
import type { CourseStore } from './store'
import { courseStatuses, type Course, type CourseStatus, type DashboardSummary, type UpdateProgressRequest } from './types'

interface Props {
  store: CourseStore
  onUnauthorized?: () => void
  onLogout?: () => void
}

export function Tracker({ store, onUnauthorized, onLogout }: Props) {
  const [courses, setCourses] = useState<Course[]>([])
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [status, setStatus] = useState<CourseStatus | ''>('')
  const [area, setArea] = useState('')

  const fail = useCallback(
    (e: unknown) => {
      if (e instanceof UnauthorizedError && onUnauthorized) onUnauthorized()
      else setError(e instanceof Error ? e.message : 'Erro ao carregar.')
    },
    [onUnauthorized],
  )

  const load = useCallback(
    (signal?: AbortSignal) =>
      Promise.all([store.courses(signal), store.dashboard(signal)]).then(([c, s]) => {
        setCourses(c)
        setSummary(s)
        setError(null)
      }),
    [store],
  )

  useEffect(() => {
    const controller = new AbortController()
    load(controller.signal).catch((e: unknown) => {
      if (!controller.signal.aborted) fail(e)
    })
    return () => controller.abort()
  }, [load, fail])

  const saveProgress = useCallback(
    async (id: number, progress: UpdateProgressRequest) => {
      try {
        const updated = await store.updateProgress(id, progress)
        setCourses((list) => list.map((c) => (c.id === id ? updated : c)))
        setSummary(await store.dashboard())
      } catch (e) {
        if (e instanceof UnauthorizedError) onUnauthorized?.()
        throw e
      }
    },
    [store, onUnauthorized],
  )

  const areas = useMemo(() => [...new Set(courses.map((c) => c.area))].sort(), [courses])
  const visible = courses.filter((c) => (!status || c.status === status) && (!area || c.area === area))

  return (
    <main className="page">
      <header className="top">
        {onLogout && (
          <button type="button" className="secondary logout" onClick={onLogout}>
            Sair
          </button>
        )}
        <h1>CursoTracker</h1>
        <p>Cursos gratuitos com certificado, um dia de cada vez.</p>
      </header>

      {error && <p className="error" role="alert">{error}</p>}
      {summary && <DashboardPanel summary={summary} />}

      <div className="filters">
        <label htmlFor="filter-status">
          Status
          <select id="filter-status" value={status} onChange={(e) => setStatus(e.target.value as CourseStatus | '')}>
            <option value="">Todos</option>
            {courseStatuses.map((s) => (
              <option key={s} value={s}>
                {statusLabels[s]}
              </option>
            ))}
          </select>
        </label>
        <label htmlFor="filter-area">
          Área
          <select id="filter-area" value={area} onChange={(e) => setArea(e.target.value)}>
            <option value="">Todas</option>
            {areas.map((a) => (
              <option key={a} value={a}>
                {a}
              </option>
            ))}
          </select>
        </label>
      </div>

      {groupByDay(visible).map((group) => (
        <section key={group.day} className="day">
          <h2>
            Dia {group.day} <span>{formatDate(group.date)}</span>
          </h2>
          {group.courses.map((course) => (
            <CourseCard key={course.id} course={course} onSave={saveProgress} />
          ))}
        </section>
      ))}

      {!error && summary && visible.length === 0 && <p className="empty">Nenhum curso com esses filtros.</p>}

      {store.backup && <BackupPanel backup={store.backup} onImported={() => load().catch(fail)} />}
    </main>
  )
}
