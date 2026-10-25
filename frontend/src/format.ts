import type { Course, CourseStatus } from './types'

export const statusLabels: Record<CourseStatus, string> = {
  NotStarted: 'Não iniciado',
  InProgress: 'Em andamento',
  Completed: 'Concluído',
  Skipped: 'Pulado',
}

const hoursFormat = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 })

export function formatHours(hours: number): string {
  return `${hoursFormat.format(hours)} h`
}

export function formatDate(isoDate: string): string {
  // "2026-10-25" sem fuso: montar a data local evita voltar um dia no Brasil
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString('pt-BR', {
    weekday: 'short',
    day: '2-digit',
    month: '2-digit',
  })
}

export interface DayGroup {
  day: number
  date: string
  courses: Course[]
}

export function groupByDay(courses: Course[]): DayGroup[] {
  const groups = new Map<number, DayGroup>()
  for (const course of courses) {
    const group = groups.get(course.day) ?? { day: course.day, date: course.date, courses: [] }
    group.courses.push(course)
    groups.set(course.day, group)
  }
  return [...groups.values()].sort((a, b) => a.day - b.day)
}
