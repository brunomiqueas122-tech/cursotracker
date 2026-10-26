import type { Course } from '../types'

function addDays(isoDate: string, days: number): string {
  const d = new Date(`${isoDate}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() + days)
  return d.toISOString().slice(0, 10)
}

/** Datas para cursos sugeridos: um por dia, a partir do dia seguinte ao fim do plano (ou de hoje). */
export function nextPlanDates(courses: Course[], today: string, count: number): string[] {
  const last = courses.map((c) => c.date).sort().at(-1)
  const start = last && last >= today ? addDays(last, 1) : today
  return Array.from({ length: count }, (_, i) => addDays(start, i))
}
