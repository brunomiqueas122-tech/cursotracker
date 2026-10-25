import seed from '../../seed-courses.json'
import { applyProgress, validateProgress } from './domain/course'
import { summarize } from './domain/dashboard'
import type { CourseStore } from './store'
import { courseStatuses, type Course } from './types'

const KEY = 'cursotracker.v1'

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

  return {
    courses: () => Promise.resolve().then(() => sorted(load())),
    dashboard: () => Promise.resolve().then(() => summarize(load())),
    updateProgress: (id, progress) =>
      Promise.resolve().then(() => {
        const errors = validateProgress(progress)
        if (errors.length > 0) throw new Error(errors.join(' '))
        const courses = load()
        const index = courses.findIndex((c) => c.id === id)
        if (index < 0) throw new Error('Curso não encontrado.')
        courses[index] = applyProgress(courses[index], progress, now())
        save(courses)
        return courses[index]
      }),
    backup: {
      exportJson: () => JSON.stringify({ version: 1, courses: load() } satisfies SavedData, null, 2),
      importJson: (json) => save(parse(json)),
    },
  }
}
