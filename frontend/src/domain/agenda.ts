import type { Course } from '../types'

const pending = (c: Course) => c.status === 'NotStarted' || c.status === 'InProgress'

/** Data local no formato do plano (AAAA-MM-DD), sem converter para UTC. */
export function toIsoDate(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function agenda(courses: Course[], today: string) {
  return {
    today: courses.filter((c) => c.date === today && pending(c)),
    overdue: courses.filter((c) => c.date < today && pending(c)),
  }
}

export const isOverdue = (course: Course, today: string) => course.date < today && pending(course)
