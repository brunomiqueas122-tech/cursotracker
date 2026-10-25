// Espelho de Domain/Course.cs + UpdateProgressValidator: usado quando os dados ficam no aparelho (modo local)
import type { Course, UpdateProgressRequest } from '../types'
import { courseStatuses } from '../types'
import { isHttpUrl } from './plan'

const MAX_HOURS = 500
const MAX_NOTES = 1000
const MAX_URL = 500

export function validateProgress(p: UpdateProgressRequest): string[] {
  const errors: string[] = []
  if (!Number.isFinite(p.hoursDone) || p.hoursDone < 0) errors.push('As horas feitas não podem ser negativas.')
  else if (p.hoursDone > MAX_HOURS) errors.push(`Informe no máximo ${MAX_HOURS} horas.`)
  if (!courseStatuses.includes(p.status)) errors.push('Status inválido.')
  const url = p.certificateUrl?.trim()
  if (url && (url.length > MAX_URL || !isHttpUrl(url))) errors.push('Informe um link começando com http:// ou https://.')
  if ((p.notes?.length ?? 0) > MAX_NOTES) errors.push(`As anotações podem ter no máximo ${MAX_NOTES} caracteres.`)
  return errors
}

export function applyProgress(course: Course, p: UpdateProgressRequest, now: Date): Course {
  // Registrar horas num curso não iniciado significa que ele começou
  const status = p.status === 'NotStarted' && p.hoursDone > 0 ? 'InProgress' : p.status

  return {
    ...course,
    status,
    hoursDone: p.hoursDone,
    completedAt: status === 'Completed' ? (course.completedAt ?? now.toISOString()) : null,
    certificateReceived: p.certificateReceived,
    certificateUrl: p.certificateReceived ? normalize(p.certificateUrl) : null,
    onLinkedIn: p.onLinkedIn,
    notes: normalize(p.notes),
  }
}

function normalize(value: string | null): string | null {
  const trimmed = value?.trim()
  return trimmed ? trimmed : null
}
