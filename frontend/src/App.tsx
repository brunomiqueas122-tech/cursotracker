import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from './api'
import { CourseCard } from './components/CourseCard'
import { DashboardPanel } from './components/DashboardPanel'
import { formatDate, groupByDay, statusLabels } from './format'
import { courseStatuses, type Course, type CourseStatus, type DashboardSummary, type UpdateProgressRequest } from './types'

export default function App() {
  const [courses, setCourses] = useState<Course[]>([])
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [status, setStatus] = useState<CourseStatus | ''>('')
  const [area, setArea] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    Promise.all([api.courses(controller.signal), api.dashboard(controller.signal)])
      .then(([c, s]) => {
        setCourses(c)
        setSummary(s)
      })
      .catch((e: unknown) => {
        if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Erro ao carregar.')
      })
    return () => controller.abort()
  }, [])

  const saveProgress = useCallback(async (id: number, progress: UpdateProgressRequest) => {
    const updated = await api.updateProgress(id, progress)
    setCourses((list) => list.map((c) => (c.id === id ? updated : c)))
    setSummary(await api.dashboard())
  }, [])

  const areas = useMemo(() => [...new Set(courses.map((c) => c.area))].sort(), [courses])
  const visible = courses.filter((c) => (!status || c.status === status) && (!area || c.area === area))

  return (
    <main className="page">
      <header className="top">
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
    </main>
  )
}
