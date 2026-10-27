import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'

type Handler = (url: string, init?: RequestInit) => Response

const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status })
const noContent = () => new Response(null, { status: 204 })
const unauthorized = () => new Response(null, { status: 401 })

function mockFetch(handler: Handler) {
  const fetchMock = vi.fn((url: string, init?: RequestInit) => Promise.resolve(handler(url, init)))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

const emptyDashboard = {
  totalCourses: 0, completedCourses: 0, skippedCourses: 0, completedPercent: 0, certificatesReceived: 0,
  certificatesPending: 0, onLinkedIn: 0, estimatedHours: 0, hoursDone: 0, days: [],
}
const profile = { lookingFor: 'Cloud', goal: 'GetJob', level: 'Beginner', language: 'Portuguese', hoursPerWeek: 5, remainingSearchesToday: 5 }

afterEach(() => vi.unstubAllGlobals())

describe('App', () => {
  it('asks to log in when there is no session', async () => {
    mockFetch(unauthorized)

    render(<App />)

    expect(await screen.findByRole('button', { name: 'Entrar' })).toBeInTheDocument()
  })

  it('shows a clear message for a wrong password', async () => {
    mockFetch(unauthorized)
    render(<App />)

    await userEvent.type(await screen.findByLabelText('E-mail'), 'ana@example.com')
    await userEvent.type(screen.getByLabelText('Senha'), 'errada123')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('E-mail ou senha incorretos.')
  })

  it('registers with the invite code, asks the profile and searches courses', async () => {
    let registered = false
    let hasProfile = false
    const fetchMock = mockFetch((url, init) => {
      if (url === '/api/auth/register') {
        registered = true
        return noContent()
      }
      if (!registered) return unauthorized()
      if (url === '/api/auth/me') return json({ email: 'ana@example.com', hasProfile })
      if (url === '/api/profile' && init?.method === 'PUT') {
        hasProfile = true
        return json(profile)
      }
      if (url === '/api/recommendations') return json({ courses: [], note: 'Nada por hoje.', remainingSearchesToday: 0, searchedAt: '2026-10-26T13:05:00Z' })
      return json([])
    })
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Criar uma conta' }))
    await userEvent.type(screen.getByLabelText('E-mail'), 'ana@example.com')
    await userEvent.type(screen.getByLabelText('Senha'), 'senha1234')
    await userEvent.type(screen.getByLabelText('Código de convite'), 'convite')
    await userEvent.click(screen.getByRole('button', { name: 'Criar conta' }))

    await userEvent.type(await screen.findByLabelText('Que tipo de curso você está procurando?'), 'Cloud')
    await userEvent.click(screen.getByRole('button', { name: 'Buscar meus cursos' }))

    expect(await screen.findByText('Nada por hoje.')).toBeInTheDocument()
    const register = fetchMock.mock.calls.find(([url]) => url === '/api/auth/register')!
    expect(JSON.parse(register[1]!.body as string)).toEqual({ email: 'ana@example.com', password: 'senha1234', inviteCode: 'convite' })
  })

  it('opens the plan for a returning user', async () => {
    mockFetch((url) => {
      if (url === '/api/auth/me') return json({ email: 'ana@example.com', hasProfile: true })
      if (url === '/api/profile') return json(profile)
      if (url === '/api/dashboard') return json(emptyDashboard)
      return json([])
    })

    render(<App />)

    expect(await screen.findByText('Seu plano ainda está vazio')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Sair' })).toBeInTheDocument()
  })
})
