import type { CourseStore } from './store'
import type { Course, CourseDetails, DashboardSummary } from './types'

export class MemoryStorage implements Storage {
  private data = new Map<string, string>()
  get length() { return this.data.size }
  clear() { this.data.clear() }
  getItem(key: string) { return this.data.get(key) ?? null }
  key(index: number) { return [...this.data.keys()][index] ?? null }
  removeItem(key: string) { this.data.delete(key) }
  setItem(key: string, value: string) { this.data.set(key, value) }
}

export function course(id: number, date: string, p: Partial<Course> = {}): Course {
  return {
    id, day: 1, date, platform: 'Plataforma', name: `Curso ${id}`, area: 'Cloud', url: `https://example.com/${id}`,
    estimatedHours: 2, hoursDone: 0, status: 'NotStarted', completedAt: null, certificateReceived: false,
    certificateUrl: null, onLinkedIn: false, notes: null, ...p,
  }
}

const emptySummary: DashboardSummary = {
  totalCourses: 0, completedCourses: 0, skippedCourses: 0, completedPercent: 0, certificatesReceived: 0,
  certificatesPending: 0, onLinkedIn: 0, estimatedHours: 0, hoursDone: 0, days: [],
}

/** Plano em memória para testar a tela sem servidor (o servidor real é testado no backend). */
export function createFakeStore(initial: Course[]): CourseStore {
  let courses = [...initial]
  let nextId = Math.max(0, ...courses.map((c) => c.id)) + 1
  const sorted = () => [...courses].sort((a, b) => a.date.localeCompare(b.date) || a.id - b.id)
  const find = (id: number) => {
    const c = courses.find((x) => x.id === id)
    if (!c) throw new Error('Curso não encontrado.')
    return c
  }
  const replace = (updated: Course) => {
    courses = courses.map((c) => (c.id === updated.id ? updated : c))
    return updated
  }

  return {
    courses: async () => sorted(),
    dashboard: async () => ({ ...emptySummary, totalCourses: courses.length }),
    updateProgress: async (id, p) => replace({ ...find(id), ...p }),
    createCourse: async (d: CourseDetails) => {
      const created = course(nextId++, d.date, d)
      courses.push(created)
      return created
    },
    updateCourse: async (id, d) => replace({ ...find(id), ...d }),
    deleteCourse: async (id) => {
      find(id)
      courses = courses.filter((c) => c.id !== id)
    },
  }
}
