import type { Course, DashboardSummary, UpdateProgressRequest } from './types'

interface ValidationProblem {
  errors?: Record<string, string[]>
}

export class UnauthorizedError extends Error {
  constructor() {
    super('Sua sessão expirou. Entre novamente.')
  }
}

async function send(url: string, init?: RequestInit): Promise<Response> {
  const response = await fetch(url, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })
  if (response.ok) return response

  switch (response.status) {
    case 400: {
      const problem = (await response.json()) as ValidationProblem
      const messages = Object.values(problem.errors ?? {}).flat()
      throw new Error(messages.join(' ') || 'Dados inválidos.')
    }
    case 401:
      throw new UnauthorizedError()
    case 404:
      throw new Error('Curso não encontrado.')
    case 429:
      throw new Error('Muitas tentativas. Espere 1 minuto e tente de novo.')
    default:
      throw new Error('Não foi possível falar com o servidor. Tente de novo em instantes.')
  }
}

async function json<T>(url: string, init?: RequestInit): Promise<T> {
  return (await (await send(url, init)).json()) as T
}

export const api = {
  courses: (signal?: AbortSignal) => json<Course[]>('/api/courses', { signal }),
  dashboard: (signal?: AbortSignal) => json<DashboardSummary>('/api/dashboard', { signal }),
  updateProgress: (id: number, body: UpdateProgressRequest) =>
    json<Course>(`/api/courses/${id}/progress`, { method: 'PUT', body: JSON.stringify(body) }),

  async isLoggedIn(signal?: AbortSignal): Promise<boolean> {
    try {
      await send('/api/auth/me', { signal })
      return true
    } catch (e) {
      if (e instanceof UnauthorizedError) return false
      throw e
    }
  },
  async login(password: string): Promise<void> {
    try {
      await send('/api/auth/login', { method: 'POST', body: JSON.stringify({ password }) })
    } catch (e) {
      throw e instanceof UnauthorizedError ? new Error('Senha incorreta.') : e
    }
  },
  async logout(): Promise<void> {
    await send('/api/auth/logout', { method: 'POST' })
  },
}
