import type { Course } from '../types'

const REMIND_AFTER_DAYS = 7
const DAY_MS = 86_400_000

export type BackupReminder = { kind: 'never' } | { kind: 'stale'; days: number }

/** Só lembra depois que existe progresso a perder. */
export function backupReminder(courses: Course[], lastBackupAt: string | null, today: string): BackupReminder | null {
  const hasProgress = courses.some((c) => c.status !== 'NotStarted' || c.hoursDone > 0 || c.certificateReceived)
  if (!hasProgress) return null
  if (!lastBackupAt) return { kind: 'never' }
  const days = Math.floor((Date.parse(`${today}T00:00:00Z`) - Date.parse(`${lastBackupAt.slice(0, 10)}T00:00:00Z`)) / DAY_MS)
  return days >= REMIND_AFTER_DAYS ? { kind: 'stale', days } : null
}
