import { useCallback, useEffect, useMemo, useState } from 'react'
import { UnauthorizedError } from './api'
import { AgendaPanel } from './components/AgendaPanel'
import { BackupPanel } from './components/BackupPanel'
import { CourseCard } from './components/CourseCard'
import { CourseForm } from './components/CourseForm'
import { DashboardPanel } from './components/DashboardPanel'
import { agenda, isOverdue, toIsoDate } from './domain/agenda'
import { backupReminder } from './domain/backupReminder'
import { buildCalendar } from './domain/calendar'
import { downloadFile } from './download'
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
  /** Data de hoje (AAAA-MM-DD); injetável para testes */
  today?: string
}

export function Tracker({ store, onUnauthorized, onLogout, today: fixedToday }: Props) {
  const [currentDay, setCurrentDay] = useState(() => toIsoDate(new Date()))
  const today = fixedToday ?? currentDay

  const [courses, setCourses] = useState<Course[]>([])
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [status, setStatus] = useState<CourseStatus | ''>('')
  const [area, setArea] = useState('')
  const [adding, setAdding] = useState(false)
  const [lastBackup, setLastBackup] = useState(() => store.backup?.lastBackupAt() ?? null)

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

  function exportBackup() {
    if (!store.backup) return
    downloadFile(`cursotracker-backup-${today}.json`, store.backup.exportJson(), 'application/json')
    setLastBackup(store.backup.lastBackupAt())
  }
  const pendingCount = courses.filter((c) => c.status === 'NotStarted' || c.status === 'InProgress').length
  const exportCalendar = () => downloadFile('cursotracker.ics', buildCalendar(courses, new Date()), 'text/calendar')

  const reminder = store.backup ? backupReminder(courses, lastBackup, today) : null

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
      {reminder && (
        <div className="reminder" role="status">
          <span>
            {reminder.kind === 'never'
              ? 'Você ainda não fez nenhum backup do seu progresso.'
              : `Seu último backup foi há ${reminder.days} dias.`}
          </span>
          <button type="button" onClick={exportBackup}>
            Baixar backup
          </button>
        </div>
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
            />
          ))}
        </section>
      ))}

      {!error && summary && visible.length === 0 && <p className="empty">Nenhum curso com esses filtros.</p>}

      {store.backup && <BackupPanel backup={store.backup} onImported={() => load().catch(fail)} onExport={exportBackup} />}
    </main>
  )
}
