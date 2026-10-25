// Espelho de Features/Dashboard/DashboardSummary.cs
import type { Course, DashboardSummary, DaySummary } from '../types'

const sum = (values: number[]) => values.reduce((a, b) => a + b, 0)

export function summarize(courses: Course[]): DashboardSummary {
  // Cursos pulados saem da meta: não contam no % nem nas horas estimadas
  const planned = courses.filter((c) => c.status !== 'Skipped')
  const completed = planned.filter((c) => c.status === 'Completed').length

  const byDay = new Map<number, Course[]>()
  for (const c of courses) byDay.set(c.day, [...(byDay.get(c.day) ?? []), c])

  const days: DaySummary[] = [...byDay.entries()]
    .sort(([a], [b]) => a - b)
    .map(([day, list]) => {
      const dayPlanned = list.filter((c) => c.status !== 'Skipped')
      return {
        day,
        date: list.map((c) => c.date).sort()[0],
        courses: dayPlanned.length,
        completed: list.filter((c) => c.status === 'Completed').length,
        estimatedHours: sum(dayPlanned.map((c) => c.estimatedHours)),
        hoursDone: sum(list.map((c) => c.hoursDone)),
      }
    })

  return {
    totalCourses: courses.length,
    completedCourses: completed,
    skippedCourses: courses.length - planned.length,
    completedPercent: planned.length === 0 ? 0 : Math.round((1000 * completed) / planned.length) / 10,
    certificatesReceived: courses.filter((c) => c.certificateReceived).length,
    certificatesPending: courses.filter((c) => c.status === 'Completed' && !c.certificateReceived).length,
    onLinkedIn: courses.filter((c) => c.onLinkedIn).length,
    estimatedHours: sum(planned.map((c) => c.estimatedHours)),
    hoursDone: sum(courses.map((c) => c.hoursDone)),
    days,
  }
}
