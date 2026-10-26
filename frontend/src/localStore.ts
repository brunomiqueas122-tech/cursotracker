import seed from '../../seed-courses.json'
import { applyProgress, validateProgress } from './domain/course'
import { summarize } from './domain/dashboard'
import { applyDetails, normalizeDetails, renumber, validateDetails } from './domain/plan'
import type { CourseStore } from './store'
import { courseStatuses, type Course, type CourseDetails } from './types'

const KEY = 'cursotracker.v1'
const LAST_BACKUP_KEY = 'cursotracker.lastBackup'

interface SavedData {
  version: 1
  courses: Course[]
}

export interface LocalStoreOptions {
  storage?: Storage
  seedCourses?: Omit<Course, 'id'>[]
  now?: () => Date
}

function isCourse(value: unknown): value is Course {
  if (typeof value !== 'object' || value === null) return false
  const c = value as Record<string, unknown>
  return (
    typeof c.id === 'number' &&
    typeof c.day === 'number' &&
    typeof c.name === 'string' &&
    typeof c.estimatedHours === 'number' &&
    typeof c.hoursDone === 'number' &&
    courseStatuses.some((s) => s === c.status)
  )
}

function parse(json: string): Course[] {
  const data: unknown = JSON.parse(json)
  const courses = (data as Partial<SavedData> | null)?.courses
  if (!Array.isArray(courses) || !courses.every(isCourse)) throw new Error('Arquivo de backup inválido.')
  return courses
}

export function createLocalStore({
  storage = localStorage,
  seedCourses = seed as Omit<Course, 'id'>[],
  now = () => new Date(),
}: LocalStoreOptions = {}): CourseStore {
  function load(): Course[] {
    const saved = storage.getItem(KEY)
    if (saved) return parse(saved)
    const courses = seedCourses.map((c, i) => ({ ...c, id: i + 1 }))
    save(courses)
    return courses
  }

  function save(courses: Course[]) {
    try {
      storage.setItem(KEY, JSON.stringify({ version: 1, courses } satisfies SavedData))
    } catch {
      throw new Error('Não foi possível salvar neste aparelho. Verifique o espaço disponível.')
    }
  }

  const sorted = (courses: Course[]) => [...courses].sort((a, b) => a.day - b.day || a.id - b.id)

  function checked(details: CourseDetails): CourseDetails {
    const errors = validateDetails(details)
    if (errors.length > 0) throw new Error(errors.join(' '))
    return details
  }

  function find(courses: Course[], id: number): number {
    const index = courses.findIndex((c) => c.id === id)
    if (index < 0) throw new Error('Curso não encontrado.')
    return index
  }

  return {
    courses: () => Promise.resolve().then(() => sorted(load())),
    dashboard: () => Promise.resolve().then(() => summarize(load())),
    updateProgress: (id, progress) =>
      Promise.resolve().then(() => {
        const errors = validateProgress(progress)
        if (errors.length > 0) throw new Error(errors.join(' '))
        const courses = load()
        const index = find(courses, id)
        courses[index] = applyProgress(courses[index], progress, now())
        save(courses)
        return courses[index]
      }),
    createCourse: (details) =>
      Promise.resolve().then(() => {
        const d = checked(details)
        const courses = load()
        const id = Math.max(0, ...courses.map((c) => c.id)) + 1
        const course: Course = {
          ...normalizeDetails(d),
          id,
          day: 1,
          hoursDone: 0,
          status: 'NotStarted',
          completedAt: null,
          certificateReceived: false,
          certificateUrl: null,
          onLinkedIn: false,
          notes: null,
        }
        const updated = renumber([...courses, course])
        save(updated)
        return updated.find((c) => c.id === id)!
      }),
    updateCourse: (id, details) =>
      Promise.resolve().then(() => {
        const d = checked(details)
        const courses = load()
        const index = find(courses, id)
        courses[index] = applyDetails(courses[index], d)
        const updated = renumber(courses)
        save(updated)
        return updated[index]
      }),
    deleteCourse: (id) =>
      Promise.resolve().then(() => {
        const courses = load()
        find(courses, id)
        save(renumber(courses.filter((c) => c.id !== id)))
      }),
    backup: {
      exportJson: () => {
        const json = JSON.stringify({ version: 1, courses: load() } satisfies SavedData, null, 2)
        try {
          storage.setItem(LAST_BACKUP_KEY, now().toISOString())
        } catch {
          // Sem espaço para a data: o lembrete só volta a aparecer, o backup em si não é afetado
        }
        return json
      },
      importJson: (json) => save(parse(json)),
      lastBackupAt: () => {
        try {
          return storage.getItem(LAST_BACKUP_KEY)
        } catch {
          return null
        }
      },
    },
  }
}
