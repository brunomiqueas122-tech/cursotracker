import { useCallback, useEffect, useMemo, useState } from 'react'
import { UnauthorizedError } from './api'
import { AgendaPanel } from './components/AgendaPanel'
import { CourseCard } from './components/CourseCard'
import { CourseForm } from './components/CourseForm'
import { DashboardPanel } from './components/DashboardPanel'
import { ImportPanel } from './components/ImportPanel'
import { agenda, isOverdue, toIsoDate } from './domain/agenda'
import { buildCalendar } from './domain/calendar'
import { portfolioCertificates } from './domain/certificates'
import { downloadFile } from './download'
import { loadTimer, saveTimer, type RunningTimer } from './timer'
import { formatDate, groupByDay, statusLabels } from './format'
import type { CourseStore } from './store'
import {
  courseStatuses,
  type Course,
  type CourseDetails,
  type CourseStatus,
  type DashboardSummary,
  type UpdateProgressRequest,
} from './types'

interface Props {
  store: CourseStore
  onUnauthorized?: () => void
  onLogout?: () => void
  onDiscover?: () => void
  onEditProfile?: () => void
  importCourses?: (backupJson: string) => Promise<{ imported: number }>
  /** Data de hoje (AAAA-MM-DD); injetável para testes */
  today?: string
}

export function Tracker({
  store,
  onUnauthorized,
  onLogout,
  onDiscover,
  onEditProfile,
  importCourses,
  today: fixedToday,
}: Props) {
  const [currentDay, setCurrentDay] = useState(() => toIsoDate(new Date()))
  const today = fixedToday ?? currentDay

  const [courses, setCourses] = useState<Course[]>([])
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [status, setStatus] = useState<CourseStatus | ''>('')
  const [area, setArea] = useState('')
  const [adding, setAdding] = useState(false)
  const [timer, setTimer] = useState<RunningTimer | null>(() => loadTimer())

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

  // App instalado pode ficar aberto de um dia para o outro: atualiza "hoje" ao voltar para ele
  useEffect(() => {
    const refresh = () => {
      if (document.visibilityState === 'visible') setCurrentDay(toIsoDate(new Date()))
    }
    document.addEventListener('visibilitychange', refresh)
    return () => document.removeEventListener('visibilitychange', refresh)
  }, [])

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

  // Criar, editar e excluir podem renumerar os dias de todos os cursos, então recarrega a lista inteira
  const mutate = useCallback(
    async (action: () => Promise<unknown>) => {
      try {
        await action()
        await load()
      } catch (e) {
        if (e instanceof UnauthorizedError) onUnauthorized?.()
        throw e
      }
    },
    [load, onUnauthorized],
  )

  const createCourse = (details: CourseDetails) =>
    mutate(() => store.createCourse(details)).then(() => setAdding(false))
  const editCourse = (id: number, details: CourseDetails) => mutate(() => store.updateCourse(id, details))
  const deleteCourse = (id: number) => mutate(() => store.deleteCourse(id))
  const moveToToday = (c: Course) =>
    editCourse(c.id, { date: today, platform: c.platform, name: c.name, area: c.area, url: c.url, estimatedHours: c.estimatedHours })

  const changeTimer = (next: RunningTimer | null) => {
    saveTimer(next)
    setTimer(next)
  }
  const startTimer = (courseId: number) => changeTimer({ courseId, startedAt: new Date().toISOString() })
  const clearTimer = () => changeTimer(null)

  const pendingCount = courses.filter((c) => c.status === 'NotStarted' || c.status === 'InProgress').length
  const exportCalendar = () => downloadFile('cursotracker.ics', buildCalendar(courses, new Date()), 'text/calendar')

  const certificates = portfolioCertificates(courses)
  const exportCertificates = () =>
    downloadFile('certificados.json', JSON.stringify(certificates, null, 2), 'application/json')

  const areas = useMemo(() => [...new Set(courses.map((c) => c.area))].sort(), [courses])
  const visible = courses.filter((c) => (!status || c.status === status) && (!area || c.area === area))

  return (
    <main className="page">
      <header className="top">
        <nav className="top-actions">
          {onDiscover && (
            <button type="button" onClick={onDiscover}>
              Descobrir cursos com IA
            </button>
          )}
          {onEditProfile && (
            <button type="button" className="secondary" onClick={onEditProfile}>
              Perfil
            </button>
          )}
          {onLogout && (
            <button type="button" className="secondary" onClick={onLogout}>
              Sair
            </button>
          )}
        </nav>
        <h1>CursoTracker</h1>
        <p>Cursos gratuitos com certificado, um dia de cada vez.</p>
      </header>

      {error && <p className="error" role="alert">{error}</p>}
      {summary && courses.length === 0 && onDiscover && (
        <section className="empty-plan">
          <h2>Seu plano ainda está vazio</h2>
          <p>Deixe a IA encontrar cursos gratuitos com certificado para o seu perfil, ou adicione um curso manualmente.</p>
          <button type="button" onClick={onDiscover}>
            Descobrir cursos com IA
          </button>
        </section>
      )}
      {summary && <DashboardPanel summary={summary} />}
      <AgendaPanel {...agenda(courses, today)} onMoveToToday={moveToToday} />

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
        <button
          type="button"
          className="secondary calendar"
          onClick={exportCalendar}
          disabled={pendingCount === 0}
          title="Baixa um arquivo .ics com um lembrete às 9h no dia de cada curso pendente"
        >
          Adicionar ao calendário
        </button>
        <button type="button" className="new-course" onClick={() => setAdding(true)} disabled={adding}>
          + Novo curso
        </button>
      </div>

      {adding && (
        <section className="course editing" aria-label="Novo curso">
          <h3>Novo curso</h3>
          <CourseForm
            initial={{ date: today, platform: '', name: '', area: '', url: '', estimatedHours: 2 }}
            areas={areas}
            submitLabel="Adicionar curso"
            onSubmit={createCourse}
            onCancel={() => setAdding(false)}
          />
        </section>
      )}

      {groupByDay(visible).map((group) => (
        <section key={group.day} className="day">
          <h2>
            Dia {group.day} <span>{formatDate(group.date)}</span>
            {group.date === today && <span className="today-badge">Hoje</span>}
          </h2>
          {group.courses.map((course) => (
            <CourseCard
              key={course.id}
              course={course}
              onSave={saveProgress}
              onEdit={editCourse}
              onDelete={deleteCourse}
              areas={areas}
              overdue={isOverdue(course, today)}
              timerStartedAt={timer?.courseId === course.id ? timer.startedAt : null}
              timerElsewhere={timer !== null && timer.courseId !== course.id}
              onStartTimer={startTimer}
              onClearTimer={clearTimer}
            />
          ))}
        </section>
      ))}

      {!error && courses.length > 0 && visible.length === 0 && <p className="empty">Nenhum curso com esses filtros.</p>}

      <section className="backup" aria-label="Portfólio">
        <h2>Portfólio</h2>
        <p>
          {certificates.length > 0
            ? `${certificates.length} certificado(s) prontos para o seu site. Baixe o arquivo e atualize o certificados.json do portfólio.`
            : 'Quando você concluir um curso e marcar o certificado como recebido, ele aparece aqui para ir ao seu portfólio.'}
        </p>
        <div className="backup-actions">
          <button type="button" onClick={exportCertificates} disabled={certificates.length === 0}>
            Exportar certificados
          </button>
        </div>
      </section>

      {importCourses && <ImportPanel importCourses={importCourses} onImported={() => load().catch(fail)} />}
    </main>
  )
}
