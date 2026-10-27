import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { CourseSuggestion, Profile, Recommendations } from '../types'
import { DiscoverPanel } from './DiscoverPanel'

const profile: Profile = {
  lookingFor: 'Cloud e DevOps para conseguir emprego',
  goal: 'GetJob',
  level: 'Beginner',
  language: 'Portuguese',
  hoursPerWeek: 6,
  remainingSearchesToday: 5,
}

const suggestion = (name: string): CourseSuggestion => ({
  name,
  platform: 'Linux Foundation',
  url: `https://example.com/${encodeURIComponent(name)}`,
  area: 'DevOps',
  estimatedHours: 14,
  language: 'Inglês',
  certificate: 'Certificado de conclusão gratuito',
  reason: 'Base pedida em vagas de entrada.',
})

const found: Recommendations = {
  courses: [suggestion('Introduction to Kubernetes'), suggestion('Docker Essentials')],
  note: 'Poucas opções em português.',
  remainingSearchesToday: 0,
  searchedAt: '2026-10-26T13:05:00Z',
}

function setup(p: Partial<Parameters<typeof DiscoverPanel>[0]> = {}) {
  const props = {
    profile,
    search: vi.fn<(r: string | null) => Promise<Recommendations>>().mockResolvedValue(found),
    latest: vi.fn<() => Promise<Recommendations | null>>().mockResolvedValue(null),
    onAdd: vi.fn<(s: CourseSuggestion[]) => Promise<void>>().mockResolvedValue(),
    onEditProfile: vi.fn(),
    onBack: vi.fn(),
    ...p,
  }
  render(<DiscoverPanel {...props} />)
  return props
}

describe('DiscoverPanel', () => {
  it('searches right away after onboarding and shows the results', async () => {
    const { search } = setup({ searchOnOpen: true })

    expect(await screen.findByRole('link', { name: 'Introduction to Kubernetes' })).toBeInTheDocument()
    expect(search).toHaveBeenCalledOnce()
    expect(search).toHaveBeenCalledWith(null)
    expect(screen.getByText('Poucas opções em português.')).toBeInTheDocument()
    expect(screen.getByText('Você já fez a busca de hoje. A próxima fica disponível amanhã.')).toBeInTheDocument()
  })

  it('sends the refinement typed by the user', async () => {
    const { search } = setup()

    await userEvent.type(screen.getByLabelText('Quer ajustar a busca? (opcional)'), 'só em português')
    await userEvent.click(screen.getByRole('button', { name: 'Buscar cursos' }))

    expect(search).toHaveBeenCalledWith('só em português')
  })

  it('adds one course or all of them to the plan', async () => {
    const { onAdd } = setup({ searchOnOpen: true })
    const card = (await screen.findByRole('link', { name: 'Docker Essentials' })).closest('article')!

    await userEvent.click(within(card).getByRole('button', { name: 'Adicionar ao plano' }))
    expect(onAdd).toHaveBeenLastCalledWith([found.courses[1]])
    expect(await within(card).findByText('✔ No seu plano')).toBeInTheDocument()

    expect(screen.queryByRole('button', { name: /Adicionar todos/ })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Adicionar ao plano' }))
    expect(onAdd).toHaveBeenLastCalledWith([found.courses[0]])
  })

  it('shows the server message when the daily limit is over', async () => {
    setup({
      searchOnOpen: true,
      search: vi.fn().mockRejectedValue(new Error('Você já fez as 5 buscas de hoje. Volte amanhã!')),
    })

    expect(await screen.findByRole('alert')).toHaveTextContent('Você já fez as 5 buscas de hoje.')
  })

  it('disables searching when no searches are left', () => {
    setup({ profile: { ...profile, remainingSearchesToday: 0 } })

    expect(screen.getByRole('button', { name: 'Buscar cursos' })).toBeDisabled()
    expect(screen.getByText('Você já fez a busca de hoje. A próxima fica disponível amanhã.')).toBeInTheDocument()
  })

  it('shows the saved search when opened, without searching again', async () => {
    const { search, latest } = setup({ latest: vi.fn().mockResolvedValue(found) })

    expect(await screen.findByRole('link', { name: 'Introduction to Kubernetes' })).toBeInTheDocument()
    expect(screen.getByText((text) => text.startsWith('Resultado da busca de 26/10'))).toBeInTheDocument()
    expect(latest).toHaveBeenCalledOnce()
    expect(search).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Buscar de novo' })).toBeDisabled()
  })
})
