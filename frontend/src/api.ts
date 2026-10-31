import type {
  Course,
  CourseDetails,
  DashboardSummary,
  Me,
  PremiumCode,
  PremiumStatus,
  Profile,
  ProfileAnswers,
  Recommendations,
  UpdateProgressRequest,
} from './types'

interface ProblemDetails {
  detail?: string
  errors?: Record<string, string[]>
}

export class UnauthorizedError extends Error {
  constructor() {
    super('Sua sessão expirou. Entre novamente.')
  }
}

export class NotFoundError extends Error {
  constructor() {
    super('Não encontrado.')
  }
}

/** 402: recurso do Premium; a tela oferece a assinatura */
export class PremiumRequiredError extends Error {}

async function problemMessage(response: Response): Promise<string | null> {
  try {
    const problem = (await response.json()) as ProblemDetails
    const messages = Object.values(problem.errors ?? {}).flat()
    return messages.length > 0 ? messages.join(' ') : (problem.detail ?? null)
  } catch {
    return null
  }
}

async function send(url: string, init?: RequestInit): Promise<Response> {
  const response = await fetch(url, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })
  if (response.ok) return response

  if (response.status === 401) throw new UnauthorizedError()
  if (response.status === 404) throw new NotFoundError()
  const message = await problemMessage(response)
  if (response.status === 402) throw new PremiumRequiredError(message ?? 'Recurso do Premium.')
  if (message) throw new Error(message)
  if (response.status === 429) throw new Error('Muitas tentativas. Espere 1 minuto e tente de novo.')
  throw new Error('Não foi possível falar com o servidor. Tente de novo em instantes.')
}

async function json<T>(url: string, init?: RequestInit): Promise<T> {
  return (await (await send(url, init)).json()) as T
}

const post = (body: unknown): RequestInit => ({ method: 'POST', body: JSON.stringify(body) })
const put = (body: unknown): RequestInit => ({ method: 'PUT', body: JSON.stringify(body) })

export const api = {
  courses: (signal?: AbortSignal) => json<Course[]>('/api/courses', { signal }),
  dashboard: (signal?: AbortSignal) => json<DashboardSummary>('/api/dashboard', { signal }),
  updateProgress: (id: number, body: UpdateProgressRequest) => json<Course>(`/api/courses/${id}/progress`, put(body)),
  createCourse: (body: CourseDetails) => json<Course>('/api/courses', post(body)),
  updateCourse: (id: number, body: CourseDetails) => json<Course>(`/api/courses/${id}`, put(body)),
  async deleteCourse(id: number): Promise<void> {
    await send(`/api/courses/${id}`, { method: 'DELETE' })
  },
  /** Arquivo "Baixar backup" do app antigo, enviado como está */
  importCourses: (backupJson: string) =>
    json<{ imported: number }>('/api/courses/import', { method: 'POST', body: backupJson }),

  async me(signal?: AbortSignal): Promise<Me | null> {
    try {
      return await json<Me>('/api/auth/me', { signal })
    } catch (e) {
      if (e instanceof UnauthorizedError) return null
      throw e
    }
  },
  async login(email: string, password: string): Promise<void> {
    try {
      await send('/api/auth/login', post({ email, password }))
    } catch (e) {
      throw e instanceof UnauthorizedError ? new Error('E-mail ou senha incorretos.') : e
    }
  },
  async register(email: string, password: string, inviteCode: string): Promise<void> {
    await send('/api/auth/register', post({ email, password, inviteCode }))
  },
  async logout(): Promise<void> {
    await send('/api/auth/logout', { method: 'POST' })
  },

  async profile(signal?: AbortSignal): Promise<Profile | null> {
    try {
      return await json<Profile>('/api/profile', { signal })
    } catch (e) {
      if (e instanceof NotFoundError) return null
      throw e
    }
  },
  saveProfile: (answers: ProfileAnswers) => json<Profile>('/api/profile', put(answers)),
  recommendations: (refinement: string | null) => json<Recommendations>('/api/recommendations', post({ refinement })),
  /** Última busca salva (não gasta a busca do dia) */
  async latestRecommendations(): Promise<Recommendations | null> {
    try {
      return await json<Recommendations>('/api/recommendations/latest')
    } catch (e) {
      if (e instanceof NotFoundError) return null
      throw e
    }
  },

  premium: () => json<PremiumStatus>('/api/premium'),
  redeemPremium: (code: string) => json<PremiumStatus>('/api/premium/redeem', post({ code })),
  premiumCodes: () => json<PremiumCode[]>('/api/premium/codes'),
  createPremiumCode: (days: number | null) => json<{ code: string; days: number | null }>('/api/premium/codes', post({ days })),
}
