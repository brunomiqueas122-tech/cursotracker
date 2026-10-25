// Espelho de Domain/CoursePlan.cs + CourseDetailsValidator
import type { Course, CourseDetails } from '../types'

const DAY_MS = 86_400_000
const dayNumber = (isoDate: string) => Math.round(Date.parse(`${isoDate}T00:00:00Z`) / DAY_MS)

/** O dia 1 é a data mais antiga do plano; cada curso fica no dia correspondente à sua data. */
export function renumber(courses: Course[]): Course[] {
  if (courses.length === 0) return courses
  const start = Math.min(...courses.map((c) => dayNumber(c.date)))
  return courses.map((c) => ({ ...c, day: dayNumber(c.date) - start + 1 }))
}

export function validateDetails(d: CourseDetails): string[] {
  const errors: string[] = []
  if (!/^\d{4}-\d{2}-\d{2}$/.test(d.date) || Number.isNaN(Date.parse(d.date))) errors.push('Informe a data.')
  if (!d.name.trim()) errors.push('Informe o nome do curso.')
  else if (d.name.trim().length > 200) errors.push('O nome pode ter no máximo 200 caracteres.')
  if (!d.platform.trim()) errors.push('Informe a plataforma.')
  else if (d.platform.trim().length > 100) errors.push('A plataforma pode ter no máximo 100 caracteres.')
  if (!d.area.trim()) errors.push('Informe a área.')
  else if (d.area.trim().length > 100) errors.push('A área pode ter no máximo 100 caracteres.')
  if (!d.url.trim()) errors.push('Informe o link do curso.')
  else if (d.url.trim().length > 500 || !isHttpUrl(d.url.trim())) errors.push('Informe um link começando com http:// ou https://.')
  if (!Number.isFinite(d.estimatedHours) || d.estimatedHours <= 0) errors.push('As horas estimadas devem ser maiores que zero.')
  else if (d.estimatedHours > 500) errors.push('Informe no máximo 500 horas.')
  return errors
}

export function normalizeDetails(d: CourseDetails): CourseDetails {
  return {
    date: d.date,
    platform: d.platform.trim(),
    name: d.name.trim(),
    area: d.area.trim(),
    url: d.url.trim(),
    estimatedHours: d.estimatedHours,
  }
}

export const applyDetails = (course: Course, d: CourseDetails): Course => ({ ...course, ...normalizeDetails(d) })

export function isHttpUrl(value: string): boolean {
  try {
    const { protocol } = new URL(value)
    return protocol === 'http:' || protocol === 'https:'
  } catch {
    return false
  }
}
