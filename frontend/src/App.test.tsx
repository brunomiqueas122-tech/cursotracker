import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'

function mockFetch(handler: (url: string, init?: RequestInit) => Response) {
  vi.stubGlobal('fetch', vi.fn((url: string, init?: RequestInit) => Promise.resolve(handler(url, init))))
}

const json = (body: unknown) => new Response(JSON.stringify(body), { status: 200 })
const emptyDashboard = {
  totalCourses: 0, completedCourses: 0, skippedCourses: 0, completedPercent: 0, certificatesReceived: 0,
  certificatesPending: 0, onLinkedIn: 0, estimatedHours: 0, hoursDone: 0, days: [],
}

afterEach(() => vi.unstubAllGlobals())

describe('App', () => {
  it('asks for the password when not logged in', async () => {
    mockFetch(() => new Response(null, { status: 401 }))

    render(<App />)

    expect(await screen.findByLabelText('Senha')).toBeInTheDocument()
  })

  it('shows an error for a wrong password', async () => {
    mockFetch(() => new Response(null, { status: 401 }))
    render(<App />)

    await userEvent.type(await screen.findByLabelText('Senha'), 'errada')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Senha incorreta.')
  })

  it('opens the tracker after a successful login', async () => {
    let loggedIn = false
    mockFetch((url) => {
      if (url === '/api/auth/login') {
        loggedIn = true
        return new Response(null, { status: 204 })
      }
      if (!loggedIn) return new Response(null, { status: 401 })
      return url === '/api/courses' ? json([]) : json(emptyDashboard)
    })
    render(<App />)

    await userEvent.type(await screen.findByLabelText('Senha'), 'certa')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('button', { name: 'Sair' })).toBeInTheDocument()
    expect(await screen.findByRole('region', { name: 'Resumo' })).toBeInTheDocument()
  })
})
