// Cronômetro de estudo: fica salvo no aparelho para sobreviver a fechar o app
export interface RunningTimer {
  courseId: number
  startedAt: string
}

const KEY = 'cursotracker.timer'
const HOUR_MS = 3_600_000

export function loadTimer(storage: Storage = localStorage): RunningTimer | null {
  try {
    const data: unknown = JSON.parse(storage.getItem(KEY) ?? 'null')
    const t = data as Partial<RunningTimer> | null
    return typeof t?.courseId === 'number' && typeof t.startedAt === 'string' && !Number.isNaN(Date.parse(t.startedAt))
      ? { courseId: t.courseId, startedAt: t.startedAt }
      : null
  } catch {
    return null
  }
}

export function saveTimer(timer: RunningTimer | null, storage: Storage = localStorage) {
  try {
    if (timer) storage.setItem(KEY, JSON.stringify(timer))
    else storage.removeItem(KEY)
  } catch {
    // Sem armazenamento: o cronômetro funciona até a página ser fechada
  }
}

export const elapsedMs = (startedAt: string, now: Date) => Math.max(0, now.getTime() - Date.parse(startedAt))

/** Horas com 2 casas, o suficiente para minutos (0,01 h ≈ 36 s). */
export const elapsedHours = (startedAt: string, now: Date) => Math.round((elapsedMs(startedAt, now) / HOUR_MS) * 100) / 100

export function formatElapsed(ms: number): string {
  const total = Math.floor(ms / 1000)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${Math.floor(total / 3600)}:${pad(Math.floor((total % 3600) / 60))}:${pad(total % 60)}`
}
