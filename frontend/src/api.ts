import type { Course, DashboardSummary, UpdateProgressRequest } from './types'

interface ValidationProblem {
  errors?: Record<string, string[]>
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })
  if (!response.ok) {
    if (response.status === 400) {
      const problem = (await response.json()) as ValidationProblem
      const messages = Object.values(problem.errors ?? {}).flat()
      throw new Error(messages.join(' ') || 'Dados inválidos.')
    }
    if (response.status === 404) throw new Error('Curso não encontrado.')
    throw new Error('Não foi possível falar com o servidor. Confira se o backend está rodando.')
  }
  return (await response.json()) as T
}

export const api = {
  courses: (signal?: AbortSignal) => request<Course[]>('/api/courses', { signal }),
  dashboard: (signal?: AbortSignal) => request<DashboardSummary>('/api/dashboard', { signal }),
  updateProgress: (id: number, body: UpdateProgressRequest) =>
    request<Course>(`/api/courses/${id}/progress`, { method: 'PUT', body: JSON.stringify(body) }),
}
